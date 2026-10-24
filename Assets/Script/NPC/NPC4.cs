using TMPro;
using UnityEngine;

// Dialogue behaviour is shared in DialogueBase. This one has a second NPC panel (Elana, Cecilia)
// next to the first one (Mr. Jacob, Alice), and fades the NPC back in at the end.
public class NPC4 : DialogueBase
{
    public GameObject npcDialoguePanel2;
    public TextMeshProUGUI npcDialogueText2;
    public GameObject npcContButton2;

    public NPCManager npcManager;
    public GameObject objectToEnable;
    public GameObject Light;
    public Collider2D disable;

    protected override SpeakerUI GetSpeakerUI(string speaker)
    {
        if (speaker == "Elana" || speaker == "Cecilia")
        {
            return new SpeakerUI { panel = npcDialoguePanel2, text = npcDialogueText2, button = npcContButton2 };
        }
        return base.GetSpeakerUI(speaker);
    }

    protected override void ShowOnlyPanel(GameObject panel)
    {
        base.ShowOnlyPanel(panel);
        npcDialoguePanel2.SetActive(npcDialoguePanel2 == panel);
    }

    protected override void ClearTexts()
    {
        base.ClearTexts();
        npcDialogueText2.text = "";
    }

    protected override void SetContinueButtons(bool isActive)
    {
        base.SetContinueButtons(isActive);
        if (npcContButton2 != null)
        {
            npcContButton2.SetActive(isActive);
        }
    }

    protected override void OnDialogueFinished()
    {
        base.OnDialogueFinished();

        if (playerIsClose)
        {
            PlayerManager manager = FindAnyObjectByType<PlayerManager>();
            if (manager != null)
            {
                manager.NotifyPlayer("Press 'F' to interact");
            }
        }

        if (npcManager != null) npcManager.StartFadeInAndShowNPC();
        if (disable != null) disable.enabled = false;
        if (objectToEnable != null) objectToEnable.SetActive(true);
        if (Light != null) Light.SetActive(true);
    }
}