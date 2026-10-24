using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// One save slot as it is written to disk. The header (savedAt, sceneName) lets the slot list show
// details without loading the game. The old single save file has the same saveData field, so it still reads.
[Serializable]
public class SaveFile
{
    public int version = 2;
    public string savedAt;
    public string sceneName;
    public List<SaveData> saveData = new List<SaveData>();
}

public class SaveSlotInfo
{
    public int slot;
    public bool exists;
    public bool damaged;
    public string sceneName;
    public string savedAt;
}

// Slot-based saves in a folder the player can choose. Slots are numbered from 1.
public static class SaveSystem
{
    public const int SlotCount = 3;

    private const string FolderKey = "saveFolderPath";
    private const string LastSlotKey = "lastSaveSlot";
    private const string LegacyFileName = "saveData.json";
    private const string PlayerId = "Player";

    private static bool legacyChecked;

    public static string DefaultFolder => Path.GetFullPath(Path.Combine(Application.persistentDataPath, "Saves"));

    public static bool IsCustomFolder => !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(FolderKey, ""));

    public static string SaveFolder
    {
        get
        {
            string custom = PlayerPrefs.GetString(FolderKey, "");
            return string.IsNullOrWhiteSpace(custom) ? DefaultFolder : custom;
        }
    }

    // The slot the current game belongs to: chosen at New Game, or set by loading or saving. 0 means none yet.
    public static int ActiveSlot { get; set; }

    // The slot that was saved or loaded last, so the list can point at it.
    public static int LastSlot
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(LastSlotKey, 1), 1, SlotCount);
        private set => PlayerPrefs.SetInt(LastSlotKey, Mathf.Clamp(value, 1, SlotCount));
    }

    public static string GetSlotPath(int slot)
    {
        return Path.Combine(SaveFolder, $"slot{slot}.json");
    }

    // Checks the folder can be created and written to before it is used for saves.
    public static bool TrySetFolder(string path, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Please enter a folder path.";
            return false;
        }

        try
        {
            path = Path.GetFullPath(path.Trim().Trim('"'));
            Directory.CreateDirectory(path);

            string probe = Path.Combine(path, ".write_test");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            error = "Can't use that folder: " + ex.Message;
            return false;
        }

        PlayerPrefs.SetString(FolderKey, path);
        PlayerPrefs.Save();
        return true;
    }

    public static void ResetFolder()
    {
        PlayerPrefs.DeleteKey(FolderKey);
        PlayerPrefs.Save();
    }

    public static SaveSlotInfo GetSlotInfo(int slot)
    {
        ImportLegacySave();

        var info = new SaveSlotInfo { slot = slot };
        string path = GetSlotPath(slot);
        if (!File.Exists(path)) return info;

        info.exists = true;
        try
        {
            SaveFile file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
            if (file == null || file.saveData == null || file.saveData.Count == 0)
            {
                info.damaged = true;
                return info;
            }

            info.sceneName = !string.IsNullOrEmpty(file.sceneName) ? file.sceneName : FindSceneName(file.saveData);
            info.savedAt = !string.IsNullOrEmpty(file.savedAt) ? file.savedAt : File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            info.damaged = true;
        }
        return info;
    }

    public static bool AnySave()
    {
        for (int slot = 1; slot <= SlotCount; slot++)
        {
            SaveSlotInfo info = GetSlotInfo(slot);
            if (info.exists && !info.damaged) return true;
        }
        return false;
    }

    // Writes to a temporary file first, so a crash mid-write never destroys the previous save.
    public static bool SaveGame(int slot, List<SaveData> data, out string error)
    {
        error = null;
        try
        {
            ImportLegacySave();

            string path = GetSlotPath(slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var file = new SaveFile
            {
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                sceneName = FindSceneName(data),
                saveData = data
            };

            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(file));

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.Move(temp, path);

            LastSlot = slot;
            ActiveSlot = slot;
            return true;
        }
        catch (Exception ex)
        {
            error = "Could not save: " + ex.Message;
            Debug.LogError(error);
            return false;
        }
    }

    public static bool TryLoad(int slot, out SaveFile file, out string error)
    {
        file = null;
        error = null;
        try
        {
            ImportLegacySave();

            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                error = "This slot is empty.";
                return false;
            }

            file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
            if (file == null || file.saveData == null || file.saveData.Count == 0)
            {
                file = null;
                error = "This save file is damaged.";
                return false;
            }
            return true;
        }
        catch (ArgumentException ex)
        {
            // JsonUtility throws this for text that is not valid JSON.
            file = null;
            error = "This save file is damaged.";
            Debug.LogWarning($"Save slot {slot} could not be read: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            file = null;
            error = "Could not load: " + ex.Message;
            Debug.LogError(error);
            return false;
        }
    }

    public static bool DeleteSlot(int slot, out string error)
    {
        error = null;
        try
        {
            string path = GetSlotPath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return true;
        }
        catch (Exception ex)
        {
            error = "Could not delete: " + ex.Message;
            return false;
        }
    }

    // Loads a slot and enters its scene. The GameController in that scene restores the saved state.
    public static bool LoadSlotIntoGame(int slot, out string error)
    {
        if (!TryLoad(slot, out SaveFile file, out error)) return false;

        string scene = FindSceneName(file.saveData);
        if (string.IsNullOrEmpty(scene))
        {
            error = "This save has no player position.";
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            error = $"The scene '{scene}' in this save is missing from the game.";
            return false;
        }

        LastSlot = slot;
        ActiveSlot = slot;
        MenuController.LoadedSaveData = file.saveData;
        Time.timeScale = 1f;
        HintManager.SuppressNextClear();
        SceneManager.LoadScene(scene);
        return true;
    }

    // Turns "lobby3" into "Lobby 3" and "lvl2" into "Level 2" for the slot list.
    public static string PrettySceneName(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return "Unknown";

        string name = scene.StartsWith("lvl", StringComparison.OrdinalIgnoreCase) ? "Level" + scene.Substring(3) : scene;
        var text = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsDigit(name[i]) && char.IsLetter(name[i - 1])) text.Append(' ');
            text.Append(i == 0 ? char.ToUpperInvariant(name[i]) : name[i]);
        }
        return text.ToString();
    }

    private static string FindSceneName(List<SaveData> data)
    {
        SaveData player = data?.Find(d => d.uniqueID == PlayerId);
        return player != null ? player.currentScene : SceneManager.GetActiveScene().name;
    }

    // The old version kept one save in persistentDataPath. Copy it into slot 1 once so no progress is lost.
    private static void ImportLegacySave()
    {
        if (legacyChecked) return;
        legacyChecked = true;

        try
        {
            string legacy = Path.Combine(Application.persistentDataPath, LegacyFileName);
            string slotOne = Path.Combine(DefaultFolder, "slot1.json");
            if (!File.Exists(legacy) || File.Exists(slotOne)) return;

            Directory.CreateDirectory(DefaultFolder);
            File.Copy(legacy, slotOne);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not import the old save file: " + ex.Message);
        }
    }
}
