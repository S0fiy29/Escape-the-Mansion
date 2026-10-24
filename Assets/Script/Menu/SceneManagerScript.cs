using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    private void OnEnable()
    {
        EnableColliders();
    }

    public void EnableColliders()
    {
        Collider2D[] colliders = FindObjectsOfType<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
    }
    public void DisableCollidersAfterSave()
    {
        Collider2D[] colliders = FindObjectsOfType<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Interactable"))  
            {
                collider.enabled = false; 
            }
        }
    }
}
