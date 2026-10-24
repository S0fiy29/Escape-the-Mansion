using UnityEngine;
using System;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] public string customUniqueID;
    public bool IsInteracted; 
    public bool isOpen;
    private DoorController doorController;


    public string uniqueID
    {
        get
        {
            return string.IsNullOrEmpty(customUniqueID) ? gameObject.name : customUniqueID;
        }
    }

    private void Awake()
    {
        if (string.IsNullOrEmpty(customUniqueID))
        {
            customUniqueID = gameObject.name;
            Debug.LogWarning($"No custom unique ID set for {gameObject.name}. Using object name as ID.");
        }

        doorController = GetComponent<DoorController>();
    }
}