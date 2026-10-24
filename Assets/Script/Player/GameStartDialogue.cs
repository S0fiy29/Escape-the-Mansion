using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class GameStartDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;      
    public TextMeshProUGUI dialogueText;  
    public string[] dialogueLines;        
    public float wordSpeed;               
    public MonoBehaviour playerMovement;  

    private int index = 0;                
    public GameObject continueButton;     
    private Coroutine typingCoroutine;    
    private bool isTyping = false;

    [Header("UI Elements to Hide")]
    public GameObject inventoryButton;
    public GameObject pauseButton;        

    void Start()
    {

        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGameLoaded)
        {
            // Load saved state
            SaveData dialogueData = GameStateManager.Instance.SavedData
                ?.Find(data => data.uniqueID == "GameStartDialogue");
            
            if (dialogueData != null && !dialogueData.isDialogueActive)
            {
                DisableDialogue();
            }
            else
            {
                StartDialogue();
            }
        }
        else
        {
            StartDialogue();
        }

        if (!dialoguePanel.activeSelf && playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    void Update()
    {
        if (!isTyping && dialogueText.text == dialogueLines[index])
        {
            continueButton.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SkipTyping();
        }

        // F works like it does in the NPC dialogues: finish the line, then continue.
        if (dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.F))
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
    }

    public void NextLine()
    {
        continueButton.SetActive(false);

        if (index < dialogueLines.Length - 1)
        {
            index++;
            dialogueText.text = "";  

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            typingCoroutine = StartCoroutine(TypeDialogue());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeDialogue()
    {
        isTyping = true;
        continueButton.SetActive(false);  
        foreach (char letter in dialogueLines[index].ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(wordSpeed);
        }

        isTyping = false;
    }

    private void EndDialogue()
    {
        dialogueText.text = "";
        index = 0;
        dialoguePanel.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        continueButton.SetActive(false);
        SetUIButtonsActive(true);
    }

    public void SkipTyping()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            dialogueText.text = dialogueLines[index];  
            isTyping = false;  
            continueButton.SetActive(true); 
        }
    }

    private void StartDialogue()
    {
        SetUIButtonsActive(false);
        dialoguePanel.SetActive(true);
        typingCoroutine = StartCoroutine(TypeDialogue());

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }

    private void DisableDialogue()
    {
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
        index = 0;

        continueButton.SetActive(false);
        SetUIButtonsActive(true);
    }

    private void SetUIButtonsActive(bool isActive)
    {
        if (inventoryButton == null && pauseButton == null)
        {
            // Buttons not assigned on this component: use the scene's UIManager instead.
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.SetUIButtonsActive(isActive);
            }
            return;
        }

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
