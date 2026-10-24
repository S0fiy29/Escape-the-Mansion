using UnityEngine;

public class BoxPosition : MonoBehaviour
{
    [SerializeField] private string customUniqueID;

    public string uniqueID
    {
        get
        {
            return string.IsNullOrEmpty(customUniqueID) ? gameObject.name : customUniqueID;
        }
    }

    public SaveData GetBoxState()
    {
        return new SaveData
        {
            uniqueID = uniqueID,
            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z
        };
    }

    public void RestoreBoxState(SaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError($"RestoreBoxState: saveData is null for {uniqueID}.");
            return;
        }

        transform.position = new Vector3(saveData.posX, saveData.posY, saveData.posZ);
    }
}
