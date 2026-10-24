using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adaptive hints. Time on a floor is counted only while the player is free to act (not paused, in dialogue or in a menu).
// Hints unlock in three tiers, relative to an expected clear time that adapts to the player:
//   expected = par x lerp(1, pace, 0.7) x lerp(1, 0.7, helpBias)
//   par      = the floor's shadow timer (TimedActivator), or a default
//   pace     = how the player's earlier clear times compare with par (slower players are given more time)
//   helpBias = how often the player asked for hints before (they get hints sooner)
// A hint is never shown on its own: the floor's lightbulb button (or a "Hint ready" note) appears, and the player
// clicks it or presses H. Hints come one at a time in order. Hint 1 is the text written for the floor in its
// original Hint Panel, hints 2 and 3 are built from the state of the floor.
// After a hint is shown the next one is locked for CooldownSeconds of free time.
// The hint text is built from the current state of the floor, so it stays correct as the puzzle progresses.
// At level 1 (the first puzzle floor) a short guide explains all of this once. Shift+H opens it again.
public class HintManager : MonoBehaviour
{
    public const int TierCount = 3;
    public const float CooldownSeconds = 60f;
    public const string SaveId = "HintState";

    private const float DefaultPar = 180f;
    private const float PanelSeconds = 10f;
    private const float TierOneFraction = 0.35f;
    private const float TierTwoFraction = 0.65f;
    private const float TierThreeFraction = 1.0f;
    private const string PaceKey = "hintPace";
    private const string HelpKey = "hintHelpBias";
    private const string GuideKey = "hintGuideShown";
    private const string OldGuideKey = "hintGuideSeen";
    private const string GuideSceneName = "lvl1";

    public enum HintRequest { NotYetUnlocked, OnCooldown, Show }
    public enum BadgeState { Hidden, Ready, Waiting }

    public static HintManager Instance { get; private set; }

    // 1 = clears floors on par, below 1 = faster, above 1 = slower.
    public static float Pace { get; private set; } = 1f;

    // 0 to 1: how often the player asked for a hint on earlier floors.
    public static float HelpBias { get; private set; }

    private bool floorActive;
    private int floorBuildIndex = -1;
    private float floorSeconds;
    private float par = DefaultPar;
    private int viewedTier;
    private bool usedHintThisFloor;
    private bool suppressNextClear;
    private float cooldownLeft;
    private string lastHintText;
    private PlayerController playerController;
    private float nextPlayerSearch;

    // The floor's original lightbulb button and panel, if it has them. The button now follows the adaptive timing,
    // and the text written in the panel is used as hint 1.
    private GameObject legacyButton;
    private GameObject legacyPanel;
    private string authoredHint;

    private bool guidePending;
    private bool guideOpen;
    private int guideKeyFrame = -1;

    private Canvas canvas;
    private GameObject panelRoot;
    private TextMeshProUGUI panelText;
    private GameObject badgeRoot;
    private CanvasGroup badgeGroup;
    private TextMeshProUGUI badgeLabel;
    private float panelUntil;

