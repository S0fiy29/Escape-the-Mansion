using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using TMPro;

public class LeverController : MonoBehaviour
{
    public DoorManager doorManager;
    public int[] doorsToActivate; 
    public int[] doorsToDeactivate;
    public Unity.Cinemachine.CinemachineVirtualCamera playerCam;
    public Unity.Cinemachine.CinemachineVirtualCamera gateCam;
    public float gateOpenDelay = 5.0f;
    public Button continueButton;
    public bool isLeverActivated = false;
    public MonoBehaviour playerMovementScript;
    private bool playerInRange = false;
    private bool isProcessing = false;
    public TextMeshProUGUI notificationText;
    private void Start()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinueButtonPressed);
        }
        else
        {
            Debug.LogError("Continue button is not assigned in the Lever script on " + gameObject.name);
        }
    }

    // Used when loading a save. The doors are restored on their own, so only the lever's own on/off state is set,
    // otherwise the next pull would apply the same door changes again instead of reversing them.
    public void RestoreState(bool activated)
    {
        isLeverActivated = activated;
    }

    public void ToggleLever()
    {
        isLeverActivated = !isLeverActivated;
        
        if (isLeverActivated)
        {
            doorManager.ActivateDoors(doorsToActivate);   
            doorManager.DeactivateDoors(doorsToDeactivate);
        }
        else
        {
            doorManager.DeactivateDoors(doorsToActivate); 
            doorManager.ActivateDoors(doorsToDeactivate);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerManager manager = collision.gameObject.GetComponent<PlayerManager>();
            if (manager != null)
            {
                playerInRange = true;
                manager.NotifyPlayer("Press 'F' to interact");
                Debug.Log("Player in range");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerManager manager = collision.gameObject.GetComponent<PlayerManager>();
            if (manager != null)
            {
                playerInRange = false;
                manager.DeNotifyPlayer();
                Debug.Log("Player out of range");
            }
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F) && !isProcessing)
        {
            StartCoroutine(OpenGateWithDelay());
        }
    }

    private void SwitchCamera()
    {
        if (gateCam != null)
        {
            playerCam.gameObject.SetActive(false);
            gateCam.gameObject.SetActive(true);
            CameraManager.SwitchCamera(gateCam);

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }

            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = false;
            }
        }
        else
        {
            Debug.LogError("Gate camera is not assigned in Lever script on " + gameObject.name);
        }
    }

    private IEnumerator OpenGateWithDelay()
    {
        isProcessing = true;

        SetNotificationVisibility(false);

        SwitchCamera();

        yield return new WaitForSeconds(gateOpenDelay); 

        ToggleLever();

        isProcessing = false;
    }

    public void OnContinueButtonPressed()
    {
        if (playerCam != null)
        {
            playerCam.gameObject.SetActive(true);
            gateCam.gameObject.SetActive(false);
            CameraManager.SwitchCamera(playerCam);

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }

            SetNotificationVisibility(true);
        }
        else
        {
            Debug.LogError("Player camera is not assigned in Lever script on " + gameObject.name);
        }
    }

    public void SetNotificationVisibility(bool isVisible)
    {
        if (notificationText != null)
        {
            Color textColor = notificationText.color;
            textColor.a = isVisible ? 1f : 0f;
            notificationText.color = textColor;

            notificationText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError("Notification text is not assigned.");
        }
    }
}
