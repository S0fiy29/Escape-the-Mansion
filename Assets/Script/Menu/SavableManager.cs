using System.Collections.Generic;
using UnityEngine;

public class SavableManager : MonoBehaviour
{
    // The manager of the current scene, so savable objects don't have to search the scene for it.
    public static SavableManager Instance { get; private set; }

    public List<SavableObject> savableObjects = new List<SavableObject>();

    private void Awake()
    {
        Instance = this;
        RegisterAllSavableObjects();
    }

    private void RegisterAllSavableObjects()
    {
        savableObjects.Clear();
        foreach (SavableObject savable in FindObjectsByType<SavableObject>(FindObjectsInactive.Include))
        {
            RegisterSavableObject(savable);
        }
    }

    public void RegisterSavableObject(SavableObject obj)
    {
        if (obj != null && !savableObjects.Contains(obj))
        {
            savableObjects.Add(obj);
        }
    }

    public void SaveAllStates(List<SaveData> saveDataList)
    {
        foreach (var savable in savableObjects)
        {
            if (savable != null)
            {
                saveDataList.Add(savable.GetSaveData());
            }
        }
    }

    public void RestoreAllStates(List<SaveData> saveDataList)
    {
        var byId = new Dictionary<string, SavableObject>();
        foreach (var savable in savableObjects)
        {
            if (savable != null)
            {
                byId.TryAdd(savable.UniqueID, savable);
            }
        }

        // Saved entries of other kinds (boxes, doors, chests) are not savable objects, so they are skipped quietly.
        foreach (var saveData in saveDataList)
        {
            if (byId.TryGetValue(saveData.uniqueID, out SavableObject savable))
            {
                savable.RestoreState(saveData);
            }
        }
    }
}
