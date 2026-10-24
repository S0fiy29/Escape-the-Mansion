using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

public class EnterNPC : MonoBehaviour
{
    private bool isInRange = false;
    public bool hasTalkedWithNPC = false;
    private PlayerManager playerManager;
    public KeyCode interactKey = KeyCode.F;
    public UnityEvent interactAction;

    void Start()
    {
        playerManager = FindObjectOfType<PlayerManager>();
        Debug.Log(playerManager != null ? "PlayerManager is assigned." : "PlayerManager is null.");
    }

    void Update()
    {
        if (isInRange && hasTalkedWithNPC && Input.GetKeyDown(interactKey))
        {
            Open();
        }
    }

    public void Open()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = true;
            if (hasTalkedWithNPC)
            {
                playerManager.NotifyPlayer("Press 'F' to go to the next scene.");
            }
            else
            {
                playerManager.NotifyPlayer("You need to talk with the people first.");
            }

            Debug.Log("Player is in range to interact with the gate.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = false;
            playerManager.DeNotifyPlayer();
            Debug.Log("Player is out of range.");
        }
    }

    public void TalkedWithNPC()
    {
        hasTalkedWithNPC = true;
        Debug.Log("Player has talked with the NPC.");
    }
}
