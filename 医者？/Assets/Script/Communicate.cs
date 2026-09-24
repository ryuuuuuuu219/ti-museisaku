using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Communicate : MonoBehaviour
{
    public InputButton buttonScript;
    public info infoScript;
    public magic magicScript;

    public string userInput;

    public TextMeshProUGUI textUI;

    public int stage=0;

    private void Start()
    {
        buttonScript.isActiveinputfield = true;
        output();
    }

    public void OnClick()
    {
        userInput = buttonScript.OnClick();

        if (!magicScript.TryConsumeMagic(userInput))
        {
            buttonScript.inputfield.text = userInput;
            buttonScript.isActiveinputfield = true;
            textUI.text = "MPが足りません。";
            return;
        }

        output();

    }

    void output()
    {
        buttonScript.isActiveinputfield = infoScript.Input(userInput, stage);
        textUI.text = "User Input: " + userInput+"\n"+
            "Reply: " + infoScript.Output(stage);
    }

}
