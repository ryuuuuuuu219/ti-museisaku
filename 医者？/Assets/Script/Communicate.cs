using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Communicate : MonoBehaviour
{
    public InputButton buttonScript;

    public string userInput;

    public TextMeshProUGUI textUI;

    private void Start()
    {
        buttonScript.isActiveinputfield = true;
    }

    public void OnClick()
    {
        userInput = buttonScript.OnClick();
        Debug.Log("User Input: " + userInput);
        output();

    }

    void output()
    {
        textUI.text=Analysis.reply(0, userInput);
        buttonScript.isActiveinputfield = true;
    }

}
