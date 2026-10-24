using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public bool isRange;
    public KeyCode interactKey;
    public UnityEvent interactAction;
    public DoorController doorController;

    void Update()
    {
        if (isRange && Input.GetKeyDown(interactKey))
        {
            Debug.Log("Interaction Key Pressed");
            interactAction.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerManager manager = collision.gameObject.GetComponent<PlayerManager>();
            if (manager != null)
            {
                if (!doorController.IsOpen)
                {
                    isRange = true;
                    manager.NotifyPlayer("Press 'F' to interact");
                    Debug.Log("Player in range");
                }
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerManager manager = collision.gameObject.GetComponent<PlayerManager>();
            if (manager != null)
            {
                isRange = false;
                manager.DeNotifyPlayer();
                Debug.Log("Player out of range");
            }
        }
    }
}
