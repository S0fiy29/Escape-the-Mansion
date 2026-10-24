using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueSpriteController : MonoBehaviour
{
    public GameObject spriteToEnable;

    public void EnableSprite()
    {
        if (spriteToEnable != null)
        {
            spriteToEnable.SetActive(true);
        }
    }
}
