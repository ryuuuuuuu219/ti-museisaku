using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputButton : MonoBehaviour
{
    public TMP_InputField inputfield;
    public bool isActiveinputfield = false;

    private void Update()
    {
        if(!isActiveinputfield)
        {
            inputfield.gameObject.SetActive(false);
            gameObject.GetComponent<Button>().interactable = false;
        }
        else
        {
            inputfield.gameObject.SetActive(true);
            gameObject.GetComponent<Button>().interactable = true;
        }
    }

    public string OnClick()
    {
        if(inputfield.text == "")
        {
            return "";
        }
        isActiveinputfield = false;
        string text = inputfield.text;
        inputfield.text = "";
        return text;
    }
}
