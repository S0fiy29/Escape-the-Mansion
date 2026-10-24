using UnityEngine;

public class InteractableChestData : MonoBehaviour
{
    [SerializeField] public string customUniqueID;
    public bool IsInteracted;
    public int isOpenInt;
    public KeyType chestKeyType;

    public enum KeyType
    {
        General,
        Gate,
        Store,
        Secret,
        None
    }

    public bool isOpen
    {
        get => isOpenInt == 1;
        set => isOpenInt = value ? 1 : 0;
    }

    public string uniqueID
    {
        get => string.IsNullOrEmpty(customUniqueID) ? gameObject.name : customUniqueID;
    }

    private void Awake()
    {
        if (string.IsNullOrEmpty(customUniqueID))
        {
            customUniqueID = gameObject.name;
            Debug.LogWarning($"No custom unique ID set for {gameObject.name}. Using object name as ID.");
        }
    }

    public void Interact()
    {
        if (!IsInteracted)
        {
            IsInteracted = true;
            ToggleOpenState();
        }
    }

    private void ToggleOpenState()
    {
        isOpen = true;
        Debug.Log($"{uniqueID} chest is now opened.");
    }

    public void RestoreState(bool interacted, int openInt)
    {
        IsInteracted = interacted;
        isOpenInt = openInt;
        Debug.Log($"Restored chest state: {uniqueID} - IsInteracted = {IsInteracted}, IsOpen = {isOpen}");
    }
}
