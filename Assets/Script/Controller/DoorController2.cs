using UnityEngine;
using System.Collections;

public class DoorController2 : MonoBehaviour
{
    public Animator doorAnimator;
    public bool isOpen = false;
    public AudioClip soundEffect;
    private Collider2D solidCollider;
    private bool isInCooldown = false; 
    public float cooldownTime = 1.0f;

    private void Start()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            if (!col.isTrigger)
            {
                solidCollider = col;
                break;
            }
        }

        if (doorAnimator == null)
        {
            Debug.LogError("Animator is not assigned in " + gameObject.name);
            return;
        }

        SetInitialDoorState();
    }

    private void SetInitialDoorState()
    {
        if (isOpen)
        {
            doorAnimator.SetBool("isOpen", true);
            doorAnimator.SetBool("isClose", false);
            solidCollider.enabled = false;
        }
        else
        {
            doorAnimator.SetBool("isOpen", false);
            doorAnimator.SetBool("isClose", true);
            solidCollider.enabled = true; 
        }
    }

    public void OpenDoor()
    {
        if (!isOpen && !isInCooldown)
        {
            isOpen = true;
            doorAnimator.SetBool("isOpen", true);
            doorAnimator.SetBool("isClose", false);

            if (soundEffect != null)
            {
                AudioSource.PlayClipAtPoint(soundEffect, transform.position);
            }

            if (solidCollider != null)
            {
                solidCollider.enabled = false;
            }

            StartCoroutine(DoorCooldown());
        }
    }

    public void CloseDoor()
    {
        if (isOpen && !isInCooldown)
        {
            isOpen = false;
            doorAnimator.SetBool("isOpen", false);
             doorAnimator.SetBool("isClose", true);

            if (solidCollider != null)
            {
                solidCollider.enabled = true;
            }

            StartCoroutine(DoorCooldown());
        }
    }

    private IEnumerator DoorCooldown()
    {
        isInCooldown = true;
        yield return new WaitForSeconds(cooldownTime); 
        isInCooldown = false;
    }
}
