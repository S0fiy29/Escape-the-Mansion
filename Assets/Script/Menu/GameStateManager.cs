using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public List<SaveData> SavedData;
    private bool isRestoringState = false;
    public bool IsGameLoaded { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadGameState(List<SaveData> saveData)
    {
        SavedData = saveData;
        isRestoringState = true;
        IsGameLoaded = true;
    }

    public void RestoreStateAfterSceneLoad()
    {
        if (SavedData != null && isRestoringState)
        {
            isRestoringState = false;

            GameController gameController = FindObjectOfType<GameController>();
            if (gameController != null)
            {
                StartCoroutine(gameController.RestoreGameState(SavedData));
            }
            else
            {
                Debug.LogError("GameController not found in the scene!");
            }
        }
    }

    public void ResetGameState()
    {
        IsGameLoaded = false;
    }
}
