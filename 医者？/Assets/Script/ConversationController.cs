using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConversationController : MonoBehaviour
{
    public GameObject SendButton;
    public TMP_InputField InputField;
    public GameObject OutputText_scrollview;
    public GameObject OutputText_prefab;
    public ConversationData ConversationData;

    [Header("返答の表示")]
    public TMP_FontAsset ReplyFont;
    public Sprite LeftCharacterImage;
    public Sprite RightCharacterImage;
    [Min(1f)] public float ImageSize = 80f;
    [Min(0f)] public float ReplySpacing = 12f;

    private ScrollRect replyScrollRect;
    private RectTransform replyContent;
    private bool ownsTemplate;
    int currentstageID => SceneController.stageID;

    private void Start()
    {
        InitializeOutput();
    }

    private bool InitializeOutput()
    {
        if (replyContent != null)
            return true;

        replyScrollRect = OutputText_scrollview != null
            ? OutputText_scrollview.GetComponent<ScrollRect>() : null;
        if (replyScrollRect == null || replyScrollRect.content == null)
        {
            Debug.LogError("OutputText_scrollviewにContent設定済みのScroll Viewを指定してください。", this);
            return false;
        }

        replyContent = replyScrollRect.content;
        replyContent.anchorMin = new Vector2(0f, 1f);
        replyContent.anchorMax = new Vector2(1f, 1f);
        replyContent.pivot = new Vector2(0.5f, 1f);
        replyContent.sizeDelta = new Vector2(0f, replyContent.sizeDelta.y);
        replyContent.anchoredPosition = Vector2.zero;
        replyScrollRect.horizontal = false;
        replyScrollRect.vertical = true;

        var layout = GetOrAdd<VerticalLayoutGroup>(replyContent.gameObject);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = ReplySpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = GetOrAdd<ContentSizeFitter>(replyContent.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (OutputText_prefab == null)
            CreatePrefab();
        return true;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private void CreatePrefab()
    {
        // 非表示の雛形を作り、返答時にContentの子として複製する。
        OutputText_prefab = new GameObject("OutputTextTemplate", typeof(RectTransform));
        OutputText_prefab.SetActive(false);
        OutputText_prefab.transform.SetParent(transform, false);
        ownsTemplate = true;
        CreateChild(OutputText_prefab.transform, "textObject").AddComponent<TextMeshProUGUI>();
        CreateChild(OutputText_prefab.transform, "imageObject").AddComponent<Image>();
    }

    private static GameObject CreateChild(Transform parent, string childName)
    {
        var child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private void SetText(Conversation value)
    {
        if (value == null || !InitializeOutput())
            return;
        if (OutputText_prefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogError("OutputText_prefabにはUIのRectTransformが必要です。", this);
            return;
        }

        var row = Instantiate(OutputText_prefab, replyContent, false);
        row.name = "Reply_" + value.dialogueID;
        row.transform.SetAsLastSibling();
        var textChild = row.transform.Find("textObject");
        var imageChild = row.transform.Find("imageObject");
        var textObject = textChild != null ? textChild.gameObject : CreateChild(row.transform, "textObject");
        var imageObject = imageChild != null ? imageChild.gameObject : CreateChild(row.transform, "imageObject");
        var text = GetOrAdd<TextMeshProUGUI>(textObject);
        var portrait = GetOrAdd<Image>(imageObject);
        if (ReplyFont != null)
            text.font = ReplyFont;
        text.text = value.dialogue;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        portrait.raycastTarget = false;
        portrait.preserveAspect = true;
        portrait.color = Color.white;

        bool isLeft = value.characterWay == Conversation.Character_way.Left;
        var sprite = isLeft ? LeftCharacterImage : RightCharacterImage;
        if (sprite != null)
            portrait.sprite = sprite;
        // 画像が未指定の場合は白い四角として表示する。
        portrait.enabled = true;
        imageObject.transform.SetSiblingIndex(isLeft ? 0 : row.transform.childCount - 1);

        var imageLayout = GetOrAdd<LayoutElement>(imageObject);
        imageLayout.minWidth = ImageSize;
        imageLayout.preferredWidth = ImageSize;
        imageLayout.minHeight = ImageSize;
        imageLayout.preferredHeight = ImageSize;
        imageLayout.flexibleWidth = 0f;
        var textLayout = GetOrAdd<LayoutElement>(textObject);
        textLayout.minWidth = 0f;
        textLayout.flexibleWidth = 1f;
        var rowLayout = GetOrAdd<HorizontalLayoutGroup>(row);
        rowLayout.spacing = ReplySpacing;
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        row.SetActive(true);

        // 横幅確定後にTMPの折り返し高さを反映し、最新の返答までスクロールする。
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(replyContent);
        Canvas.ForceUpdateCanvases();
        replyScrollRect.StopMovement();
        replyScrollRect.verticalNormalizedPosition = 0f;
    }

    public void Onclick_send()
    {
        if (InputField == null || ConversationData == null || !InitializeOutput())
            return;
        string inputText = InputField.text;
        InputField.text = "";
        ConversationData.CheckingInput(inputText, currentstageID);
        SetText(ConversationData.Reply(currentstageID));
    }

    private void OnDestroy()
    {
        if (ownsTemplate && OutputText_prefab != null)
            Destroy(OutputText_prefab);
    }
}
