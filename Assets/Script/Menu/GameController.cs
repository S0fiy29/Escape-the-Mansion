using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    private const string PlayerId = "Player";
    private const string PlayerKeysId = "PlayerKeys";
    private const string LeverPrefix = "lever:";
    private const string FinalLeverPrefix = "finallever:";
    private const string GatePrefix = "gate:";
    private const string SafeId = "SafeState";

    public GameObject PausePanel;
    private GameObject player;
    private PlayerManager playerManager;
    public SavableManager savableManager;
    private UIManager uiManager;
    private bool isSaving = false;

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmationPrompt = null;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        playerManager = FindAnyObjectByType<PlayerManager>();

        if (MenuController.LoadedSaveData != null)
        {
            StartCoroutine(RestoreGameState(MenuController.LoadedSaveData));
            MenuController.LoadedSaveData = null;
        }
    }

    // Opens the slot list and saves into the slot the player picks.
    public void SaveGame()
    {
        SaveSlotsUI.Open(SaveSlotsUI.Mode.Save, SaveToSlot);
    }

    // Opens the slot list and loads the slot the player picks.
    public void LoadGame()
    {
        SaveSlotsUI.Open(SaveSlotsUI.Mode.Load, LoadFromSlot);
    }

    // Returns an error message for the slot screen, or null when the save worked.
    private string SaveToSlot(int slot)
    {
        if (isSaving) return "A save is already in progress.";

        isSaving = true;
        try
        {
            List<SaveData> saveDataList = GatherSaveData();
            if (saveDataList == null) return "Could not collect the game state (Save Manager missing in this scene).";

            if (!SaveSystem.SaveGame(slot, saveDataList, out string error)) return error;

            StartCoroutine(ConfirmationBox());
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return "Could not save: " + ex.Message;
        }
        finally
        {
            // Always release the flag, even if a chest or door throws while saving.
            isSaving = false;
        }
    }

    private string LoadFromSlot(int slot)
    {
        return SaveSystem.LoadSlotIntoGame(slot, out string error) ? null : error;
    }

    // Collects the player position, interactable objects, chests, doors and key data of this scene.
    public List<SaveData> GatherSaveData()
    {
        var saveDataList = new List<SaveData>();

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Y and Z are stored swapped, and swapped back when restoring.
            saveDataList.Add(new SaveData
            {
                uniqueID = PlayerId,
                currentScene = SceneManager.GetActiveScene().name,
                posX = player.transform.position.x,
                posY = player.transform.position.z,
                posZ = player.transform.position.y
            });
        }

        foreach (var box in FindObjectsByType<BoxPosition>())
        {
            saveDataList.Add(box.GetBoxState());
        }

        foreach (var door in FindObjectsByType<DoorController>())
        {
            saveDataList.Add(door.GetSaveData());
        }

        foreach (var door in FindObjectsByType<DoorController2>())
        {
            saveDataList.Add(new SaveData
            {
                uniqueID = door.gameObject.name,
                isDoorOpen = door.isOpen
            });
        }

        foreach (var chest in FindObjectsByType<InteractableChest>())
        {
            saveDataList.Add(chest.GetChestState());
        }

        // Puzzle state that was missing: door lever positions, the final lever, the exit gate and the safe.
        foreach (var lever in FindObjectsByType<LeverController>())
        {
            saveDataList.Add(new SaveData { uniqueID = LeverPrefix + SaveIds.PathOf(lever), isInteracted = lever.isLeverActivated });
        }

        foreach (var lever in FindObjectsByType<Lever>())
        {
            saveDataList.Add(new SaveData { uniqueID = FinalLeverPrefix + SaveIds.PathOf(lever), isInteracted = lever.IsActivated });
        }

        foreach (var gate in FindObjectsByType<GateController>())
        {
            saveDataList.Add(new SaveData { uniqueID = GatePrefix + SaveIds.PathOf(gate), isDoorOpen = gate.IsOpen });
        }

        if (FindAnyObjectByType<PlayerCodePin>() != null)
        {
            saveDataList.Add(new SaveData { uniqueID = SafeId, isInteracted = PlayerCodePin.isSafeOpened });
        }

        // Time on this floor and the player's hint profile, so a loaded game keeps its adaptive hint timing.
        if (HintManager.Instance != null)
        {
            saveDataList.Add(HintManager.ToSaveData());
        }

        if (playerManager != null)
        {
            saveDataList.Add(new SaveData
            {
                uniqueID = PlayerKeysId,
                totalKeys = playerManager.keyCount,
                gateKeys = playerManager.keyGateCount,
                storeKeys = playerManager.keyStoreCount,
                secretKeys = playerManager.keySecretCount,
            });
        }

        var manager = FindAnyObjectByType<SavableManager>();
        if (manager == null)
        {
            Debug.LogError("SavableManager not found in the scene.");
            return null;
        }

        var dialogue = FindAnyObjectByType<GameStartDialogue>();
        if (dialogue != null)
        {
            saveDataList.Add(new SaveData
            {
                uniqueID = "GameStartDialogue",
                isDialogueActive = dialogue.dialoguePanel.activeSelf
            });
        }

        manager.SaveAllStates(saveDataList);
        return saveDataList;
    }

    // Restores the game state, including player position, interactable objects, chests, and key data.
    public IEnumerator RestoreGameState(List<SaveData> saveDataList)
    {
        // Let the scene's objects finish starting up before changing them.
        yield return null;
        yield return null;

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SaveData playerData = saveDataList.Find(data => data.uniqueID == PlayerId);
            if (playerData != null)
            {
                player.transform.position = new Vector3(playerData.posX, playerData.posZ, playerData.posY);
            }
        }

        // Look every object up once by its ID, instead of searching the whole scene for each saved entry.
        var boxes = new Dictionary<string, BoxPosition>();
        foreach (var box in FindObjectsByType<BoxPosition>()) boxes.TryAdd(box.uniqueID, box);

        var doors = new Dictionary<string, DoorController>();
        foreach (var door in FindObjectsByType<DoorController>()) doors.TryAdd(door.uniqueID, door);

        var levers = new Dictionary<string, DoorController2>();
        foreach (var lever in FindObjectsByType<DoorController2>()) levers.TryAdd(lever.gameObject.name, lever);

        var chests = new Dictionary<string, InteractableChest>();
        foreach (var chest in FindObjectsByType<InteractableChest>())
        {
            if (chest.chestData != null) chests.TryAdd(chest.chestData.uniqueID, chest);
        }

        var leverControllers = new Dictionary<string, LeverController>();
        foreach (var lever in FindObjectsByType<LeverController>()) leverControllers.TryAdd(LeverPrefix + SaveIds.PathOf(lever), lever);

        var finalLevers = new Dictionary<string, Lever>();
        foreach (var lever in FindObjectsByType<Lever>()) finalLevers.TryAdd(FinalLeverPrefix + SaveIds.PathOf(lever), lever);

        var gates = new Dictionary<string, GateController>();
        foreach (var gate in FindObjectsByType<GateController>()) gates.TryAdd(GatePrefix + SaveIds.PathOf(gate), gate);

        var restoredDoors = new HashSet<string>();
        foreach (SaveData saveData in saveDataList)
        {
            string id = saveData.uniqueID;
            if (id == PlayerId || id == PlayerKeysId) continue;

            if (id == SafeId)
            {
                PlayerCodePin.isSafeOpened = saveData.isInteracted;
                continue;
            }

            if (id == HintManager.SaveId)
            {
                HintManager.FromSaveData(saveData);
                continue;
            }

            if (leverControllers.TryGetValue(id, out LeverController leverController))
            {
                leverController.RestoreState(saveData.isInteracted);
                continue;
            }

            if (finalLevers.TryGetValue(id, out Lever finalLever))
            {
                finalLever.RestoreState(saveData.isInteracted);
                continue;
            }

            if (gates.TryGetValue(id, out GateController gate))
            {
                if (saveData.isDoorOpen) gate.RestoreOpen();
                continue;
            }

            if (boxes.TryGetValue(id, out BoxPosition box))
            {
                box.RestoreBoxState(saveData);
            }

            // Only the first saved entry of an ID is used for doors.
            if (doors.TryGetValue(id, out DoorController door) && restoredDoors.Add(id))
            {
                door.RestoreState(saveData);
            }

            if (levers.TryGetValue(id, out DoorController2 lever))
            {
                if (saveData.isDoorOpen)
                {
                    lever.OpenDoor();
                }
                else
                {
                    lever.CloseDoor();
                }
            }

            if (chests.TryGetValue(id, out InteractableChest chest))
            {
                chest.RestoreChestState(saveData);
            }
        }

        var dialogue = FindAnyObjectByType<GameStartDialogue>();
        if (dialogue != null)
        {
            SaveData dialogueData = saveDataList.Find(data => data.uniqueID == "GameStartDialogue");
            if (dialogueData != null)
            {
                dialogue.dialoguePanel.SetActive(dialogueData.isDialogueActive);
                if (!dialogueData.isDialogueActive)
                {
                    dialogue.SkipTyping();

                    uiManager = FindAnyObjectByType<UIManager>();
                    if (uiManager != null)
                    {
                        uiManager.SetUIButtonsActive(true);
                    }

                    if (dialogue.playerMovement != null)
                    {
                        dialogue.playerMovement.enabled = true;
                    }
                }
            }
        }

        var dialogue2 = FindAnyObjectByType<GameStartDialogue2>();
        if (dialogue2 != null)
        {
            dialogue2.DisableDialogue();
        }

        var keysOwner = FindAnyObjectByType<PlayerManager>();
        SaveData keyData = saveDataList.Find(data => data.uniqueID == PlayerKeysId);
        if (keyData != null && keysOwner != null)
        {
            keysOwner.RestoreKeys(keyData);
        }

        var manager = FindAnyObjectByType<SavableManager>();
        if (manager != null)
        {
            manager.RestoreAllStates(saveDataList);
        }
    }

    // Handles the confirmation box display. Uses real time because the pause menu stops game time.
    public IEnumerator ConfirmationBox()
    {
        if (confirmationPrompt == null) yield break;

        confirmationPrompt.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        confirmationPrompt.SetActive(false);
    }

    // Pause, resume, and restart game functionality.
    public void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        PausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        PausePanel.SetActive(false);
    }

    public void GoToMenu() => SceneManager.LoadScene("MainMenu");
}
