using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class WordViewManager : MonoBehaviour
{
    public ConversationData ConversationData;
    public Button WordButtonPrefab;
    public TMP_InputField InputField;

    private ScrollRect wordView;
    private readonly List<string> keywords = new List<string>();
    private readonly List<string> displayedKeywords = new List<string>();
    private readonly HashSet<string> uniqueKeywords = new HashSet<string>();
    private readonly List<GameObject> generatedButtons = new List<GameObject>();
    private bool initialized;

    private void Start()
    {
        wordView = GetComponent<ScrollRect>();
        if (ConversationData == null || wordView.content == null || WordButtonPrefab == null ||
            WordButtonPrefab.GetComponentInChildren<TMP_Text>(true) == null)
        {
            Debug.LogError("WordViewにConversationData、Content、TMP付きのWordButtonPrefabを設定してください。", this);
            enabled = false;
            return;
        }

        var content = wordView.content;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
        content.anchoredPosition = Vector2.zero;
        wordView.horizontal = false;
        wordView.vertical = true;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        initialized = true;
        RefreshWords();
    }

    private void LateUpdate()
    {
        // 入力方法を問わず会話の進行を反映し、一覧が変わったときだけ再生成する。
        RefreshWords();
    }

    public void RefreshWords()
    {
        if (!initialized || ConversationData == null)
            return;

        keywords.Clear();
        uniqueKeywords.Clear();
        var nodes = ConversationData.GetConversationsByStage(SceneController.stageID);
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                if (node == null || node.conversationPhase != Conversation.phase.Interview ||
                    ConversationData.replyedID.Contains(node.dialogueID) ||
                    !ConversationData.CanUnlock(node) || node.keywords == null)
                    continue;

                foreach (var keyword in node.keywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword) && uniqueKeywords.Add(keyword))
                        keywords.Add(keyword);
                }
            }
        }

        if (HasSameKeywords())
            return;

        float scrollPosition = wordView.verticalNormalizedPosition;
        ClearButtons();
        foreach (var keyword in keywords)
        {
            var button = Instantiate(WordButtonPrefab, wordView.content, false);
            button.name = "Word_" + keyword;
            button.interactable = true;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = keyword;
            label.richText = false;
            button.onClick.AddListener(() => SetInputText(label));
            var size = button.GetComponent<LayoutElement>();
            if (size == null)
                size = button.gameObject.AddComponent<LayoutElement>();
            size.minHeight = WordButtonPrefab.GetComponent<RectTransform>().rect.height;
            size.preferredHeight = size.minHeight;
            button.gameObject.SetActive(true);
            generatedButtons.Add(button.gameObject);
        }
        displayedKeywords.Clear();
        displayedKeywords.AddRange(keywords);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(wordView.content);
        wordView.verticalNormalizedPosition = scrollPosition;
    }

    private void SetInputText(TMP_Text label)
    {
        if (InputField == null)
        {
            Debug.LogWarning("WordViewのInputFieldを設定してください。", this);
            return;
        }

        InputField.text = label.text;
    }

    private bool HasSameKeywords()
    {
        if (keywords.Count != displayedKeywords.Count)
            return false;
        for (int i = 0; i < keywords.Count; i++)
        {
            if (keywords[i] != displayedKeywords[i])
                return false;
        }
        return true;
    }

    private void ClearButtons()
    {
        foreach (var button in generatedButtons)
        {
            if (button == null)
                continue;
            button.SetActive(false);
            Destroy(button);
        }
        generatedButtons.Clear();
    }

    private void OnDestroy()
    {
        ClearButtons();
    }
}
