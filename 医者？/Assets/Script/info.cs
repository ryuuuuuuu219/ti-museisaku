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

        foreach (ConversationNode node in stageData.nodes)
        {
            if (node == null || outputtedNodeIds.Contains(node.nodeId))
            {
                continue;
            }

            if (!ArePrerequisitesOutputted(node) ||
                IsBlockedByOutputtedNode(node) ||
                !MatchesKeywords(node, userInput))
            {
                continue;
            }

            if (replyNode == null || HasHigherPriority(node, replyNode))
            {
                replyNode = node;
            }
        }

        return replyNode == null || replyNode.isWaitingInput;
    }

    public bool TryOutput(int stage, out string text, out bool isWaitingInput)
    {
        ConversationNode selectedNode = replyNode;
        replyNode = null;

        if (selectedNode == null)
        {
            StageConversationData stageData = FindStageData(stage);
            if (stageData != null)
            {
                foreach (ConversationNode node in stageData.nodes)
                {
                    if (node == null || outputtedNodeIds.Contains(node.nodeId))
                    {
                        continue;
                    }

                    if (node.keywordMatchType != ConversationKeywordMatchType.None ||
                        !ArePrerequisitesOutputted(node) ||
                        IsBlockedByOutputtedNode(node))
                    {
                        continue;
                    }

                    if (selectedNode == null || HasHigherPriority(node, selectedNode))
                    {
                        selectedNode = node;
                    }
                }
            }
        }

        if (selectedNode == null)
        {
            text = "?";
            isWaitingInput = true;
            return false;
        }

        outputtedNodeIds.Add(selectedNode.nodeId);
        text = selectedNode.text;
        isWaitingInput = selectedNode.isWaitingInput;
        return true;
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
                IsBlockedByOutputtedNode(node) ||
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

        if (node.keywordMatchType == ConversationKeywordMatchType.Exact)
        {
            foreach (string keyword in node.keywords)
            {
                if (!string.IsNullOrEmpty(keyword) && userInput == keyword)
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

    private bool IsBlockedByOutputtedNode(ConversationNode node)
    {
        if (node.blockingNodeIds == null || node.blockingNodeIds.Length == 0)
        {
            return false;
        }

        foreach (string blockingNodeId in node.blockingNodeIds)
        {
            if (!string.IsNullOrWhiteSpace(blockingNodeId) &&
                outputtedNodeIds.Contains(blockingNodeId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasHigherPriority(ConversationNode candidate, ConversationNode current)
    {
        int candidateNumber = GetNodeNumber(candidate.nodeId);
        int currentNumber = GetNodeNumber(current.nodeId);

        if (candidateNumber != currentNumber)
        {
            return candidateNumber < currentNumber;
        }

        return string.CompareOrdinal(candidate.nodeId, current.nodeId) < 0;
    }

    private static int GetNodeNumber(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return int.MaxValue;
        }

        int markerIndex = nodeId.LastIndexOf("-C", StringComparison.Ordinal);
        if (markerIndex < 0 || markerIndex + 2 >= nodeId.Length)
        {
            return int.MaxValue;
        }

        return int.TryParse(nodeId.Substring(markerIndex + 2), out int nodeNumber)
            ? nodeNumber
            : int.MaxValue;
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

                if (GetNodeNumber(node.nodeId) == int.MaxValue)
                {
                    Debug.LogError(
                        $"nodeIdから優先番号を取得できません: {node.nodeId}",
                        data);
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
                if (node == null)
                {
                    continue;
                }

                if (node.prerequisiteNodeIds != null)
                {
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

                if (node.blockingNodeIds != null)
                {
                    foreach (string blockingNodeId in node.blockingNodeIds)
                    {
                        if (!nodeIds.Contains(blockingNodeId))
                        {
                            Debug.LogError(
                                $"{node.nodeId}の除外ノードが見つかりません: {blockingNodeId}",
                                data);
                        }
                    }
                }
            }
        }
    }
}
