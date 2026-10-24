using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject[] uiButtons;

    public void SetUIButtonsActive(bool isActive)
    {
        foreach (GameObject button in uiButtons)
        {
            if (button != null)
            {
                button.SetActive(isActive);
            }
        }
    }
}
