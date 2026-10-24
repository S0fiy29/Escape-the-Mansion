using UnityEngine;
using UnityEngine.UI;

using System.Collections;

public class Lever : MonoBehaviour
{
    public GateController gateToOpen;
    public Unity.Cinemachine.CinemachineVirtualCamera playerCam;
    public Unity.Cinemachine.CinemachineVirtualCamera gateCam;
    public Button continueButton;
    public float gateOpenDelay = 2.0f;
    public MonoBehaviour playerMovementScript;
    private bool isActivated = false;

    public bool IsActivated => isActivated;

    // Used when loading a save, so the lever and its gate cutscene don't play again.
    public void RestoreState(bool activated)
    {
        isActivated = activated;
    }

    private void Start()
    {
        if (gateToOpen == null)
        {
            Debug.LogError("GateController is not assigned to the Lever script on " + gameObject.name);
        }

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            Debug.Log("Player collided with lever.");
            if (gateToOpen != null)
            {
                SwitchToGateCamera();
                StartCoroutine(OpenGateWithDelay());
                isActivated = true;
            }
            else
            {
                Debug.LogError("GateController reference is missing in Lever script.");
            }
        }
    }

    private void SwitchToGateCamera()
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
                PlayerController playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

                Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            Debug.LogError("Gate camera is not assigned in Lever script on " + gameObject.name);
        }
    }

    private IEnumerator OpenGateWithDelay()
    {
        yield return new WaitForSeconds(gateOpenDelay); 
        gateToOpen.OpenGate();
        Debug.Log("Lever activated after delay, gate opened.");
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
                Debug.Log("Player movement enabled.");
            }
        }
        else
        {
            Debug.LogError("Player camera is not assigned in Lever script on " + gameObject.name);
        }
    }
}
