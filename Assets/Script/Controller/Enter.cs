using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

public class Enter : MonoBehaviour
{
    private bool isInRange = false;
    private PlayerManager playerManager;
    public KeyCode interactKey = KeyCode.F;
    public UnityEvent interactAction;

    void Start()
    {
        playerManager = FindObjectOfType<PlayerManager>();
    }

    void Update()
    {
        if (isInRange && Input.GetKeyDown(interactKey))
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
            playerManager.NotifyPlayer("Press 'F' to go to the next scene.");
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
}
