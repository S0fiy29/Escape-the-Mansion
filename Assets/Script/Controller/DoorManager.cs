using UnityEngine;
using System.Collections.Generic;

public class DoorManager : MonoBehaviour
{
    public GameObject[] doors; 
    public List<DoorController2> Doors;
    public string uniqueID;

    private void Awake()
    {
        Doors = new List<DoorController2>(FindObjectsOfType<DoorController2>());
        Debug.Log($"Initialized DoorManager with {Doors.Count} doors.");
    }

    public void ActivateDoors(int[] doorIndices)
    {
        foreach (int index in doorIndices)
        {
            if (index >= 0 && index < doors.Length)
            {
                DoorController2 doorController = doors[index].GetComponent<DoorController2>();
                if (doorController != null)
                {
                    doorController.OpenDoor();
                }
            }
        }
    }

    public void DeactivateDoors(int[] doorIndices)
    {
        foreach (int index in doorIndices)
        {
            if (index >= 0 && index < doors.Length)
            {
                DoorController2 doorController = doors[index].GetComponent<DoorController2>();
                if (doorController != null)
                {
                    doorController.CloseDoor();
                }
            }
        }
    }

    public List<SaveData> GetDoorStates()
    {
        List<SaveData> doorStates = new List<SaveData>();
        foreach (var door in Doors)
        {
            doorStates.Add(new SaveData
            {
                uniqueID = door.name, 
                isDoorOpen = door.isOpen
            });
        }
        return doorStates;
    }

    public void RestoreDoorStates(List<SaveData> savedStates)
    {
        foreach (var state in savedStates)
        {
            DoorController2 door = Doors.Find(d => d.name == state.uniqueID);
            if (door != null)
            {
                if (state.isDoorOpen)
                {
                    door.OpenDoor();
                }
                else
                {
                    door.CloseDoor();
                }
            }
        }
    }
}
