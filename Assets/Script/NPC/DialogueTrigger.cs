using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string line;
    }

    public GameObject npcDialoguePanel;
    public TextMeshProUGUI npcDialogueText;
    public GameObject playerDialoguePanel;
    public TextMeshProUGUI playerDialogueText;
    public NPCManager npcManager;

    public DialogueLine[] dialogue;
    private int index;

    public GameObject npcContButton;
    public GameObject playerContButton;
    public float wordSpeed;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    public MonoBehaviour playerMovement;
    public GameObject dialogueTriggerObject;

    [Header("UI Elements to Hide")]
    public GameObject inventoryButton;
    public GameObject pauseButton;
    
    private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player") && !isDialogueActive)
    {
        DisplayDialogue();
    }
}


    private void Update()
    {
        // Same flow as the other dialogue scripts: first F finishes the line, next F moves on.
        if (isDialogueActive && Input.GetKeyDown(KeyCode.F))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else
            {
                NextLine();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SkipTyping();
        }
    }

    public void NextLine()
    {
        if (index < dialogue.Length - 1)
        {
            index++;
            ResetText();
            DisplayDialogue();
        }
        else
        {
            EndDialogue();
            EnterNPC enterNPC = FindObjectOfType<EnterNPC>();

            if (enterNPC != null)
            {
                enterNPC?.TalkedWithNPC();
            }

            npcManager?.StartFadeOutAndHideNPC();
        }
    }

    public void SkipTyping()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            isTyping = false;

            if (npcDialoguePanel.activeInHierarchy)
            {
                npcDialogueText.text = dialogue[index].line;
                npcContButton.SetActive(true);
            }
            else if (playerDialoguePanel.activeInHierarchy)
            {
                playerDialogueText.text = dialogue[index].line;
                playerContButton.SetActive(true);
            }
        }
    }

    private void ResetText()
    {
        npcDialogueText.text = "";
        playerDialogueText.text = "";
    }

    public void DisplayDialogue()
    {
        SetUIButtonsActive(false);
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            PlayerController playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

            Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
        }

        ResetText();
        isDialogueActive = true;
        string speaker = dialogue[index].speaker;

        if (speaker == "Mr. Abraham" || speaker == "Alice")
        {
            playerDialoguePanel.SetActive(false);
            npcDialoguePanel.SetActive(true);
            npcContButton.SetActive(false);
            StartTyping(npcDialogueText, dialogue[index].line);
        }
        else if (speaker == "Hazil")
        {
            npcDialoguePanel.SetActive(false);
            playerDialoguePanel.SetActive(true);
            playerContButton.SetActive(false);
            StartTyping(playerDialogueText, dialogue[index].line);
        }
    }

    private void StartTyping(TextMeshProUGUI dialogueTextComponent, string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        isTyping = true;
        typingCoroutine = StartCoroutine(TypeText(dialogueTextComponent, text));
    }

    IEnumerator TypeText(TextMeshProUGUI dialogueTextComponent, string text)
    {
        dialogueTextComponent.text = "";
        // A wordSpeed of 0 in the Inspector made the text appear instantly, so fall back to the usual speed.
        float delay = wordSpeed > 0f ? wordSpeed : 0.05f;
        foreach (char letter in text.ToCharArray())
        {
            dialogueTextComponent.text += letter;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;

        if (npcDialoguePanel.activeInHierarchy)
        {
            npcContButton.SetActive(true);
        }
        else if (playerDialoguePanel.activeInHierarchy)
        {
            playerContButton.SetActive(true);
        }
    }

    private void EndDialogue()
    {
        npcDialoguePanel.SetActive(false);
        playerDialoguePanel.SetActive(false);
        isDialogueActive = false;

        if (dialogueTriggerObject != null)
        {
            dialogueTriggerObject.SetActive(false);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        SetUIButtonsActive(true);
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

        Debug.Log($"UI Buttons set to {(isActive ? "active" : "inactive")}");
    }
}
