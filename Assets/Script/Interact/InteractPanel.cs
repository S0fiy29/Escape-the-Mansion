using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractPanel : MonoBehaviour
{
    public GameObject Panel;
    public TextMeshProUGUI dialogueText;
    private bool playerIsClose;
    public MonoBehaviour playerMovement;
    private bool isInputDisabled = false;

    void Update()
    {
        PlayerManager manager = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerManager>();

        if (!isInputDisabled && Input.GetKeyDown(KeyCode.F) && playerIsClose)
        {
            if (Panel.activeInHierarchy)
            {
                ResetDialogue();
            }
            else
            {
                isInputDisabled = true;
                DisplayPanel();
            }
        }
    }

    private void DisplayPanel()
    {
        Panel.SetActive(true);

        PlayerManager manager = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerManager>();
        manager.DeNotifyPlayer();

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }

    public void Move()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        isInputDisabled = false;
    }

    private void ResetDialogue()
    {
        dialogueText.text = "";
        Panel.SetActive(false);
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
