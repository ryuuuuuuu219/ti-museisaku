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
    public int[] unlockPhase;
    public UnlockType unlockType;

    public enum UnlockType
    {
        None,
        Keyword_all,
        Keyword_any,
        Unlocked_phases,
    }
}

public class info : MonoBehaviour
{
    
    public TextData[] textDataArray;
    int? replyID; 

    public bool Input(string userInput)
    {
        bool waiting = true;
        for (var i=textDataArray.Length-1;i>=0;i--)
        {
            TextData data = textDataArray[i];
            bool allKeywordsPresent = true;
            if(data.keyword.Length==0)
            {
                continue;
            }
            foreach (string keyword in data.keyword)
            {
                if (userInput.Contains(keyword))
                {
                    if (data.unlockType==TextData.UnlockType.Keyword_any)
                    {
                        data.isLock = false;
                        if(!data.isOutputed) replyID = System.Array.IndexOf(textDataArray, data);
                        waiting = data.isWaitingInput;
                    }
                }
                else
                {
                    allKeywordsPresent = false;
                }
            }
            if (data.unlockType==TextData.UnlockType.Keyword_all && data.isLock&&allKeywordsPresent)
            {
                data.isLock = false;
                if (!data.isOutputed) replyID = System.Array.IndexOf(textDataArray, data);
                waiting = data.isWaitingInput;
            }
        }
        return waiting;
    }

    public string Output(int stage, int phase)
    {
        if (replyID.HasValue)
        {
            int index = replyID.Value;
            replyID = null;
            textDataArray[index].isOutputed = true;
            return textDataArray[index].text;
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


    private void Awake()
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
            isLock = false,//初期返答
            isWaitingInput = true,
            stage = 0,
            phase = 0,
            text = "母を助けてください",
            keyword = new string[] { "", },//初期返答
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
