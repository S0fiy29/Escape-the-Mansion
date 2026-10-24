using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum KeyType
    {
        General,
        Gate,
        Store,
        Secret
    }

    [Header("Door Properties")]
    [SerializeField] private bool isOpen = false;
    public bool IsOpen => isOpen;
    public KeyType requiredKeyType;
    [SerializeField] private string customUniqueID;

    public string uniqueID
    {
        get
        {
            return string.IsNullOrEmpty(customUniqueID) ? gameObject.name : customUniqueID;
        }
    }

    [Header("References")]
    public Animator animator;
    public AudioClip soundEffect;
    public Collider2D solidCollider;

    private bool isProcessing = false;

    public void Interact()
    {
        if (isProcessing || animator == null || soundEffect == null)
        {
            Debug.LogWarning($"DoorController on {gameObject.name} is not ready to process interactions.");
            return;
        }

        PlayerManager manager = FindObjectOfType<PlayerManager>();

        if (manager == null)
        {
            Debug.LogError($"PlayerManager not found.");
            return;
        }

        if (!isOpen)
        {
            switch (requiredKeyType)
            {
                case KeyType.General:
                    HandleGeneralKey(manager);
                    break;
                case KeyType.Gate:
                    HandleGateKey(manager);
                    break;
                case KeyType.Store:
                    HandleStoreKey(manager);
                    break;
                case KeyType.Secret:
                    HandleSecretKey(manager);
                    break;
                default:
                    manager.NotifyPlayer("Door is locked. Unknown key type.");
                    break;
            }
        }
    }

    private void HandleGeneralKey(PlayerManager manager)
    {
        if (manager.keyCount > 0)
        {
            OpenDoorProcess(manager, "Door opened with a general key.");
            manager.UseKey();
        }
        else
        {
            manager.NotifyPlayer("Door is locked. Find a general key.");
        }
    }

    private void HandleGateKey(PlayerManager manager)
    {
        if (manager.keyGateCount > 0)
        {
            OpenDoorProcess(manager, "Door opened with a gate key.");
            manager.UseGateKey();
        }
        else
        {
            manager.NotifyPlayer("A special gate key is required.");
        }
    }

    private void HandleStoreKey(PlayerManager manager)
    {
        if (manager.keyStoreCount > 0)
        {
            OpenDoorProcess(manager, "Door opened with a store key.");
            manager.UseStoreKey();
        }
        else
        {
            manager.NotifyPlayer("You need the store key.");
        }
    }

    private void HandleSecretKey(PlayerManager manager)
    {
        if (manager.keySecretCount > 0)
        {
            OpenDoorProcess(manager, "Door opened with a secret key.");
            manager.UseSecretKey();
        }
        else
        {
            manager.NotifyPlayer("This door requires a secret key.");
        }
    }

    private void OpenDoorProcess(PlayerManager manager, string message)
    {
        if (!isOpen)
        {
            isOpen = true;
            isProcessing = true;

            UpdateDoorState();

            AudioSource.PlayClipAtPoint(soundEffect, transform.position);
            manager.DeNotifyPlayer();
            Debug.Log(message);

            isProcessing = false;
        }
    }

    public SaveData GetSaveData()
    {
        return new SaveData
        {
            uniqueID = uniqueID,
            open = isOpen ? 1 : 0
        };
    }

    public void RestoreState(SaveData doorState)
    {
        if (doorState == null)
        {
            Debug.LogError($"RestoreState: doorState is null for {uniqueID}.");
            return;
        }

        if (isOpen != (doorState.open == 1))
        {
            isOpen = doorState.open == 1;
            UpdateDoorState();
        }
    }

    public void UpdateDoorState()
    {
        if (animator != null)
        {
            animator.SetBool("isOpened", isOpen);
        }

        if (solidCollider != null)
        {
            solidCollider.enabled = !isOpen;
        }
    }
}
