using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageConversation",
    menuName = "Conversation/Stage")]
public class StageConversationData : ScriptableObject
{
    public int stage;
    public ConversationNode[] nodes = Array.Empty<ConversationNode>();
}

[Serializable]
public class ConversationNode
{
    public string nodeId;

    [TextArea(1, 3)]
    public string questionExample;

    [TextArea(2, 6)]
    public string text;

    public string[] keywords = Array.Empty<string>();
    public ConversationKeywordMatchType keywordMatchType;
    public string[] prerequisiteNodeIds = Array.Empty<string>();
    public bool isWaitingInput = true;
}

public enum ConversationKeywordMatchType
{
    None,
    Any,
    All,
}
