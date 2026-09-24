using System;
using System.Collections.Generic;
using UnityEngine;

public class info : MonoBehaviour
{
    public StageConversationData[] stageConversationData = Array.Empty<StageConversationData>();

    private readonly HashSet<string> outputtedNodeIds = new();
    private ConversationNode replyNode;

    public bool Input(string userInput, int stage)
    {
        userInput ??= string.Empty;
        replyNode = null;

        StageConversationData stageData = FindStageData(stage);
        if (stageData == null)
        {
            return true;
        }

        for (int i = stageData.nodes.Length - 1; i >= 0; i--)
        {
            ConversationNode node = stageData.nodes[i];
            if (node == null || outputtedNodeIds.Contains(node.nodeId))
            {
                continue;
            }

            if (!ArePrerequisitesOutputted(node) || !MatchesKeywords(node, userInput))
            {
                continue;
            }

            replyNode = node;
            return node.isWaitingInput;
        }

        return true;
    }

    public string Output(int stage)
    {
        if (replyNode != null)
        {
            ConversationNode selectedNode = replyNode;
            replyNode = null;
            outputtedNodeIds.Add(selectedNode.nodeId);
            return selectedNode.text;
        }

        StageConversationData stageData = FindStageData(stage);
        if (stageData == null)
        {
            return "?";
        }

        foreach (ConversationNode node in stageData.nodes)
        {
            if (node == null || outputtedNodeIds.Contains(node.nodeId))
            {
                continue;
            }

            if (node.keywordMatchType != ConversationKeywordMatchType.None ||
                !ArePrerequisitesOutputted(node))
            {
                continue;
            }

            outputtedNodeIds.Add(node.nodeId);
            return node.text;
        }

        return "?";
    }

    public string[] GetAvailableKeywords(int stage)
    {
        StageConversationData stageData = FindStageData(stage);
        if (stageData == null || stageData.nodes == null)
        {
            return Array.Empty<string>();
        }

        var keywords = new List<string>();
        var addedKeywords = new HashSet<string>();

        foreach (ConversationNode node in stageData.nodes)
        {
            if (node == null ||
                outputtedNodeIds.Contains(node.nodeId) ||
                node.keywordMatchType == ConversationKeywordMatchType.None ||
                !ArePrerequisitesOutputted(node) ||
                node.keywords == null)
            {
                continue;
            }

            foreach (string keyword in node.keywords)
            {
                if (!string.IsNullOrWhiteSpace(keyword) && addedKeywords.Add(keyword))
                {
                    keywords.Add(keyword);
                }
            }
        }

        return keywords.ToArray();
    }

    private StageConversationData FindStageData(int stage)
    {
        foreach (StageConversationData data in stageConversationData)
        {
            if (data != null && data.stage == stage)
            {
                return data;
            }
        }

        return null;
    }

    private bool MatchesKeywords(ConversationNode node, string userInput)
    {
        if (node.keywordMatchType == ConversationKeywordMatchType.None ||
            node.keywords == null || node.keywords.Length == 0)
        {
            return false;
        }

        if (node.keywordMatchType == ConversationKeywordMatchType.Any)
        {
            foreach (string keyword in node.keywords)
            {
                if (!string.IsNullOrEmpty(keyword) && userInput.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        foreach (string keyword in node.keywords)
        {
            if (string.IsNullOrEmpty(keyword) || !userInput.Contains(keyword))
            {
                return false;
            }
        }

        return true;
    }

    private bool ArePrerequisitesOutputted(ConversationNode node)
    {
        if (node.prerequisiteNodeIds == null || node.prerequisiteNodeIds.Length == 0)
        {
            return true;
        }

        foreach (string prerequisiteNodeId in node.prerequisiteNodeIds)
        {
            if (string.IsNullOrWhiteSpace(prerequisiteNodeId) ||
                !outputtedNodeIds.Contains(prerequisiteNodeId))
            {
                return false;
            }
        }

        return true;
    }

    private void Awake()
    {
        ValidateData();
    }

    private void ValidateData()
    {
        var stageIds = new HashSet<int>();
        var nodeIds = new HashSet<string>();

        foreach (StageConversationData data in stageConversationData)
        {
            if (data == null)
            {
                Debug.LogError("ステージ会話データに未設定の参照があります。", this);
                continue;
            }

            if (!stageIds.Add(data.stage))
            {
                Debug.LogError($"stageが重複しています: {data.stage}", data);
            }

            if (data.nodes == null)
            {
                Debug.LogError($"会話ノード配列が未設定です: {data.name}", data);
                continue;
            }

            foreach (ConversationNode node in data.nodes)
            {
                if (node == null)
                {
                    Debug.LogError($"{data.name}に未設定の会話ノードがあります。", data);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.nodeId))
                {
                    Debug.LogError($"nodeIdが未設定です: {data.name}", data);
                    continue;
                }

                if (!nodeIds.Add(node.nodeId))
                {
                    Debug.LogError($"nodeIdが重複しています: {node.nodeId}", data);
                }
            }
        }

        foreach (StageConversationData data in stageConversationData)
        {
            if (data == null || data.nodes == null)
            {
                continue;
            }

            foreach (ConversationNode node in data.nodes)
            {
                if (node == null || node.prerequisiteNodeIds == null)
                {
                    continue;
                }

                foreach (string prerequisiteNodeId in node.prerequisiteNodeIds)
                {
                    if (!nodeIds.Contains(prerequisiteNodeId))
                    {
                        Debug.LogError(
                            $"{node.nodeId}の前提ノードが見つかりません: {prerequisiteNodeId}",
                            data);
                    }
                }
            }
        }
    }
}
