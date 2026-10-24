using UnityEngine;
using UnityEngine.SceneManagement;

public class GateController : MonoBehaviour
{
    public Animator animator;
    public AudioClip openSound;
    public bool IsOpen { get; private set; } = false;
    public KeyCode interactKey = KeyCode.F;
    private bool isInRange = false;

    private PlayerManager playerManager;

    private void Start()
    {
        playerManager = FindObjectOfType<PlayerManager>();
    }

    // Used when loading a save: opens the gate without the sound or the "go to the next scene" prompt.
    public void RestoreOpen()
    {
        if (IsOpen) return;

        IsOpen = true;
        if (animator != null)
        {
            animator.SetBool("Open", true);
        }
    }

    public void OpenGate()
    {
        if (!IsOpen)
        {
            IsOpen = true;
            animator.SetBool("Open", true);
            AudioSource.PlayClipAtPoint(openSound, transform.position);
            Debug.Log("Gate opened!");
            
            if (isInRange)
            {
                playerManager.NotifyPlayer("Press 'F' to go to the next scene.");
            }
        }
    }

    private void Update()
    {
        if (isInRange && IsOpen && Input.GetKeyDown(interactKey))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = true;

            if (IsOpen)
            {
                playerManager.NotifyPlayer("Press 'F' to go to the next scene.");
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
}
