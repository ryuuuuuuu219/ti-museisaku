using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Communicate : MonoBehaviour
{
    public InputButton buttonScript;
    public info infoScript;

    public string userInput;

    public TextMeshProUGUI textUI;

    public int stage=0;
    public int phase=0;

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
        buttonScript.isActiveinputfield = infoScript.Input(userInput);
        textUI.text = "User Input: " + userInput+"\n"+
            infoScript.Output(stage, phase);
    }

}
