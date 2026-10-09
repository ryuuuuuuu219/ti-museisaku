using System;
using System.Collections.Generic;
using UnityEngine;

// シーン間の一時保存。GameObjectへの追加は不要。
public static class ConversationSave
{
    private const string SaveKey = "Conversation.TemporaryState.v1";

    [Serializable]
    private class SavedState
    {
        public int stageID;
        public List<int> replyedID;
        public List<int> replyIDqueue;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginSession()
    {
        // 前回終了時の状態は引き継がない。Domain Reload無効時も初期化する。
        Clear();
        SceneController.stageID = 0;
    }

    public static void SaveCurrent()
    {
        var data = UnityEngine.Object.FindAnyObjectByType<ConversationData>();
        // カルテなど会話データがないシーンでは既存の保存を上書きしない。
        if (data == null)
            return;

        var state = new SavedState
        {
            stageID = SceneController.stageID,
            replyedID = new List<int>(data.replyedID),
            replyIDqueue = new List<int>(data.replyIDqueue)
        };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
    }

    public static bool Restore(ConversationData data, int stageID)
    {
        if (data == null || !PlayerPrefs.HasKey(SaveKey))
            return false;

        SavedState state;
        try
        {
            state = JsonUtility.FromJson<SavedState>(PlayerPrefs.GetString(SaveKey));
        }
        catch (ArgumentException)
        {
            Debug.LogWarning("会話の一時保存データを読み込めないため削除します。");
            Clear();
            return false;
        }

        if (state == null || state.stageID != stageID ||
            state.replyedID == null || state.replyIDqueue == null)
        {
            Clear();
            return false;
        }

        var conversations = data.GetConversationsByStage(stageID);
        if (conversations == null)
            return false;

        var validIDs = new HashSet<int>();
        foreach (var conversation in conversations)
        {
            if (conversation != null)
                validIDs.Add(conversation.dialogueID);
        }

        data.replyedID.Clear();
        data.replyIDqueue.Clear();
        // 会話定義の変更で消えたIDと重複を除き、保存順は維持する。
        foreach (int id in state.replyedID)
        {
            if (validIDs.Contains(id) && !data.replyedID.Contains(id))
                data.replyedID.Add(id);
        }
        foreach (int id in state.replyIDqueue)
        {
            if (validIDs.Contains(id) && !data.replyedID.Contains(id) &&
                !data.replyIDqueue.Contains(id))
                data.replyIDqueue.Add(id);
        }
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
