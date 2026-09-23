using UnityEngine;

[System.Serializable]
public class TextData
{
    public bool isLock;
    public bool isOutputed=false;
    public bool isWaitingInput;
    public int stage;
    public int phase;
    public string text;
    public string[] keyword;
    public UnlockType unlockType;

    public enum UnlockType
    {
        None,
        Keyword_all,
        Keyword_any,
    }
}

public class info : MonoBehaviour
{
    
    public TextData[] textDataArray;
    int? replyID; 

    public bool Input(string userInput)
    {
        bool waiting = false;
        for (var i=textDataArray.Length-1;i>=0;i--)
        {
            TextData data = textDataArray[i];
            bool allKeywordsPresent = true;
            foreach (string keyword in data.keyword)
            {
                if (userInput.Contains(keyword))
                {
                    if (data.unlockType==TextData.UnlockType.Keyword_any)
                    {
                        data.isLock = false;
                        replyID = System.Array.IndexOf(textDataArray, data);
                    }
                }
                else
                {
                    allKeywordsPresent = false;
                }
            }
            if (data.unlockType==TextData.UnlockType.Keyword_all && data.isLock)
            {
                data.isLock = !allKeywordsPresent;
            }
        }
        return waiting;
    }

    public string Output(int stage, int phase)
    {
        if (replyID.HasValue)
        {
            textDataArray[replyID.Value].isOutputed = true;
            replyID = null;
            return textDataArray[replyID.Value].text;
        }
        foreach (TextData data in textDataArray)
        {
            if (data.stage == stage && data.phase == phase && !data.isLock && !data.isOutputed)
            {
                data.isOutputed = true;
                return data.text;
            }
        }
        return "?";
    }


    private void Start()
    {
        textDataArray = ConcentrateArray(textDataArray, Data101());
        textDataArray = ConcentrateArray(textDataArray, Data102());
    }

    TextData[] ConcentrateArray(TextData[] A, TextData[] B)
    {
        TextData[] result = new TextData[A.Length + B.Length];
        A.CopyTo(result, 0);
        B.CopyTo(result, A.Length);
        return result;
    }

    string[] preset_QuestionInput()
    {
        return new string[] { "?", "？" };
    }

    string[] ArrayCombine(string[] A, string[] B)
    {
        string[] result = new string[A.Length + B.Length];
        A.CopyTo(result, 0);
        B.CopyTo(result, A.Length);
        return result;
    }   

    TextData[] Data101()
    {
        TextData[] dataArray = new TextData[1];
        dataArray[0]=new TextData
        {
            isLock = false,
            isWaitingInput = true,
            stage = 0,
            phase = 0,
            text = "母を助けてください",
            keyword = null,
            unlockType = TextData.UnlockType.None
        };
        return dataArray;
    }

    TextData[] Data102()
    {
        TextData[] dataArray = new TextData[1];
        dataArray[0] = new TextData
        {
            isLock = true,
            isWaitingInput = true,
            stage = 0,
            phase = 0,
            text = "熱かったです",
            keyword = new string[] { "熱", "体温", },
            unlockType = TextData.UnlockType.Keyword_any
        };
        return dataArray;
    }

}
