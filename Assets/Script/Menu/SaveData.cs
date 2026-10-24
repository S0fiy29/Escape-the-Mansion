using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string uniqueID;      
    public string currentScene;
    public float posX, posY, posZ;
    public bool isInteracted;
    public bool isDoorOpen;
    public bool isColliderEnabled;
    public int open;
    public int isOpen;
    public bool isDialogueActive;
    public int isActive;

    public float hintCooldown;

    public int totalKeys;
    public int gateKeys;
    public int storeKeys;
    public int secretKeys;
}
