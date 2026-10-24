using UnityEngine;

public class SavableObject : MonoBehaviour
{
    [SerializeField] private string customUniqueID;

    public string UniqueID => string.IsNullOrEmpty(customUniqueID) ? gameObject.name : customUniqueID;

    private void Awake()
    {
        // Awake order between objects is not fixed, so fall back to a search if the manager is not ready yet.
        SavableManager manager = SavableManager.Instance != null ? SavableManager.Instance : FindAnyObjectByType<SavableManager>();
        if (manager != null)
        {
            manager.RegisterSavableObject(this);
        }
        else
        {
            Debug.LogError("SavableManager not found in the scene.");
        }
    }

    public SaveData GetSaveData()
    {
        return new SaveData
        {
            uniqueID = UniqueID,
            isActive = gameObject.activeSelf ? 1 : 0
        };
    }

    public void RestoreState(SaveData saveData)
    {
        if (saveData.uniqueID != UniqueID)
        {
            Debug.LogWarning($"Mismatched ID for {UniqueID}. Skipping restoration.");
            return;
        }

        gameObject.SetActive(saveData.isActive == 1);
    }
}
