using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneAutoLoader : MonoBehaviour
{
    private void Start()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RestoreStateAfterSceneLoad();
        }
    }
}
