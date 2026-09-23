using UnityEngine;

[System.Serializable]
public class TextData
{
    public bool isLock_keyword;
    public bool isLock_phase;
    public bool isOutputed=false;
    public bool isWaitingInput;
    public int stage;
    public int phase;
    public string text;
    public string[] keyword = System.Array.Empty<string>();
    public int[] unlockPhase = System.Array.Empty<int>();
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
    
    public TextData[] textDataArray = System.Array.Empty<TextData>();
    int? replyID; 

    public bool Input(string userInput, int stage)
    {
        userInput ??= string.Empty;
        RefreshPhaseLocks();

        bool waiting = true;
        for (var i=textDataArray.Length-1;i>=0;i--)
        {
            TextData data = textDataArray[i];
            if (data.stage != stage || data.isOutputed)
            {
                continue;
            }
            bool allKeywordsPresent = true;
            if(data.keyword == null || data.keyword.Length==0)
            {
                continue;
            }
            if(data.isLock_phase)
            {
                continue;
            }
            foreach (string keyword in data.keyword)
            {
                if (userInput.Contains(keyword))
                {
                    if (data.unlockType==TextData.UnlockType.Keyword_any)
                    {
                        data.isLock_keyword = false;
                        if (!replyID.HasValue)
                        {
                            replyID = i;
                            waiting = data.isWaitingInput;
                        }
                    }
                }
                else
                {
                    allKeywordsPresent = false;
                }
            }
            if (data.unlockType==TextData.UnlockType.Keyword_all && allKeywordsPresent)
            {
                data.isLock_keyword = false;
                if (!replyID.HasValue)
                {
                    replyID = i;
                    waiting = data.isWaitingInput;
                }
            }
        }
        return waiting;
    }

    void RefreshPhaseLocks()
    {
        foreach (TextData data in textDataArray)
        {
            if (data.unlockPhase == null || data.unlockPhase.Length == 0)
            {
                data.isLock_phase = false;
                continue;
            }

            bool allRequiredPhasesOutputed = true;
            foreach (int requiredPhase in data.unlockPhase)
            {
                bool requiredPhaseFound = false;
                bool requiredPhaseOutputed = true;
                foreach (TextData candidate in textDataArray)
                {
                    if (candidate.stage != data.stage || candidate.phase != requiredPhase)
                    {
                        continue;
                    }

                    requiredPhaseFound = true;
                    if (!candidate.isOutputed)
                    {
                        requiredPhaseOutputed = false;
                        break;
                    }
                }

                if (!requiredPhaseFound || !requiredPhaseOutputed)
                {
                    allRequiredPhasesOutputed = false;
                    break;
                }
            }

            data.isLock_phase = !allRequiredPhasesOutputed;
        }
    }

    public string Output(int stage, int phase)
    {
        if (replyID.HasValue)
        {
            int index = replyID.Value;
            replyID = null;
            textDataArray[index].isOutputed = true;
            string text = textDataArray[index].text;
            RefreshPhaseLocks();
            return text;
        }
        foreach (TextData data in textDataArray)
        {
            if (data.stage == stage && data.phase == phase &&
                !data.isLock_keyword && !data.isLock_phase && !data.isOutputed)
            {
                data.isOutputed = true;
                string text = data.text;
                RefreshPhaseLocks();
                return text;
            }
        }
        return "?";
    }


    private void Awake()
    {
        textDataArray = ConcentrateArray(textDataArray, Data101());
        textDataArray = ConcentrateArray(textDataArray, Data102());
        textDataArray = ConcentrateArray(textDataArray, Data103());
        textDataArray = ConcentrateArray(textDataArray, Data104());
    }

    TextData[] ConcentrateArray(TextData[] A, TextData[] B)
    {
        TextData[] result = new TextData[A.Length + B.Length];
        A.CopyTo(result, 0);
        B.CopyTo(result, A.Length);
        return result;
    }

    TextData[] Data101()
    {
        TextData[] dataArray = new TextData[1];
        dataArray[0]=new TextData
        {
            isLock_keyword = false,//初期返答
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
            isLock_keyword = true,
            isWaitingInput = true,
            stage = 0,
            phase = 1,
            text = "熱いです",
            keyword = new string[] { "熱", "体温", },//「体温はどうだった？」を想定
            unlockType = TextData.UnlockType.Keyword_any
        };
        return dataArray;
    }

    TextData[] Data103()
    {
        TextData[] dataArray = new TextData[1];
        dataArray[0] = new TextData
        {
            isLock_keyword = true,
            isWaitingInput = true,
            stage = 0,
            phase = 2,
            text = "昨日からです",
            keyword = new string[] { "いつ", "から", "変", },//「いつから体調が悪い？」を想定
            unlockType = TextData.UnlockType.Keyword_any
        };
        return dataArray;
    }

    TextData[] Data104()
    {
        TextData[] dataArray = new TextData[1];
        dataArray[0] = new TextData
        {
            isLock_keyword = true,
            isLock_phase = true,
            isWaitingInput = true,
            stage = 0,
            phase = 3,
            text = "熱かったです",
            unlockPhase = new int[] { 2, }, //「昨日からです」の返答後に「昨日は何か変わったことした？」を想定
            keyword = new string[] { "何", "行動", "変", },//「昨日は何か変わったことした？」を想定
            unlockType = TextData.UnlockType.Keyword_any
        };
        return dataArray;
    }

}
