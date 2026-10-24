using UnityEngine;

// Dialogue behaviour is shared in DialogueBase. After the last line it swaps colliders and objects,
// hides the NPC and reveals the dialogue sprite.
public class NPC3 : DialogueBase
{
    public NPCManager npcManager;
    public Collider2D disable;
    public Collider2D enable;
    public GameObject toActive;
    public GameObject toDeactive;

    private void Start()
    {
        if (enable != null)
        {
            enable.enabled = false;
        }
    }

    protected override void OnDialogueFinished()
    {
        if (enable != null) enable.enabled = true;
        if (disable != null) disable.enabled = false;
        if (toActive != null) toActive.SetActive(true);
        if (toDeactive != null) toDeactive.SetActive(false);

        base.OnDialogueFinished();

        if (npcManager != null)
        {
            npcManager.StartFadeOutAndHideNPC();
        }

        DialogueSpriteController spriteController = FindAnyObjectByType<DialogueSpriteController>();
        if (spriteController != null)
        {
            spriteController.EnableSprite();
        }
    }
}