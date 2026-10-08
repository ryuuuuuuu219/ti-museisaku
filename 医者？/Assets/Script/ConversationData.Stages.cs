// ステージごとの会話配列。処理はConversationData.csに定義する。
public partial class ConversationData
{
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
}
