using UnityEngine;

/// <summary>
/// Dialogue that ends the game: after the last line it pauses time and shows the end panel.
/// </summary>
public abstract class EndGameDialogue : DialogueBase
{
    public GameObject gameOverPanel;
    public GameObject button;

    protected override void OnDialogueFinished()
    {
        Time.timeScale = 0;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        if (button != null)
        {
            button.SetActive(false);
        }
    }
}
