// Dialogue that starts by itself when the scene loads. GameController calls DisableDialogue when restoring a save.
public class GameStartDialogue2 : DialogueBase
{
    protected override bool UsesProximity => false;

    // The intro does not count as talking to an NPC.
    protected override void OnDialogueFinished() { }

    private void Start()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGameLoaded)
        {
            SaveData dialogueData = GameStateManager.Instance.SavedData
                ?.Find(data => data.uniqueID == "GameStartDialogue2");

            if (dialogueData != null && !dialogueData.isDialogueActive)
            {
                DisableDialogue();
                return;
            }
        }

        DisplayDialogue();
    }
}