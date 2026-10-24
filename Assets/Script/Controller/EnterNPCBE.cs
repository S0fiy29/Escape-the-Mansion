using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

public class EnterNPCBE : MonoBehaviour
{
    private bool isInRange = false;
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
        if (isInRange && Input.GetKeyDown(interactKey))
        {
            OpenSpecificScene(21);
        }
    }

    public void OpenSpecificScene(int sceneIndex)
    {
        Debug.Log($"Loading scene: {sceneIndex}");
        SceneManager.LoadScene(sceneIndex);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = true;
            playerManager.NotifyPlayer("Press 'F' to go to the next scene.");
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
