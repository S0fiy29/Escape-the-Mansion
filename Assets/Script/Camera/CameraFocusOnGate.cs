using UnityEngine;


public class CameraFocusOnGate : MonoBehaviour
{
    public Unity.Cinemachine.CinemachineVirtualCamera gateCamera;
    public float focusDuration = 2f;
    private float timer;
    private bool isFocusing = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) 
        {
            isFocusing = true;
            timer = focusDuration;
            gateCamera.Priority = 10;
        }
    }

    void Update()
    {
        if (isFocusing)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                gateCamera.Priority = 0;
                isFocusing = false;
            }
        }
    }
}
