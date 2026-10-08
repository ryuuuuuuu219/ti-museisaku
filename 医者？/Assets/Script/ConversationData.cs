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

    public Conversation[] conversations_stage0;
    public Conversation[] conversations_stage1;

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
            if (conversation.ExclusionIDs.Any(id => replyedID.Contains(id)))
            {
                continue;

            }
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
                        }
                        Debug.Log("Added dialogue ID: " + conversation.dialogueID);

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
