using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The save and load screen: a list of slots plus the folder the saves are stored in.
// It builds itself when opened, so every scene can use it without any setup.
public class SaveSlotsUI : MonoBehaviour
{
    public enum Mode { Save, Load, NewGame }

    private class SlotRow
    {
        public Button slotButton;
        public TextMeshProUGUI slotLabel;
        public Button deleteButton;
        public TextMeshProUGUI deleteLabel;
        public float overwriteConfirmUntil;
        public float deleteConfirmUntil;
    }

    private static readonly Color PanelColor = new Color(0.13f, 0.11f, 0.10f, 0.98f);
    private static readonly Color ButtonColor = new Color(0.24f, 0.19f, 0.15f, 1f);
    private static readonly Color DangerColor = new Color(0.40f, 0.17f, 0.15f, 1f);
    private static readonly Color AccentColor = new Color(0.90f, 0.62f, 0.33f, 1f);
    private static readonly Color MutedColor = new Color(0.72f, 0.66f, 0.60f, 1f);
    private static readonly Color ErrorColor = new Color(0.97f, 0.50f, 0.45f, 1f);

    private static SaveSlotsUI instance;

    public static bool IsOpen => instance != null;

    private Mode mode;
    private Func<int, string> onChosen;
    private readonly SlotRow[] rows = new SlotRow[SaveSystem.SlotCount];
    private TMP_InputField folderInput;
    private TextMeshProUGUI statusText;

