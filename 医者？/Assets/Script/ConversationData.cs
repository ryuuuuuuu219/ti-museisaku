using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class RequireGroup
{
    public int[] IDs;
}

[System.Serializable]
public class Conversation
{
    public enum Character_way
    {
        Left,
        Right
    }
    public enum state
    {
        Normal,
        Angry,
        Sad,
        Happy
    }
    public enum phase
    {
        Interview, Examination, Treatment
    }
    public int dialogueID;
    public string characterName;
    public state characterState;
    public string dialogue;
    public string[] keywords;           //論理和で検索するためのキーワード
    public Character_way characterWay;
    public phase conversationPhase;
    public RequireGroup[] RequireGroups;
    public int[] ExclusionIDs;
    public bool isForbidden;
    public bool isLock;
}

public class ConversationData : MonoBehaviour
{
    public List<int> replyIDqueue = new List<int>();
    public HashSet<int> replyedID = new HashSet<int>();

    #region stage0
    public Conversation[] conversations_stage0 = new Conversation[]
{
    new Conversation {
        dialogueID = 1,
        dialogue = "ノード1",
        keywords = new[] { "A", "a" }
    },
    new Conversation {
        dialogueID = 2,
        dialogue = "ノード2",
        keywords = new[] { "B", "b" }
    },
    new Conversation {
        dialogueID = 3,
        dialogue = "ノード3",
        keywords = new[] { "C", "c" },
        RequireGroups = new[] {
            new RequireGroup { IDs = new[] { 2 } }
        }
    },
    new Conversation {
        dialogueID = 4,
        dialogue = "ノード4",
        keywords = new[] { "D", "d" },
        isForbidden = true
    },
    new Conversation {
        dialogueID = 5,
        dialogue = "ノード5",
        keywords = new[] { "E", "e" },
        RequireGroups = new[] {
            new RequireGroup { IDs = new[] { 2 } }
        }
    },
    new Conversation {
        dialogueID = 6,
        dialogue = "ノード6",
        keywords = new[] { "F", "f" },
        RequireGroups = new[] {
            new RequireGroup { IDs = new[] { 3, 5 } }
        }
    },
    new Conversation {
        dialogueID = 7,
        dialogue = "ノード7",
        keywords = new[] { "G", "g" },
        RequireGroups = new[] {
            new RequireGroup { IDs = new[] { 6 } }
        }
    },
    new Conversation {
        dialogueID = 8,
        dialogue = "ノード8",
        keywords = new[] { "H", "h" },
        RequireGroups = new[] {
            new RequireGroup { IDs = new[] { 6 } }
        },
        ExclusionIDs = new[] { 9 }
    },
    new Conversation {
        dialogueID = 9,
        dialogue = "ノード9",
        keywords = new[] { "I", "i" },
        RequireGroups = new[] {
            new RequireGroup { IDs = new[] { 6 } }
        },
        ExclusionIDs = new[] { 8 }
    },
    new Conversation {
        dialogueID = 10,
        dialogue = "ノード10",
        keywords = new[] { "J", "j" },
        RequireGroups = new[] {
            new RequireGroup { IDs = new[] { 7, 8 } },
            new RequireGroup { IDs = new[] { 7, 9 } }
        }
    },
    new Conversation {
        dialogueID = 11,
        dialogue = "ノード11",
        keywords = new[] { "K", "k" },
        RequireGroups = new[] {
            new RequireGroup { IDs = new[] { 10 } }
        }
    }
};
    #endregion
    #region stage1
    public Conversation[] conversations_stage1;
    #endregion

    public Conversation[] GetConversationsByStage(int stage)
    {
        switch (stage)
        {
            case 0:
                return conversations_stage0;
            case 1:
                return conversations_stage1;
            default:
                Debug.LogError("Invalid stage number: " + stage);
                return null;
        }
    }

    public Conversation GetConversationByID(int dialogueID, int stage)
    {
        Conversation[] conversations = GetConversationsByStage(stage);
        if (conversations == null)
        {
            Debug.LogError("No conversations found for stage: " + stage);
            return null;
        }
        foreach (Conversation conversation in conversations)
        {
            if (conversation.dialogueID == dialogueID)
            {
                return conversation;
            }
        }
        Debug.LogError("No conversation found with dialogueID: " + dialogueID + " in stage: " + stage);
        return null;
    }

    public void CheckingInput(string input, int stage)
    {

        Conversation[] conversations = GetConversationsByStage(stage);
        if (conversations == null)
        {
            Debug.LogError("No conversations found for stage: " + stage);
            return;
        }

        foreach (Conversation conversation in conversations)
        {
            conversation.isLock = !CanUnlock(conversation);

            foreach (string keyword in conversation.keywords)
            {
                if (!conversation.isLock)
                {
                    if (input.Contains(keyword))
                    {
                        if (!replyedID.Contains(conversation.dialogueID) &&
                            !replyIDqueue.Contains(conversation.dialogueID))
                        {
                            replyIDqueue.Add(conversation.dialogueID);
                        Debug.Log("Added dialogue ID: " + conversation.dialogueID);
                        }

                    }
                }
            }
        }

    }
    public bool CanUnlock(Conversation conversation)
    {
        // 除外条件
        if (conversation.ExclusionIDs != null &&
            conversation.ExclusionIDs.Any(id => replyedID.Contains(id)))
        {
            return false;
        }

        // 前提条件なし
        if (conversation.RequireGroups == null ||
            conversation.RequireGroups.Length == 0)
        {
            return true;
        }

        // グループ間はOR、グループ内はAND
        return conversation.RequireGroups.Any(group =>
            group.IDs != null &&
            group.IDs.All(id => replyedID.Contains(id))
        );
    }

    public Conversation Reply(int stage)
    {
        while (replyIDqueue.Count > 0)
        {
            int id = replyIDqueue[0];
            replyIDqueue.RemoveAt(0);

            Conversation reply = GetConversationByID(id, stage);

            if (reply == null)
                continue;

            if (!CanUnlock(reply))
                continue;

            if (replyedID.Contains(id))
                continue;

            replyedID.Add(id);

            return reply;
        }

        return null;
    }

}
