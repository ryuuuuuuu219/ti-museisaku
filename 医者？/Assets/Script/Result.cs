using UnityEngine;
using UnityEngine.UI;

public class Result : MonoBehaviour
{
    public enum ResultType
    {
        Bad,
        Good
    }

    public ResultType resultType;
    public Button Button;
    public Text resultText;

    public void result(ResultType type)
    {
        resultType = type;
        switch (resultType)
        {
            case ResultType.Bad:
                resultText.text = "Bad End";
                Button.onClick.AddListener(toMain_bad);
                break;
            case ResultType.Good:
                resultText.text = "Good End";
                Button.onClick.AddListener(toMain_good);
                break;
            default:
                Debug.Log("Unknown Result");
                break;
        }
    }

    public void toMain_bad()
    {
        SceneController.LoadMainScene();
    }

    public void toMain_good()
    {
        SceneController.stageID++;
        SceneController.LoadMainScene();
    }
}
