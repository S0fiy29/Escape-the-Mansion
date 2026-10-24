using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerHidden : MonoBehaviour
{
    private bool playerIsClose;
    public GameObject targetObject;
    public KeyCode keyToPress = KeyCode.F;

    void Update()
    {
        if (Input.GetKeyDown(keyToPress) && targetObject != null)
        {
            targetObject.SetActive(false);
            Debug.Log($"GameObject {targetObject.name} has been deactivated.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerManager manager = other.gameObject.GetComponent<PlayerManager>();
            playerIsClose = true;
            manager.NotifyPlayer("Press 'F' to interact");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerManager manager = other.gameObject.GetComponent<PlayerManager>();
            playerIsClose = false;
            manager.DeNotifyPlayer();
        }
    }
}
