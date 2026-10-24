using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;

public class CodePanel : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI codeText;
    public string correctPassword = "";
    string codeTextValue = "";

    void Update()
    {
        codeText.text = codeTextValue;

        // An empty password would match the empty entry and open the safe straight away.
        if (correctPassword.Length > 0 && codeTextValue == correctPassword)
        {
            PlayerCodePin.isSafeOpened = true;
        }

        if(codeTextValue.Length >= 4)
        {
            codeTextValue ="";
        }
    }

    public void AddDigit(string digit)
    {
        codeTextValue += digit;
    }
}
