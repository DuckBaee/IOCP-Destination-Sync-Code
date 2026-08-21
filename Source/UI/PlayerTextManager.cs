using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerTextManager : MonoBehaviour
{
    public TMP_InputField _InputField;

    void Start()
    {
        
    }

    public string GetText()
    {
        return _InputField.text;
    }

    public void ClickDisconnect()
    {
        NetworkManager.instance.OnDisconnectButtonClick();
    }
}