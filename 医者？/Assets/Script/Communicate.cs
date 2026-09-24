using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class Communicate : MonoBehaviour
{
    public InputButton buttonScript;
    public info infoScript;
    public magic magicScript;

    public string userInput;

    public TextMeshProUGUI textUI;
    public ScrollRect logScrollRect;

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
            AppendLog("MPが足りません。");
            return;
        }

        output();

    }

    void output()
    {
        infoScript.Input(userInput, stage);

        var log = new StringBuilder("User Input: ");
        log.Append(userInput);

        bool hasOutput = false;
        bool isWaitingInput = false;

        while (infoScript.TryOutput(stage, out string reply, out isWaitingInput))
        {
            log.Append("\nReply: ");
            log.Append(reply);
            hasOutput = true;

            if (isWaitingInput)
            {
                break;
            }
        }

        if (!hasOutput)
        {
            log.Append("\nReply: ?");
        }

        buttonScript.isActiveinputfield = isWaitingInput || !hasOutput;
        AppendLog(log.ToString());
    }

    void AppendLog(string message)
    {
        if (!string.IsNullOrEmpty(textUI.text))
        {
            textUI.text += "\n\n";
        }

        textUI.text += message;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(textUI.rectTransform);
        logScrollRect.verticalNormalizedPosition = 0f;
    }

}
