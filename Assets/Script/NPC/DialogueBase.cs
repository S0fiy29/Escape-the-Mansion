using System.Collections;
using TMPro;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string line;
}

/// <summary>
/// Shared logic for the two-panel dialogues (one NPC panel, one player panel).
/// Controls are the same everywhere: F finishes the current line, then moves to the next one.
/// Space skips the typing. The continue buttons still work.
/// </summary>
public abstract class DialogueBase : MonoBehaviour
{
    private const string PlayerSpeaker = "Hazil";
    private const float DefaultWordSpeed = 0.05f;

    // NPC Dialogue Panel
    public GameObject npcDialoguePanel;
    public TextMeshProUGUI npcDialogueText;

    // Player Dialogue Panel
    public GameObject playerDialoguePanel;
    public TextMeshProUGUI playerDialogueText;

    public DialogueLine[] dialogue;

    public GameObject npcContButton;
    public GameObject playerContButton;
    public float wordSpeed = DefaultWordSpeed;
    public MonoBehaviour playerMovement;

    [Header("UI Elements to Hide")]
    public GameObject inventoryButton;
    public GameObject pauseButton;

    [System.NonSerialized] public bool playerIsClose;
    [System.NonSerialized] public bool isDialogueActive;

    protected bool isTyping;

    private int index;
    private Coroutine typingCoroutine;
    private TextMeshProUGUI currentText;
    private GameObject currentButton;
    private GameObject playerObject;

    // The panel, text and continue button a line is shown with.
    protected struct SpeakerUI
    {
        public GameObject panel;
        public TextMeshProUGUI text;
        public GameObject button;
    }

    // Dialogues that start by themselves (no walking up and pressing F) return false.
    protected virtual bool UsesProximity => true;

    // Anyone who is not the player speaks in the NPC panel, so a new character name never leaves the screen empty.
    protected virtual SpeakerUI GetSpeakerUI(string speaker)
    {
        if (speaker == PlayerSpeaker)
        {
            return new SpeakerUI { panel = playerDialoguePanel, text = playerDialogueText, button = playerContButton };
        }
        return new SpeakerUI { panel = npcDialoguePanel, text = npcDialogueText, button = npcContButton };
    }

    // Shows the given panel and hides the others. Override when a dialogue has more panels.
    protected virtual void ShowOnlyPanel(GameObject panel)
    {
        npcDialoguePanel.SetActive(npcDialoguePanel == panel);
        playerDialoguePanel.SetActive(playerDialoguePanel == panel);
    }

    protected virtual void ClearTexts()
    {
        npcDialogueText.text = "";
        playerDialogueText.text = "";
    }

    protected virtual void SetContinueButtons(bool isActive)
    {
        if (npcContButton != null) npcContButton.SetActive(isActive);
        if (playerContButton != null) playerContButton.SetActive(isActive);
    }

    // Called after the last line. By default the NPC tracker is told the player talked to them.
    protected virtual void OnDialogueFinished()
    {
        EnterNPC enterNPC = FindAnyObjectByType<EnterNPC>();
        if (enterNPC != null)
        {
            enterNPC.TalkedWithNPC();
        }
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            HandleInteractKey();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SkipTyping();
        }
    }

    private void HandleInteractKey()
    {
        if (UsesProximity)
        {
            if (!playerIsClose) return;

            if (!isDialogueActive)
            {
                DisplayDialogue();
                return;
            }
        }
        else if (!isDialogueActive)
        {
            return;
        }

        if (isTyping)
        {
            SkipTyping();
        }
        else
        {
            NextLine();
        }
    }

    // Used by a UI button that starts or advances the dialogue.
    public void TriggerDialogueFromButton()
    {
        if (!isDialogueActive)
        {
            DisplayDialogue();
        }
        else if (!isTyping)
        {
            NextLine();
        }
    }

    public void NextLine()
    {
        SetContinueButtons(false);

        if (dialogue != null && index < dialogue.Length - 1)
        {
            index++;
            DisplayDialogue();
        }
        else
        {
            DisableDialogue();
            OnDialogueFinished();
        }
    }

    public void SkipTyping()
    {
        if (!isTyping) return;

        StopTyping();

        if (currentText != null)
        {
            currentText.text = dialogue[index].line;
        }
        if (currentButton != null)
        {
            currentButton.SetActive(true);
        }
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }

    // Closes the dialogue and gives control back to the player.
    public void DisableDialogue()
    {
        StopTyping();
        ClearTexts();
        SetContinueButtons(false);
        index = 0;
        isDialogueActive = false;

        ShowOnlyPanel(null);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        SetUIButtonsActive(true);
    }

    protected void DisplayDialogue()
    {
        if (dialogue == null || dialogue.Length == 0)
        {
            Debug.LogWarning($"{name}: no dialogue lines assigned.", this);
            return;
        }

        isDialogueActive = true;
        SetUIButtonsActive(false);
        FreezePlayer();
        ClearTexts();
        SetContinueButtons(false);

        SpeakerUI ui = GetSpeakerUI(dialogue[index].speaker);
        ShowOnlyPanel(ui.panel);
        currentText = ui.text;
        currentButton = ui.button;

        StopTyping();
        isTyping = true;
        typingCoroutine = StartCoroutine(TypeText(dialogue[index].line));
    }

    private IEnumerator TypeText(string line)
    {
        currentText.text = "";
        // A wordSpeed of 0 in the Inspector would make the text appear instantly.
        float delay = wordSpeed > 0f ? wordSpeed : DefaultWordSpeed;

        foreach (char letter in line)
        {
            currentText.text += letter;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typingCoroutine = null;

        if (currentButton != null)
        {
            currentButton.SetActive(true);
        }
    }

    private void FreezePlayer()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObject != null)
        {
            PlayerManager manager = playerObject.GetComponent<PlayerManager>();
            if (manager != null)
            {
                manager.DeNotifyPlayer();
            }
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = false;

            if (playerObject != null)
            {
                Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }

    protected void SetUIButtonsActive(bool isActive)
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

        if (inventoryButton != null) inventoryButton.SetActive(isActive);
        if (pauseButton != null) pauseButton.SetActive(isActive);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerIsClose = true;
        PlayerManager manager = other.GetComponent<PlayerManager>();
        if (manager != null)
        {
            manager.NotifyPlayer("Press 'F' to interact");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerIsClose = false;
        PlayerManager manager = other.GetComponent<PlayerManager>();
        if (manager != null)
        {
            manager.DeNotifyPlayer();
        }
    }
}
