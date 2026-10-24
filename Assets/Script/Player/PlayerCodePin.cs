using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCodePin : MonoBehaviour
{
    [SerializeField]
    GameObject codePanel, closedSafe, OpenedSafe;
    public static bool isSafeOpened = false;

    void Start()
    {
        // The flag is static, so without this reset it stayed true from an earlier scene and every later safe started open.
        // A loaded save sets it again after the scene has started.
        isSafeOpened = false;

        codePanel.SetActive(false);
        closedSafe.SetActive(true);
        OpenedSafe.SetActive(false);
    }

    void Update()
    {
        
        if(isSafeOpened)
        {
            codePanel.SetActive(false);
            closedSafe.SetActive(false);
            OpenedSafe.SetActive(true);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.gameObject.name.Equals("Safe") && !isSafeOpened)
        {
            codePanel.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if(col.gameObject.name.Equals("Safe"))
        {
            codePanel.SetActive(false);
        }
    }


}
