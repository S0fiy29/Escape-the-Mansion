using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    public int keyCount;
    public int keyGateCount;
    public int keyStoreCount;
    public int keySecretCount;

    public TextMeshProUGUI totalKeysText;
    public TextMeshProUGUI gateKeysText;
    public TextMeshProUGUI storeKeysText;
    public TextMeshProUGUI secretKeysText;

    public TextMeshProUGUI notificationText;

    public void PickupKey() { keyCount++; UpdateKeyUI(); NotifyPlayer("Obtained a key!"); }
    public void UseKey() { if (keyCount > 0) { keyCount--; UpdateKeyUI(); } }

    public void PickupGateKey() { keyGateCount++; UpdateKeyUI(); NotifyPlayer("Obtained a gate key!"); }
    public void UseGateKey() { if (keyGateCount > 0) { keyGateCount--; UpdateKeyUI(); } }

    public void PickupStoreKey() { keyStoreCount++; UpdateKeyUI(); NotifyPlayer("Obtained a store key!"); }
    public void UseStoreKey() { if (keyStoreCount > 0) { keyStoreCount--; UpdateKeyUI(); } }

    public void PickupSecretKey() { keySecretCount++; UpdateKeyUI(); NotifyPlayer("Obtained a secret key!"); }
    public void UseSecretKey() { if (keySecretCount > 0) { keySecretCount--; UpdateKeyUI(); } }

    public void UpdateKeyUI()
    {
        // Texts are optional per scene, so a missing one is skipped quietly.
        if (totalKeysText != null) totalKeysText.text = $"Total Keys: {keyCount}";
        if (gateKeysText != null) gateKeysText.text = $"Gate Keys: {keyGateCount}";
        if (storeKeysText != null) storeKeysText.text = $"Store Keys: {keyStoreCount}";
        if (secretKeysText != null) secretKeysText.text = $"Secret Keys: {keySecretCount}";
    }

    
    public void RestoreKeys(SaveData saveData)
    {
        Debug.Log("RestoreKeys function called.");

        if (saveData != null)
        {
            keyCount = saveData.totalKeys;
            keyGateCount = saveData.gateKeys;
            keyStoreCount = saveData.storeKeys;
            keySecretCount = saveData.secretKeys;

            Debug.Log($"Restoring keys: Total={keyCount}, Gate={keyGateCount}, Store={keyStoreCount}, Secret={keySecretCount}");
            UpdateKeyUI();
        }
        else
        {
            Debug.LogWarning("SaveData is null. Unable to restore keys.");
        }
    }


    public void NotifyPlayer(string message)
    {
        if (notificationText != null) notificationText.text = message;
    }

    public void DeNotifyPlayer()
    {
        if (notificationText != null) notificationText.text = "";
    }
}
