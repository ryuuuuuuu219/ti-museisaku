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
        output();
    }

    public void OnClick()
    {
        userInput = buttonScript.OnClick();
        output();

    }

    void output()
    {
        buttonScript.isActiveinputfield = infoScript.Input(userInput, stage);
        textUI.text = "User Input: " + userInput+"\n"+
            "Reply: " + infoScript.Output(stage, phase);
    }

}