    private GameObject guideCanvasRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("HintManager");
        DontDestroyOnLoad(go);
        go.AddComponent<HintManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Pace = PlayerPrefs.GetFloat(PaceKey, 1f);
        HelpBias = PlayerPrefs.GetFloat(HelpKey, 0f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    // ---------- Adaptive timing ----------

    // Seconds of free time on the floor at which each tier unlocks.
    public static float[] TierTimes(float par, float pace, float helpBias)
    {
        float expected = par * Mathf.Lerp(1f, Mathf.Clamp(pace, 0.5f, 1.6f), 0.7f);
        expected *= Mathf.Lerp(1f, 0.7f, Mathf.Clamp01(helpBias));

        float first = Mathf.Max(45f, expected * TierOneFraction);
        float second = Mathf.Max(first + 30f, expected * TierTwoFraction);
        float third = Mathf.Max(second + 30f, expected * TierThreeFraction);
        return new[] { first, second, third };
    }

    // What pressing H does right now.
    public static HintRequest ClassifyRequest(int unlockedTier, float cooldownLeft)
    {
        if (unlockedTier <= 0) return HintRequest.NotYetUnlocked;
        if (cooldownLeft > 0f) return HintRequest.OnCooldown;
        return HintRequest.Show;
    }

    // What the small badge at the bottom of the screen shows: nothing, "ready", or a countdown to the next hint.
    public static BadgeState ClassifyBadge(int unlockedTier, int viewedTier, float cooldownLeft, bool free, bool panelShowing)
    {
        if (!free || panelShowing || unlockedTier <= viewedTier) return BadgeState.Hidden;
        return cooldownLeft > 0f ? BadgeState.Waiting : BadgeState.Ready;
    }

    // Hints are given in order, one at a time: the next one the player has not seen yet. When every unlocked
    // hint has been seen, the latest one is shown again.
    public static int ChooseTier(int unlockedTier, int viewedTier)
    {
        if (unlockedTier <= 0) return 0;
        return viewedTier < unlockedTier ? viewedTier + 1 : unlockedTier;
    }

    // The original lightbulb "Hint Button" and "Hint Panel" of a floor, and the hint text written in the panel.
    public static bool FindLegacyHint(Scene scene, out GameObject button, out GameObject panel, out string text)
    {
        button = null;
        panel = null;
        text = null;

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.gameObject.scene != scene) continue;
            if (t.name == "Hint Button") button = t.gameObject;
            else if (t.name == "Hint Panel") panel = t.gameObject;
        }

        if (panel != null)
        {
            TMP_Text longest = null;
            foreach (TMP_Text candidate in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (longest == null || candidate.text.Length > longest.text.Length) longest = candidate;
            }
            if (longest != null && !string.IsNullOrWhiteSpace(longest.text)) text = longest.text.Trim();
        }

