using System.Collections;
using TMPro;
using UnityEngine;

public class InteractDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public string[] dialogueLines;
    public float wordSpeed;
    public GameObject continueButton;

    private int index;
    private bool playerIsClose;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private bool isInputDisabled = false;

    public MonoBehaviour playerMovement;
    public GameObject npcSprite;

    [Header("UI Elements to Hide")]
    public GameObject inventoryButton;
    public GameObject pauseButton;

    public void TriggerDialogueFromButton()
    {
        if (!dialoguePanel.activeInHierarchy)
        {
            DisplayDialogue();
        }
        else if (!isTyping)
        {
            NextLine();
        }
    }

    void Update()
    {
        if (!isInputDisabled && Input.GetKeyDown(KeyCode.F) && playerIsClose)
        {
            if (dialoguePanel.activeInHierarchy)
            {
                NextLine();
            }
            else
            {
                isInputDisabled = true;
                DisplayDialogue();
            }
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            CompleteCurrentLine();
        }

        if (!isTyping && dialogueText.text == dialogueLines[index])
        {
            continueButton.SetActive(true);
        }
    }

    public void NextLine()
    {
        continueButton.SetActive(false);

        if (index < dialogueLines.Length - 1)
        {
            index++;
            dialogueText.text = "";
            DisplayDialogue();
        }
        else
        {
            isInputDisabled = false;
            ResetDialogue();
            if (npcSprite != null)
            {
                npcSprite.SetActive(true);
            }
        }
    }

    private void DisplayDialogue()
    {
        SetUIButtonsActive(false);
        dialoguePanel.SetActive(true);

        typingCoroutine = StartCoroutine(TypeText());
        continueButton.SetActive(false);

        PlayerManager manager = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerManager>();
        manager.DeNotifyPlayer();

        if (playerMovement != null)
        {
            playerMovement.enabled = false;

            PlayerController playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

            Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ResetDialogue()
    {
        dialogueText.text = "";
        index = 0;
        dialoguePanel.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        SetUIButtonsActive(true);
    }

    IEnumerator TypeText()
    {
        dialogueText.text = "";
        isTyping = true;

        foreach (char letter in dialogueLines[index].ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(wordSpeed);

            if (!isTyping)
                yield break;
        }

        isTyping = false;
        continueButton.SetActive(true);
    }

    private void CompleteCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        dialogueText.text = dialogueLines[index]; 
        isTyping = false; 
        continueButton.SetActive(true);
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

    private void SetUIButtonsActive(bool isActive)
    {
        if (inventoryButton != null)
        {
            inventoryButton.SetActive(isActive);
        }
        if (pauseButton != null)
        {
            pauseButton.SetActive(isActive);
        }
    }
}
