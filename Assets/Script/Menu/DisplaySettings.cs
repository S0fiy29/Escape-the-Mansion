using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Display and audio settings shared by the menu and by game start-up.
// Saved choices are applied before the first scene loads, so they hold even if the menu is never opened.
public static class DisplaySettings
{
    public const string VolumeKey = "masterVolume";
    public const string QualityKey = "masterQuality"; // index of the menu choice, not the Unity quality level
    public const string FullscreenKey = "masterFullscreen";
    public const string BrightnessKey = "masterBrightness";
    public const string ResolutionWidthKey = "masterResWidth";
    public const string ResolutionHeightKey = "masterResHeight";

    // The menu offers Low, Medium, High and Ultra. These are the Unity quality levels they stand for
    // (the project has Very Low, Low, Medium, High, Very High and Ultra).
    private static readonly int[] QualityLevels = { 1, 2, 3, 5 };

    public const int DefaultQualityChoice = 1; // Medium

    public static int LevelForChoice(int choice)
    {
        choice = Mathf.Clamp(choice, 0, QualityLevels.Length - 1);
        return Mathf.Clamp(QualityLevels[choice], 0, QualitySettings.names.Length - 1);
    }

    // The menu choice closest to a Unity quality level, without going above it.
    public static int ChoiceForLevel(int level)
    {
        int best = 0;
        for (int i = 0; i < QualityLevels.Length; i++)
        {
            if (QualityLevels[i] <= level) best = i;
        }
        return best;
    }

    public static void ApplyQuality(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
        // The lower quality levels turn vSync off, which makes laptops run flat out. Keep it on for every level.
        QualitySettings.vSyncCount = 1;
    }

    public static void ApplyFullscreen(bool fullscreen)
    {
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    // Screen.resolutions lists each size once per refresh rate on some laptops. Keep one entry per size.
    public static Resolution[] UniqueResolutions(IEnumerable<Resolution> all)
    {
        return all.GroupBy(r => (r.width, r.height))
                  .Select(g => g.OrderByDescending(r => r.refreshRateRatio.value).First())
                  .OrderBy(r => r.width).ThenBy(r => r.height)
                  .ToArray();
    }

    // Index of the entry that is the same size as the window, or the closest one.
    public static int ClosestResolution(Resolution[] list, int width, int height)
    {
        if (list == null || list.Length == 0) return 0;

        int best = 0;
        long bestDistance = long.MaxValue;
        for (int i = 0; i < list.Length; i++)
        {
            long dw = list[i].width - width, dh = list[i].height - height;
            long distance = dw * dw + dh * dh;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }
        return best;
    }

    public static void SetResolution(int width, int height)
    {
        Screen.SetResolution(width, height, Screen.fullScreenMode);
        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);
    }

    public static void ClearSavedResolution()
    {
        PlayerPrefs.DeleteKey(ResolutionWidthKey);
        PlayerPrefs.DeleteKey(ResolutionHeightKey);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySaved()
    {
        if (PlayerPrefs.HasKey(VolumeKey))
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey));
        }

        if (PlayerPrefs.HasKey(QualityKey))
        {
            ApplyQuality(LevelForChoice(PlayerPrefs.GetInt(QualityKey)));
        }
        else
        {
            QualitySettings.vSyncCount = 1;
        }

        BrightnessOverlay.Set(PlayerPrefs.GetFloat(BrightnessKey, 1f));

#if !UNITY_EDITOR
        if (PlayerPrefs.HasKey(FullscreenKey))
        {
            ApplyFullscreen(PlayerPrefs.GetInt(FullscreenKey, 1) == 1);
        }

        // Only a size this screen really supports is restored, so a laptop never gets a mode it cannot show.
        if (PlayerPrefs.HasKey(ResolutionWidthKey) && PlayerPrefs.HasKey(ResolutionHeightKey))
        {
            int width = PlayerPrefs.GetInt(ResolutionWidthKey), height = PlayerPrefs.GetInt(ResolutionHeightKey);
            if (Screen.resolutions.Any(r => r.width == width && r.height == height))
            {
                Screen.SetResolution(width, height, Screen.fullScreenMode);
            }
        }
#endif
    }
}

// Screen brightness for the whole game. 1 is neutral. Below 1 a dark layer is drawn over everything,
// above 1 a pale haze lifts the picture. It stays on top of every canvas, including menus.
public static class BrightnessOverlay
{
    public const float Min = 0.5f;
    public const float Max = 1.5f;

    private static GameObject root;
    private static Image image;

    public static Color ColorFor(float brightness)
    {
        brightness = Mathf.Clamp(brightness, Min, Max);
        if (brightness < 1f) return new Color(0f, 0f, 0f, (1f - brightness) / (1f - Min) * 0.6f);
        return new Color(1f, 1f, 1f, (brightness - 1f) / (Max - 1f) * 0.25f);
    }

    public static void Set(float brightness)
    {
        Color color = ColorFor(brightness);
        if (color.a <= 0.001f)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        Ensure();
        image.color = color;
        root.SetActive(true);
    }

    private static void Ensure()
    {
        if (root != null) return;

        root = new GameObject("BrightnessOverlay", typeof(RectTransform));
        Object.DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var layer = new GameObject("Layer", typeof(RectTransform), typeof(Image));
        layer.transform.SetParent(root.transform, false);
        image = layer.GetComponent<Image>();
        image.raycastTarget = false;

        var rect = (RectTransform)layer.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