        return button != null;
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return (total / 60) + ":" + (total % 60).ToString("00");
    }

    // Updates the player's profile after a floor was cleared.
    public static void ApplyClear(float clearSeconds, float par, bool usedHint)
    {
        float ratio = Mathf.Clamp(clearSeconds / Mathf.Max(1f, par), 0.3f, 3f);
        Pace = Mathf.Lerp(Pace, ratio, 0.5f);
        HelpBias = Mathf.Lerp(HelpBias, usedHint ? 1f : 0f, 0.4f);
        PlayerPrefs.SetFloat(PaceKey, Pace);
        PlayerPrefs.SetFloat(HelpKey, HelpBias);
    }

    // Used when starting a new game. The guide is shown again at level 1 too.
    public static void ResetProfile()
    {
        Pace = 1f;
        HelpBias = 0f;
        PlayerPrefs.DeleteKey(PaceKey);
        PlayerPrefs.DeleteKey(HelpKey);
        PlayerPrefs.DeleteKey(GuideKey);
        PlayerPrefs.DeleteKey(OldGuideKey);
    }

    // The guide is shown once, the first time the player is free in this scene.
    public static bool IsGuideScene(string sceneName)
    {
        return sceneName == GuideSceneName;
    }

    // Loading a save jumps to another scene, which must not count as clearing a floor.
    public static void SuppressNextClear()
    {
        if (Instance != null) Instance.suppressNextClear = true;
    }

    public static SaveData ToSaveData()
    {
        HintManager hm = Instance;
        return new SaveData
        {
            uniqueID = SaveId,
            posX = hm != null ? hm.floorSeconds : 0f,
            posY = Pace,
            posZ = HelpBias,
            isInteracted = hm != null && hm.usedHintThisFloor,
            isOpen = hm != null ? hm.viewedTier : 0,
            hintCooldown = hm != null ? hm.cooldownLeft : 0f
        };
    }

    public static void FromSaveData(SaveData data)
    {
        if (data == null) return;

        Pace = Mathf.Clamp(data.posY, 0.3f, 3f);
        HelpBias = Mathf.Clamp01(data.posZ);
        if (Instance != null)
        {
            Instance.floorSeconds = Mathf.Max(0f, data.posX);
            Instance.usedHintThisFloor = data.isInteracted;
            Instance.viewedTier = Mathf.Clamp(data.isOpen, 0, TierCount);
            Instance.cooldownLeft = Mathf.Clamp(data.hintCooldown, 0f, CooldownSeconds);
        }
    }

    private static bool IsPuzzleFloor(Scene scene)
    {
        return FindObjectsByType<GateController>().Any(o => o.gameObject.scene == scene)
            || FindObjectsByType<DoorController>().Any(o => o.gameObject.scene == scene)
            || FindObjectsByType<InteractableChest>().Any(o => o.gameObject.scene == scene)
            || FindObjectsByType<LeverController>().Any(o => o.gameObject.scene == scene)
            || FindObjectsByType<PlayerCodePin>().Any(o => o.gameObject.scene == scene);
    }

    // The floor's shadow timer is the designer's expected clear time.
    private static float FindPar(Scene scene)
    {
        TimedActivator timer = FindObjectsByType<TimedActivator>().FirstOrDefault(o => o.gameObject.scene == scene);
        return timer != null && timer.activationTime > 0f ? timer.activationTime : DefaultPar;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Going to the next floor (build index + 1) from a floor we were playing counts as clearing it.
        bool cleared = floorActive && !suppressNextClear && scene.buildIndex == floorBuildIndex + 1;
        if (cleared)
        {
            ApplyClear(floorSeconds, par, usedHintThisFloor);
        }
        suppressNextClear = false;

        floorActive = IsPuzzleFloor(scene);
        floorBuildIndex = scene.buildIndex;
        floorSeconds = 0f;
        par = FindPar(scene);
        viewedTier = 0;
        usedHintThisFloor = false;
        cooldownLeft = 0f;
        lastHintText = null;
        playerController = null;

        BindLegacyHint(scene);
        guidePending = IsGuideScene(scene.name) && PlayerPrefs.GetInt(GuideKey, 0) == 0;
        HideGuide();
        HideHint();
    }

    // Takes over the floor's original lightbulb button, so there is one hint system and not two.
    private void BindLegacyHint(Scene scene)
    {
        if (!FindLegacyHint(scene, out legacyButton, out legacyPanel, out authoredHint)) return;

        // The button used to appear after a fixed time. It now appears when the adaptive timing says a hint is ready.
        foreach (TimedActivator timer in FindObjectsByType<TimedActivator>(FindObjectsInactive.Include))
        {
            if (timer.gameObject.scene == scene && timer.objectToActivate == legacyButton) timer.enabled = false;
        }

        Button button = legacyButton.GetComponentInChildren<Button>(true);
        if (button != null) button.onClick.AddListener(OnLegacyButtonClicked);

        legacyButton.SetActive(false);
        if (legacyPanel != null) legacyPanel.SetActive(false);
    }

    private void OnLegacyButtonClicked()
    {
        // The button's own events open the old panel. Close it again at once: the hint shows in the hint panel.
        if (legacyPanel != null) legacyPanel.SetActive(false);
        RequestHint();
    }

    // ---------- Per frame ----------

    private bool PlayerIsFree
    {
        get
        {
            if (Time.timeScale <= 0f || SaveSlotsUI.IsOpen) return false;
            if (playerController == null && Time.unscaledTime >= nextPlayerSearch)
            {
                playerController = FindAnyObjectByType<PlayerController>();
                nextPlayerSearch = Time.unscaledTime + 1f;
            }
            return playerController != null && playerController.enabled;
        }
    }

    private int UnlockedTier()
    {
        float[] times = TierTimes(par, Pace, HelpBias);
        int tier = 0;
        for (int i = 0; i < times.Length; i++)
        {
            if (floorSeconds >= times[i]) tier = i + 1;
        }
        return tier;
    }

    private void Update()
    {
        bool free = PlayerIsFree;
        UpdateGuide(free);

        if (!floorActive) return;

        // Time spent reading the guide does not count towards hints.
        if (free && !guideOpen)
        {
            floorSeconds += Time.deltaTime;
            if (cooldownLeft > 0f) cooldownLeft = Mathf.Max(0f, cooldownLeft - Time.deltaTime);
        }

        int unlocked = UnlockedTier();
        bool panelShowing = panelRoot != null && panelRoot.activeSelf;

        // H that opened or closed the guide this frame must not also ask for a hint.
        bool askedForHint = free && !guideOpen && guideKeyFrame != Time.frameCount && !ShiftHeld && Input.GetKeyDown(KeyCode.H);
        if (askedForHint)
        {
            if (panelShowing) HideHint();
            else RequestHint();
            panelShowing = panelRoot != null && panelRoot.activeSelf;
        }

        if (panelShowing && (!free || Time.unscaledTime > panelUntil))
        {
            HideHint();
            panelShowing = false;
        }

        BadgeState badge = ClassifyBadge(unlocked, viewedTier, cooldownLeft, free, panelShowing);

        // Where the floor has the original lightbulb button, the button is the cue for a ready hint.
        // The text note is then only used for the countdown.
        BadgeState textBadge = badge;
        if (legacyButton != null)
        {
            bool showButton = badge == BadgeState.Ready;
            if (legacyButton.activeSelf != showButton) legacyButton.SetActive(showButton);
            if (legacyPanel != null && legacyPanel.activeSelf) legacyPanel.SetActive(false);
            if (showButton) textBadge = BadgeState.Hidden;
        }

        if (textBadge != BadgeState.Hidden || (badgeRoot != null && badgeRoot.activeSelf))
        {
            EnsureUI();
            badgeRoot.SetActive(textBadge != BadgeState.Hidden);
            if (textBadge == BadgeState.Ready)
            {
                badgeLabel.text = "Hint ready - press H";
                badgeGroup.alpha = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
            }
            else if (textBadge == BadgeState.Waiting)
            {
                badgeLabel.text = "Next hint in " + FormatTime(cooldownLeft);
                badgeGroup.alpha = 0.6f;
            }
        }
    }

    // Shows the hint for the current tier, or says why there is none yet.
    public void RequestHint()
    {
        EnsureUI();
        int unlocked = UnlockedTier();

        switch (ClassifyRequest(unlocked, cooldownLeft))
        {
            case HintRequest.NotYetUnlocked:
            {
                float wait = TierTimes(par, Pace, HelpBias)[0] - floorSeconds;
                string when = wait < 60f ? "less than a minute" : "about " + Mathf.CeilToInt(wait / 60f) + " minute" + (wait > 60f ? "s" : "");
                ShowPanel("No hint yet. Keep exploring: the first one will be ready in " + when + ".");
                break;
            }
            case HintRequest.OnCooldown:
            {
                // The last hint can be read again, but a new one has to wait.
                string countdown = "<size=70%><color=#B8A99A>Next hint in " + FormatTime(cooldownLeft) + "</color></size>";
                ShowPanel(string.IsNullOrEmpty(lastHintText)
                    ? "Hints need a short break. The next one is ready in " + FormatTime(cooldownLeft) + "."
                    : lastHintText + "\n" + countdown);
                break;
            }
            default:
            {
                // Hint 1 is the text written for the floor. Hints 2 and 3 are built from the state of the floor.
                int tier = ChooseTier(unlocked, viewedTier);
                usedHintThisFloor = true;
                viewedTier = Mathf.Max(viewedTier, tier);
                string body = tier == 1 && !string.IsNullOrEmpty(authoredHint) ? authoredHint : BuildHint(tier, SceneManager.GetActiveScene());
                lastHintText = "<b>Hint " + tier + " of " + TierCount + "</b>\n" + body;
                cooldownLeft = CooldownSeconds;
                ShowPanel(lastHintText);
                break;
            }
        }
    }

    // Shows a message in the hint panel for a few seconds.
    public void ShowMessage(string text)
    {
        EnsureUI();
        ShowPanel(text);
    }

    private void ShowPanel(string text)
    {
        panelText.text = text;
        panelRoot.SetActive(true);
        badgeRoot.SetActive(false);
        panelUntil = Time.unscaledTime + PanelSeconds;
    }

    private void HideHint()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (badgeRoot != null) badgeRoot.SetActive(false);
    }

    // ---------- Home guide ----------

    private static bool ShiftHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    private void UpdateGuide(bool free)
    {
        if (guidePending && free)
        {
            guidePending = false;
            PlayerPrefs.SetInt(GuideKey, 1);
            ShowGuide();
            return;
        }

        bool hPressed = Input.GetKeyDown(KeyCode.H);

        if (guideOpen)
        {
            if (hPressed || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape))
            {
                HideGuide();
                guideKeyFrame = Time.frameCount;
            }
            return;
        }

        // Shift+H opens the guide again anywhere. Plain H does it where there are no hints to ask for.
        if (free && hPressed && (ShiftHeld || !floorActive))
        {
            ShowGuide();
            guideKeyFrame = Time.frameCount;
        }
    }

    // Opens the guide that explains the hint system.
    public void ShowGuide()
    {
        EnsureGuideUI();
        guideCanvasRoot.SetActive(true);
        guideOpen = true;
    }

    public void HideGuide()
    {
        guideOpen = false;
        if (guideCanvasRoot != null) guideCanvasRoot.SetActive(false);
    }

    private void EnsureGuideUI()
    {
        if (guideCanvasRoot != null) return;

        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        guideCanvasRoot = new GameObject("HintGuideUI", typeof(RectTransform));
        guideCanvasRoot.transform.SetParent(transform, false);
        var guideCanvas = guideCanvasRoot.AddComponent<Canvas>();
        guideCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        guideCanvas.sortingOrder = 950;

        var scaler = guideCanvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        guideCanvasRoot.AddComponent<GraphicRaycaster>();

        GameObject panel = MakeBox("Panel", guideCanvasRoot.transform, new Color(0.13f, 0.11f, 0.10f, 0.97f), new Vector2(980f, 700f), Vector2.zero);
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 36, 36);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = MakeLayoutText(panel.transform, "Need a hint?", 48f, new Color(0.90f, 0.62f, 0.33f, 1f), TextAlignmentOptions.Center, 70f);
        title.fontStyle = FontStyles.Bold;

        TextMeshProUGUI body = MakeLayoutText(panel.transform,
            "Stuck on a floor? Press <b>H</b> to ask for a hint.\n\n" +
            "- Hints unlock the longer you spend on a floor. How soon depends on how quickly you have been clearing floors.\n" +
            "- There are " + TierCount + " levels, and each hint is more direct than the last.\n" +
            "- After you take a hint, you must wait " + Mathf.RoundToInt(CooldownSeconds / 60f) + " minute for the next one.\n" +
            "- When a hint is ready, the lightbulb button appears on the screen. Click it or press <b>H</b>. " +
            "On a floor without a lightbulb, a small \"Hint ready\" note shows at the bottom of the screen instead.",
            30f, Color.white, TextAlignmentOptions.TopLeft, 400f);
        body.enableAutoSizing = true;
        body.fontSizeMin = 22f;
        body.fontSizeMax = 30f;

        MakeLayoutText(panel.transform, "Press Shift+H on any floor to read this again.", 24f, new Color(0.72f, 0.66f, 0.60f, 1f), TextAlignmentOptions.Center, 36f);

        var buttonObject = new GameObject("GotItButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        buttonObject.AddComponent<LayoutElement>().preferredHeight = 74f;
        var buttonImage = buttonObject.GetComponent<Image>();
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        var colors = button.colors;
        Color normal = new Color(0.24f, 0.19f, 0.15f, 1f);
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.25f);
        colors.selectedColor = normal;
        button.colors = colors;
        button.onClick.AddListener(HideGuide);
        TextMeshProUGUI buttonText = MakeText(buttonObject.transform, 30f, Color.white);
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.text = "Got it";

        guideCanvasRoot.SetActive(false);
    }

    private static TextMeshProUGUI MakeLayoutText(Transform parent, string text, float size, Color color, TextAlignmentOptions alignment, float height)
    {
        TextMeshProUGUI label = MakeText(parent, size, color);
        label.text = text;
        label.alignment = alignment;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        var rect = (RectTransform)label.transform;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return label;
    }

    // ---------- Hint text ----------

    // Builds the hint for a tier (1 to 3) from the state of the floor, so it changes as the puzzle progresses.
    public static string BuildHint(int tier, Scene scene)
    {
        tier = Mathf.Clamp(tier, 1, TierCount);

        GameObject player = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault(p => p.scene == scene);
        Vector3 from = player != null ? player.transform.position : Vector3.zero;
        PlayerManager keys = FindObjectsByType<PlayerManager>().FirstOrDefault(p => p.gameObject.scene == scene);

        List<GateController> gates = FindObjectsByType<GateController>().Where(o => o.gameObject.scene == scene).ToList();
        List<DoorController> lockedDoors = FindObjectsByType<DoorController>().Where(o => o.gameObject.scene == scene && !o.IsOpen).ToList();
        List<InteractableChest> closedChests = FindObjectsByType<InteractableChest>().Where(o => o.gameObject.scene == scene && o.chestData != null && !o.chestData.isOpen).ToList();
        List<LeverController> doorLevers = FindObjectsByType<LeverController>().Where(o => o.gameObject.scene == scene && !o.isLeverActivated).ToList();
        List<Lever> exitLevers = FindObjectsByType<Lever>().Where(o => o.gameObject.scene == scene && !o.IsActivated).ToList();
        bool safeClosed = FindObjectsByType<PlayerCodePin>().Any(o => o.gameObject.scene == scene) && !PlayerCodePin.isSafeOpened;

        // 1. The exit is open.
        GateController openGate = gates.FirstOrDefault(g => g.IsOpen);
        if (openGate != null)
        {
            if (tier == 1) return "The exit gate is open.";
            if (tier == 2) return "The exit gate is " + Where(from, openGate.transform.position) + ".";
            return "The exit gate is " + Where(from, openGate.transform.position) + ". Press F at the gate to go on.";
        }

        // 2. A key that is in the player's hands and a locked door that takes it.
        if (keys != null)
        {
            foreach (DoorController.KeyType type in System.Enum.GetValues(typeof(DoorController.KeyType)))
            {
                if (KeyCount(keys, type) <= 0) continue;
                DoorController door = Nearest(lockedDoors.Where(d => d.requiredKeyType == type), from);
                if (door == null) continue;

                string name = type.ToString().ToLowerInvariant();
                if (tier == 1) return "You are carrying a " + name + " key, and a door on this floor takes it.";
                if (tier == 2) return "A " + name + " door is " + Where(from, door.transform.position) + ".";
                return "Use your " + name + " key on the " + name + " door that is " + Where(from, door.transform.position) + ". A key is used up when a door opens, so choose carefully.";
            }
        }

        // 3. Chests that are still closed.
        if (closedChests.Count > 0)
        {
            InteractableChest chest = Nearest(closedChests, from);
            bool someEmpty = closedChests.Any(c => c.chestData.chestKeyType == InteractableChestData.KeyType.None);
            if (tier == 1) return "Not every chest on this floor has been opened yet.";
            if (tier == 2) return closedChests.Count + (closedChests.Count == 1 ? " chest is" : " chests are") + " still closed. The nearest is " + Where(from, chest.transform.position) + ".";
            return "Check the closed chests, starting with the one that is " + Where(from, chest.transform.position) + "." + (someEmpty ? " Some chests may be empty, so don't count on every one." : "");
        }

        // 4. A safe that has not been opened.
        if (safeClosed)
        {
            GameObject safe = FindObjectsByType<GameObject>().FirstOrDefault(g => g.scene == scene && g.name == "Safe");
            CodePanel panel = FindObjectsByType<CodePanel>(FindObjectsInactive.Include).FirstOrDefault(p => p.gameObject.scene == scene);
            if (tier == 1) return "There is a safe on this floor that has not been opened.";
            if (tier == 2) return safe != null ? "The safe is " + Where(from, safe.transform.position) + ". It needs a code." : "The safe needs a code. Look for numbers written somewhere on this floor.";
            return panel != null && !string.IsNullOrEmpty(panel.correctPassword) ? "The safe code is " + panel.correctPassword + "." : "The safe needs a code: look for numbers written somewhere on this floor.";
        }

        // 5. Door levers nobody pulled.
        if (doorLevers.Count > 0)
        {
            LeverController lever = Nearest(doorLevers, from);
            if (tier == 1) return "Some levers on this floor have not been pulled yet.";
            if (tier == 2) return "The nearest lever that has not been pulled is " + Where(from, lever.transform.position) + ".";
            return "Pull the lever that is " + Where(from, lever.transform.position) + ". A lever opens some doors and closes others, so walk through the ones that opened.";
        }

        // 6. The lever that opens the exit.
        if (exitLevers.Count > 0 && gates.Count > 0)
        {
            Lever lever = Nearest(exitLevers, from);
            if (tier == 1) return "The way out is sealed. A lever somewhere on this floor opens the exit gate.";
            if (tier == 2) return "The lever for the exit gate is " + Where(from, lever.transform.position) + ".";
            return "The lever for the exit gate is " + Where(from, lever.transform.position) + ". Walk into it to open the gate.";
        }

        // 7. Locked doors, but no matching key and nothing left to open.
        if (lockedDoors.Count > 0)
        {
            string needed = string.Join(", ", lockedDoors.Select(d => d.requiredKeyType.ToString().ToLowerInvariant()).Distinct());
            if (tier == 1) return "Some doors are still locked and you do not have a matching key.";
            if (tier == 2) return "The locked doors need these keys: " + needed + ".";
            return "You still need keys of these types: " + needed + ". Look for items you have not picked up and notes you have not read.";
        }

        return "Explore every room, talk to anyone you meet and read the notes you find. Something on this floor is still unfinished.";
    }

    private static int KeyCount(PlayerManager keys, DoorController.KeyType type)
    {
        switch (type)
        {
            case DoorController.KeyType.General: return keys.keyCount;
            case DoorController.KeyType.Gate: return keys.keyGateCount;
            case DoorController.KeyType.Store: return keys.keyStoreCount;
            default: return keys.keySecretCount;
        }
    }

    private static T Nearest<T>(IEnumerable<T> items, Vector3 from) where T : Component
    {
        T best = null;
        float bestDistance = float.MaxValue;
        foreach (T item in items)
        {
            float distance = Vector2.Distance(from, item.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = item;
            }
        }
        return best;
    }

    // "nearby, toward the top right". Up is up on the screen.
    private static string Where(Vector3 from, Vector3 to)
    {
        Vector2 delta = to - from;
        string distance = delta.magnitude < 4f ? "very close" : delta.magnitude < 10f ? "nearby" : "far away";

        string[] directions = { "to the right", "toward the top right", "toward the top", "toward the top left", "to the left", "toward the bottom left", "toward the bottom", "toward the bottom right" };
        int sector = Mathf.RoundToInt(Mathf.Atan2(delta.y, delta.x) / (Mathf.PI / 4f));
        return distance + ", " + directions[((sector % 8) + 8) % 8];
    }

    // ---------- UI ----------

    private void EnsureUI()
    {
        if (canvas != null) return;

        var root = new GameObject("HintUI", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        panelRoot = MakeBox("HintPanel", root.transform, new Color(0.10f, 0.08f, 0.07f, 0.93f), new Vector2(1100f, 170f), new Vector2(0f, 150f));
        panelText = MakeText(panelRoot.transform, 30f, Color.white);
        panelText.alignment = TextAlignmentOptions.Center;
        panelText.enableAutoSizing = true;
        panelText.fontSizeMin = 20f;
        panelText.fontSizeMax = 32f;
        panelRoot.SetActive(false);

        badgeRoot = MakeBox("HintBadge", root.transform, new Color(0.24f, 0.19f, 0.15f, 0.95f), new Vector2(460f, 64f), new Vector2(0f, 150f));
        badgeLabel = MakeText(badgeRoot.transform, 28f, new Color(0.90f, 0.62f, 0.33f, 1f));
        badgeLabel.alignment = TextAlignmentOptions.Center;
        badgeLabel.text = "Hint ready - press H";
        badgeGroup = badgeRoot.AddComponent<CanvasGroup>();
        badgeGroup.blocksRaycasts = false;
        badgeGroup.interactable = false;
        badgeRoot.SetActive(false);
    }

    private static GameObject MakeBox(string name, Transform parent, Color color, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return go;
    }

    private static TextMeshProUGUI MakeText(Transform parent, float size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 12f);
        rect.offsetMax = new Vector2(-24f, -12f);
        return text;
    }
}
