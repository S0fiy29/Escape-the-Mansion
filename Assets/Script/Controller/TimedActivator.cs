using UnityEngine;

public class TimedActivator : MonoBehaviour
{
    public GameObject objectToActivate; // The GameObject to activate
    public float activationTime = 300f; // Time in seconds (5 minutes)

    private float elapsedTime = 0f; // Timer to track elapsed time
    private bool isActivated = false; // Prevent multiple activations

    void Update()
    {
        if (!isActivated)
        {
            elapsedTime += Time.deltaTime; // Increment timer

            if (elapsedTime >= activationTime)
            {
                ActivateObject();
            }
        }
    }

    private void ActivateObject()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true); // Activate the GameObject
            isActivated = true; // Prevent further activation
            Debug.Log($"{objectToActivate.name} has been activated after {activationTime} seconds.");
        }
        else
        {
            Debug.LogWarning("No GameObject assigned to activate.");
        }
    }
}