    // onChosen receives the slot number and returns an error message, or null when it worked.
    public static void Open(Mode mode, Func<int, string> onChosen)
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }

        var go = new GameObject("SaveSlotsUI");
        instance = go.AddComponent<SaveSlotsUI>();
        instance.Build(mode, onChosen);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // Confirmation prompts go back to normal if they are not clicked in time.
        for (int i = 0; i < rows.Length; i++)
        {
            SlotRow row = rows[i];
            bool expired = false;
            if (row.overwriteConfirmUntil > 0f && Time.unscaledTime > row.overwriteConfirmUntil)
            {
                row.overwriteConfirmUntil = 0f;
                expired = true;
            }
            if (row.deleteConfirmUntil > 0f && Time.unscaledTime > row.deleteConfirmUntil)
            {
                row.deleteConfirmUntil = 0f;
                expired = true;
            }
            if (expired) RefreshSlots();
        }
    }

    private void Close()
    {
        if (instance == this) instance = null;
        Destroy(gameObject);
    }

    private void Build(Mode mode, Func<int, string> onChosen)
    {
        this.mode = mode;
        this.onChosen = onChosen;

        EnsureEventSystem();

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // Expand keeps the whole 1920x1080 design area on screen, so panels are never cut off on wide screens.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();

        // Dark backdrop that also stops clicks reaching the game behind it.
        Image dim = CreateImage("Dim", transform, new Color(0f, 0f, 0f, 0.75f));
        Stretch(dim.rectTransform);

        Image panel = CreateImage("Panel", transform, PanelColor);
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1000f, 960f);

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 30, 30);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        string titleText = mode == Mode.Save ? "Save Game" : mode == Mode.Load ? "Load Game" : "New Game";
        TextMeshProUGUI title = CreateText(panel.transform, titleText, 48f, AccentColor, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        SetHeight(title.gameObject, 76f);

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = BuildSlotRow(panel.transform, i + 1);
        }

        Image divider = CreateImage("Divider", panel.transform, new Color(1f, 1f, 1f, 0.12f));
        SetHeight(divider.gameObject, 3f);

        TextMeshProUGUI folderLabel = CreateText(panel.transform, "Save folder", 26f, MutedColor, TextAlignmentOptions.MidlineLeft);
        SetHeight(folderLabel.gameObject, 36f);

        BuildFolderRow(panel.transform);

        statusText = CreateText(panel.transform, "", 24f, MutedColor, TextAlignmentOptions.Center);
        SetHeight(statusText.gameObject, 60f);

        Button close = CreateButton(panel.transform, "Close", ButtonColor, 30f, out TextMeshProUGUI closeLabel);
        closeLabel.text = "Close";
        closeLabel.alignment = TextAlignmentOptions.Center;
        close.onClick.AddListener(Close);
        SetHeight(close.gameObject, 74f);

        RefreshAll();
    }

    private SlotRow BuildSlotRow(Transform parent, int slot)
    {
        var rowObject = new GameObject("Slot " + slot, typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);
        SetHeight(rowObject, 118f);

        var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var row = new SlotRow();

        row.slotButton = CreateButton(rowObject.transform, "SlotButton", ButtonColor, 32f, out row.slotLabel);
        row.slotLabel.alignment = TextAlignmentOptions.MidlineLeft;
        row.slotLabel.margin = new Vector4(24f, 0f, 24f, 0f);
        row.slotButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        row.slotButton.onClick.AddListener(() => OnSlotClicked(slot));

        row.deleteButton = CreateButton(rowObject.transform, "DeleteButton", DangerColor, 26f, out row.deleteLabel);
        row.deleteLabel.alignment = TextAlignmentOptions.Center;
        row.deleteButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 170f;
        row.deleteButton.onClick.AddListener(() => OnDeleteClicked(slot));

        return row;
    }

    private void BuildFolderRow(Transform parent)
    {
        var rowObject = new GameObject("FolderRow", typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);
        SetHeight(rowObject, 68f);

        var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        folderInput = CreateFolderInput(rowObject.transform);
        folderInput.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        folderInput.onEndEdit.AddListener(OnFolderTyped);

        if (FolderPicker.CanBrowse)
        {
            Button browse = CreateButton(rowObject.transform, "Browse", ButtonColor, 26f, out TextMeshProUGUI browseLabel);
            browseLabel.text = "Browse...";
            browseLabel.alignment = TextAlignmentOptions.Center;
            browse.gameObject.AddComponent<LayoutElement>().preferredWidth = 190f;
            browse.onClick.AddListener(OnBrowseClicked);
        }

        Button reset = CreateButton(rowObject.transform, "Reset", ButtonColor, 26f, out TextMeshProUGUI resetLabel);
        resetLabel.text = "Default";
        resetLabel.alignment = TextAlignmentOptions.Center;
        reset.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;
        reset.onClick.AddListener(OnResetClicked);
    }

    private void OnSlotClicked(int slot)
    {
        SlotRow row = rows[slot - 1];
        SaveSlotInfo info = SaveSystem.GetSlotInfo(slot);

        // Replacing an existing save needs a second click. The slot this game already uses was confirmed at the start.
        bool needsConfirm = info.exists && (mode == Mode.NewGame || (mode == Mode.Save && slot != SaveSystem.ActiveSlot));
        if (needsConfirm && Time.unscaledTime > row.overwriteConfirmUntil)
        {
            row.overwriteConfirmUntil = Time.unscaledTime + 4f;
            string question = mode == Mode.NewGame ? "Start here? This save is overwritten when you save." : "Overwrite this save?";
            row.slotLabel.text = $"<b>Slot {slot}</b>   {question}\n<size=70%><color=#E0A060>Click again to confirm</color></size>";
            return;
        }

        string error = onChosen?.Invoke(slot);
        if (string.IsNullOrEmpty(error))
        {
            Close();
            return;
        }

        SetStatus(error, true);
        RefreshSlots();
    }

    private void OnDeleteClicked(int slot)
    {
        SlotRow row = rows[slot - 1];

        if (Time.unscaledTime > row.deleteConfirmUntil)
        {
            row.deleteConfirmUntil = Time.unscaledTime + 3f;
            row.deleteLabel.text = "Sure?";
            return;
        }

        row.deleteConfirmUntil = 0f;
        if (SaveSystem.DeleteSlot(slot, out string error))
        {
            SetStatus($"Slot {slot} deleted.");
        }
        else
        {
            SetStatus(error, true);
        }
        RefreshSlots();
    }

    private void OnBrowseClicked()
    {
        string picked = FolderPicker.Pick(SaveSystem.SaveFolder);
        if (!string.IsNullOrEmpty(picked))
        {
            ApplyFolder(picked);
        }
    }

    private void OnResetClicked()
    {
        SaveSystem.ResetFolder();
        RefreshAll();
        SetStatus("Using the default save folder.");
    }

    private void OnFolderTyped(string text)
    {
        if (string.Equals(text, SaveSystem.SaveFolder, StringComparison.OrdinalIgnoreCase)) return;
        ApplyFolder(text);
    }

    private void ApplyFolder(string path)
    {
        if (SaveSystem.TrySetFolder(path, out string error))
        {
            RefreshAll();
            SetStatus("Saves are now stored in that folder.");
        }
        else
        {
            RefreshAll();
            SetStatus(error, true);
        }
    }

    private void RefreshAll()
    {
        folderInput.SetTextWithoutNotify(SaveSystem.SaveFolder);
        RefreshSlots();

        if (mode == Mode.NewGame)
        {
            SetStatus("Pick the slot this game will save to.");
        }
        else if (mode == Mode.Load && !SaveSystem.AnySave())
        {
            SetStatus("No saves in this folder. Choose another folder to look for saves.");
        }
        else
        {
            SetStatus(SaveSystem.IsCustomFolder ? "Saves are stored in the folder above." : "Using the default save folder.");
        }
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            int slot = i + 1;
            SlotRow row = rows[i];
            SaveSlotInfo info = SaveSystem.GetSlotInfo(slot);

            if (!info.exists)
            {
                row.slotLabel.text = $"<b>Slot {slot}</b>   <color=#B8A99A>Empty</color>";
            }
            else if (info.damaged)
            {
                row.slotLabel.text = $"<b>Slot {slot}</b>   Damaged save\n<size=70%><color=#B8A99A>This file can't be loaded. You can delete it.</color></size>";
            }
            else
            {
                string tag = mode != Mode.NewGame && slot == SaveSystem.ActiveSlot ? "(this game)" : slot == SaveSystem.LastSlot ? "(last used)" : "";
                string last = tag.Length > 0 ? $"  <size=70%><color=#E0A060>{tag}</color></size>" : "";
                row.slotLabel.text = $"<b>Slot {slot}</b>   {SaveSystem.PrettySceneName(info.sceneName)}{last}\n<size=70%><color=#B8A99A>Saved {info.savedAt}</color></size>";
            }

            row.slotButton.interactable = mode != Mode.Load || (info.exists && !info.damaged);
            row.deleteButton.gameObject.SetActive(info.exists);
            row.deleteLabel.text = "Delete";
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        statusText.text = message;
        statusText.color = isError ? ErrorColor : MutedColor;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(Transform parent, string name, Color color, float fontSize, out TextMeshProUGUI label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
        colors.selectedColor = color;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.35f);
        button.colors = colors;

        label = CreateText(go.transform, "", fontSize, Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return button;
    }

    private static TMP_InputField CreateFolderInput(Transform parent)
    {
        var go = new GameObject("FolderInput", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image background = go.GetComponent<Image>();
        background.color = new Color(0.07f, 0.06f, 0.05f, 1f);

        // Stays inactive until the input field is fully set up.
        go.SetActive(false);

        var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        area.transform.SetParent(go.transform, false);
        Stretch(area.GetComponent<RectTransform>(), 14f);

        TextMeshProUGUI text = CreateText(area.transform, "", 24f, Color.white, TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);

        TextMeshProUGUI placeholder = CreateText(area.transform, "Type a folder path", 24f, new Color(1f, 1f, 1f, 0.35f), TextAlignmentOptions.MidlineLeft);
        placeholder.fontStyle = FontStyles.Italic;
        Stretch(placeholder.rectTransform);

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = background;
        input.textViewport = area.GetComponent<RectTransform>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;

        go.SetActive(true);
        return input;
    }

    private static void SetHeight(GameObject go, float height)
    {
        var element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
    }

    private static void Stretch(RectTransform rect, float padding = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }
}
