using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public enum ExaminationMagicMode
{
    Visual,
    Internal,
    Mental
}

public class ComprehensiveExaminationController : MonoBehaviour
{
    public const string SceneName = "ComprehensiveExaminationScene";
    private const float EntryManaCost = 20f;
    private const float ModeChangeManaCost = 2f;
    private const float StandardManaDrainPerSecond = 0.1f;
    private const float MentalManaDrainPerSecond = 1f;
    private const float PainSampleInterval = 0.15f;
    private const int UiLayer = 5;

    private static magic pendingManaSource;
    private static InputButton pendingReturnInput;
    private static bool isLoading;

    private static readonly Dictionary<string, float> SavedProgress = new();
    private static readonly HashSet<string> SavedFindings = new();
    private static readonly float[] HeartbeatWaveform = { 10f, -3f, 1f, 0f, 0f, 0f, 0f, 0f };

    [SerializeField]
    [Tooltip("Communicate.stageに対応する心拍・苦痛のf(MP)設定。未登録ステージは中立値を使う。")]
    private ExaminationStageVitalsData[] stageVitals = Array.Empty<ExaminationStageVitalsData>();

    [SerializeField]
    [Min(0.05f)]
    [Tooltip("患部表示が半径0から最大半径まで拡大し、0へ戻るまでの秒数。")]
    private float lesionSawtoothPeriodSeconds = 1f;

    private readonly Dictionary<ExaminationMagicMode, string[]> layersByMagic = new()
    {
        { ExaminationMagicMode.Visual, new[] { "身体表面" } },
        { ExaminationMagicMode.Internal, new[] { "表皮・皮下", "神経", "血管", "筋肉", "骨", "内臓" } },
        { ExaminationMagicMode.Mental, new[] { "主観症状" } }
    };

    private readonly List<Canvas> disabledCanvases = new();
    private readonly List<LesionState> lesions = new();
    private readonly List<GameObject> modeOptionObjects = new();

    private magic manaSource;
    private InputButton returnInput;
    private Communicate communicate;
    private Camera examinationCamera;
    private Canvas examinationCanvas;
    private TMP_FontAsset uiFont;
    private TextMeshProUGUI mpText;
    private TextMeshProUGUI layerText;
    private TextMeshProUGUI modeText;
    private TextMeshProUGUI findingsText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI heartText;
    private TextMeshProUGUI painText;
    private LineRenderer magnifierLine;
    private RingBufferLineGraph heartGraph;
    private RingBufferLineGraph painGraph;
    private RenderTexture heartGraphTexture;
    private RenderTexture painGraphTexture;

    private ExaminationMagicMode currentMagic = ExaminationMagicMode.Visual;
    private string currentLayer = "身体表面";
    private bool modeMenuOpen;
    private bool manaExhausted;
    private bool isClosing;
    private float manaDrainAccumulator;
    private float painWavePhase;
    private float heartSampleTimer;
    private float painSampleTimer;
    private int heartPulseSampleIndex;
    private Vector3 magnifierWorldPosition;
    private bool magnifierIsInExaminationArea;

    private sealed class LesionState
    {
        public string Id;
        public string FindingText;
        public ExaminationMagicMode RequiredMagic;
        public string RequiredLayer;
        public Vector2 Position;
        public float Radius;
        public float RequiredContactSeconds;
        public float MaxAlpha;
        public float Progress;
        public bool Discovered;
        public LineRenderer Line;
    }

    public static bool TryOpen(magic source, InputButton inputButton)
    {
        if (source == null)
        {
            return false;
        }

        Scene loadedScene = SceneManager.GetSceneByName(SceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            return true;
        }

        if (isLoading || !Application.CanStreamedLevelBeLoaded(SceneName))
        {
            Debug.LogError($"検査シーンをロードできません: {SceneName}");
            return false;
        }

        if (!source.TrySpendMana(EntryManaCost))
        {
            return false;
        }

        pendingManaSource = source;
        pendingReturnInput = inputButton;
        isLoading = true;

        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            isLoading = false;
            return false;
        }

        operation.completed += _ => isLoading = false;
        return true;
    }

    private void Awake()
    {
        manaSource = pendingManaSource != null
            ? pendingManaSource
            : FindAnyObjectByType<magic>();
        returnInput = pendingReturnInput != null
            ? pendingReturnInput
            : FindAnyObjectByType<InputButton>();
        communicate = FindAnyObjectByType<Communicate>();

        pendingManaSource = null;
        pendingReturnInput = null;

        DisableExistingCanvases();
        EnsureEventSystem();
        uiFont = FindJapaneseFont();
        BuildCameraAndWorld();
        BuildInterface();
        CreatePrototypeLesions();
        RefreshMagicAndLayerDisplay();
        RefreshFindings();
    }

    private void Update()
    {
        if (manaSource == null || isClosing)
        {
            return;
        }

        UpdateManaDrain();
        UpdatePointerAndMagnifier();
        UpdateLesions();
        UpdateReadouts();
    }

    private void DisableExistingCanvases()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.enabled)
            {
                canvas.enabled = false;
                disabledCanvases.Add(canvas);
            }
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new("Examination EventSystem");
        eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private TMP_FontAsset FindJapaneseFont()
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in fonts)
        {
            if (font != null &&
                font.name.IndexOf("NotoSansJP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return font;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }

    private void BuildCameraAndWorld()
    {
        GameObject cameraObject = new("Comprehensive Examination Camera");
        cameraObject.layer = UiLayer;
        examinationCamera = cameraObject.AddComponent<Camera>();
        examinationCamera.clearFlags = CameraClearFlags.SolidColor;
        examinationCamera.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 1f);
        examinationCamera.orthographic = true;
        examinationCamera.orthographicSize = 5f;
        examinationCamera.cullingMask = 1 << UiLayer;
        examinationCamera.depth = 100f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Sprite roundedSprite = CreateRoundedRectangleSprite();
        CreateBodyRegion("頭部", new Vector2(0f, 3.25f), new Vector2(1.15f, 0.9f), roundedSprite);
        CreateBodyRegion("胴", new Vector2(0f, 1.25f), new Vector2(1.75f, 2.65f), roundedSprite);
        CreateBodyRegion("左腕", new Vector2(-1.35f, 1.35f), new Vector2(0.55f, 2.45f), roundedSprite);
        CreateBodyRegion("右腕", new Vector2(1.35f, 1.35f), new Vector2(0.55f, 2.45f), roundedSprite);
        CreateBodyRegion("左足", new Vector2(-0.52f, -1.75f), new Vector2(0.72f, 2.65f), roundedSprite);
        CreateBodyRegion("右足", new Vector2(0.52f, -1.75f), new Vector2(0.72f, 2.65f), roundedSprite);

        magnifierLine = CreatePolygonLine("虫眼鏡対象範囲", new Vector2(0f, 0f), 0.62f, 32, 0.055f, 20);
        SetLineColor(magnifierLine, new Color(0.2f, 0.95f, 1f, 0.9f));
    }

    private void CreateBodyRegion(string label, Vector2 position, Vector2 size, Sprite sprite)
    {
        GameObject region = new(label);
        region.layer = UiLayer;
        region.transform.position = new Vector3(position.x, position.y, 0f);

        SpriteRenderer renderer = region.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.42f, 0.62f, 0.72f, 0.82f);
        renderer.sortingOrder = 1;

        Vector2 spriteSize = renderer.sprite.bounds.size;
        region.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);

        GameObject textObject = new(label + " Label");
        textObject.layer = UiLayer;
        textObject.transform.SetParent(region.transform, false);
        textObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        textObject.transform.localScale = new Vector3(
            1f / region.transform.localScale.x,
            1f / region.transform.localScale.y,
            1f);

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = label;
        text.font = uiFont;
        text.fontSize = 2.4f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.05f, 0.11f, 0.15f, 0.95f);
        text.sortingOrder = 2;
        text.rectTransform.sizeDelta = new Vector2(size.x, size.y);
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new("Comprehensive Examination Canvas", typeof(RectTransform));
        canvasObject.layer = UiLayer;
        examinationCanvas = canvasObject.AddComponent<Canvas>();
        examinationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        examinationCanvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform leftPanel = CreatePanel(
            "左情報欄",
            canvasObject.transform,
            new Vector2(0f, 0f),
            new Vector2(0.33f, 1f),
            new Color(0.055f, 0.09f, 0.12f, 0.94f));

        RectTransform leftScrollContent = CreateVerticalScrollContent(leftPanel);

        RectTransform facePanel = CreateScrollItem(
            "患者の顔",
            leftScrollContent,
            20f,
            280f,
            new Color(0.18f, 0.25f, 0.29f, 1f));
        CreateText("患者の顔\n（表情写真）", facePanel, Vector2.zero, Vector2.one, 30f, TextAlignmentOptions.Center);

        RectTransform heartGraphBase = CreateScrollItem(
            "心拍グラフ土台",
            leftScrollContent,
            320f,
            300f,
            new Color(0.045f, 0.075f, 0.095f, 0.98f));
        heartText = CreateText("心拍：88 bpm", heartGraphBase, new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.96f), 26f);
        heartGraph = BuildLineGraph(
            heartGraphBase,
            "Heart Graph",
            9,
            new Color(0.25f, 1f, 0.45f, 1f),
            5f,
            16f,
            0f,
            out heartGraphTexture);

        RectTransform painGraphBase = CreateScrollItem(
            "苦痛グラフ土台",
            leftScrollContent,
            640f,
            300f,
            new Color(0.045f, 0.075f, 0.095f, 0.98f));
        painText = CreateText("苦痛：5.0 / 10　ばらつき：2.0", painGraphBase, new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.96f), 26f);
        painGraph = BuildLineGraph(
            painGraphBase,
            "Pain Graph",
            8,
            new Color(1f, 0.25f, 0.3f, 1f),
            5f,
            16f,
            5f,
            out painGraphTexture);

        RectTransform findingsPanel = CreateScrollItem(
            "判明した情報欄",
            leftScrollContent,
            960f,
            360f,
            new Color(0.07f, 0.12f, 0.15f, 0.98f));
        findingsText = CreateText("判明した情報\n・まだありません", findingsPanel, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), 21f, TextAlignmentOptions.TopLeft);

        Button returnButton = CreateButton("診断へ戻る", leftPanel, new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.13f));
        returnButton.onClick.AddListener(CloseExamination);

        RectTransform rightPanel = CreatePanel(
            "右操作欄",
            canvasObject.transform,
            new Vector2(0.70f, 0.03f),
            new Vector2(0.985f, 0.97f),
            new Color(0.055f, 0.09f, 0.12f, 0.94f));

        layerText = CreateText("現在レイヤー：身体表面", rightPanel, new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.96f), 27f);
        Button previousLayer = CreateButton("＜ 表層", rightPanel, new Vector2(0.06f, 0.77f), new Vector2(0.47f, 0.84f));
        Button nextLayer = CreateButton("深層 ＞", rightPanel, new Vector2(0.53f, 0.77f), new Vector2(0.94f, 0.84f));
        previousLayer.onClick.AddListener(() => ChangeLayer(-1));
        nextLayer.onClick.AddListener(() => ChangeLayer(1));

        mpText = CreateText("MP", rightPanel, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.76f), 30f);
        CreateButton("カルテ", rightPanel, new Vector2(0.06f, 0.54f), new Vector2(0.94f, 0.65f));
        statusText = CreateText("虫眼鏡を患部へ重ねてください。", rightPanel, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.51f), 23f, TextAlignmentOptions.TopLeft);

        RectTransform optionPanel = CreatePanel(
            "検査魔法候補",
            rightPanel,
            new Vector2(0.06f, 0.12f),
            new Vector2(0.94f, 0.34f),
            new Color(0.08f, 0.13f, 0.17f, 0.98f));

        CreateModeOption("読心", ExaminationMagicMode.Mental, optionPanel, 0.68f, 0.98f);
        CreateModeOption("非破壊内部検査", ExaminationMagicMode.Internal, optionPanel, 0.35f, 0.65f);
        CreateModeOption("目視", ExaminationMagicMode.Visual, optionPanel, 0.02f, 0.32f);
        optionPanel.gameObject.SetActive(false);
        modeOptionObjects.Add(optionPanel.gameObject);

        Button modeButton = CreateButton("検査魔法を選択 ▲", rightPanel, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.10f));
        modeText = modeButton.GetComponentInChildren<TextMeshProUGUI>();
        modeButton.onClick.AddListener(ToggleModeMenu);
    }

    private RectTransform CreateVerticalScrollContent(RectTransform leftPanel)
    {
        RectTransform viewport = CreatePanel(
            "左情報スクロールViewport",
            leftPanel,
            new Vector2(0.02f, 0.15f),
            new Vector2(0.98f, 0.985f),
            new Color(0f, 0f, 0f, 0.01f));

        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = new("左情報スクロールContent", typeof(RectTransform));
        contentObject.layer = UiLayer;
        contentObject.transform.SetParent(viewport, false);

        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1340f);

        ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.verticalNormalizedPosition = 1f;

        return content;
    }

    private RectTransform CreateScrollItem(
        string name,
        RectTransform content,
        float top,
        float height,
        Color color)
    {
        RectTransform item = CreatePanel(
            name,
            content,
            new Vector2(0.02f, 1f),
            new Vector2(0.98f, 1f),
            color);
        item.pivot = new Vector2(0.5f, 1f);
        item.anchoredPosition = new Vector2(0f, -top);
        item.sizeDelta = new Vector2(0f, height);
        return item;
    }

    private RingBufferLineGraph BuildLineGraph(
        RectTransform graphBase,
        string graphName,
        int graphLayer,
        Color lineColor,
        float graphCenterY,
        float graphHalfHeight,
        float initialValue,
        out RenderTexture graphTexture)
    {
        GameObject graphViewObject = new(graphName + " View", typeof(RectTransform));
        graphViewObject.layer = UiLayer;
        graphViewObject.transform.SetParent(graphBase, false);

        RectTransform graphViewRect = graphViewObject.GetComponent<RectTransform>();
        graphViewRect.anchorMin = new Vector2(0.04f, 0.05f);
        graphViewRect.anchorMax = new Vector2(0.96f, 0.74f);
        graphViewRect.offsetMin = Vector2.zero;
        graphViewRect.offsetMax = Vector2.zero;

        RawImage graphView = graphViewObject.AddComponent<RawImage>();
        graphView.color = Color.white;
        graphView.raycastTarget = false;

        graphTexture = new RenderTexture(384, 160, 0, RenderTextureFormat.ARGB32)
        {
            name = graphName + " Render Texture",
            filterMode = FilterMode.Bilinear
        };
        graphTexture.Create();
        graphView.texture = graphTexture;

        GameObject graphCameraObject = new(graphName + " Camera");
        graphCameraObject.layer = graphLayer;
        Camera graphCamera = graphCameraObject.AddComponent<Camera>();
        graphCamera.clearFlags = CameraClearFlags.SolidColor;
        graphCamera.backgroundColor = new Color(0.025f, 0.04f, 0.055f, 1f);
        graphCamera.orthographic = true;
        graphCamera.orthographicSize = graphHalfHeight;
        graphCamera.aspect = 384f / 160f;
        graphCamera.cullingMask = 1 << graphLayer;
        graphCamera.targetTexture = graphTexture;
        graphCamera.transform.position = new Vector3(137.5f, graphCenterY, -10f);

        GameObject graphObject = new(graphName + " Ring Buffer Line");
        graphObject.layer = graphLayer;
        float horizontalScale = 15f / (RingBufferLineGraph.SampleCount - 1);
        graphObject.transform.localScale = new Vector3(horizontalScale, 1f, 1f);
        graphObject.transform.position = new Vector3(100f * (1f - horizontalScale), 0f, 0f);

        LineRenderer graphLine = graphObject.AddComponent<LineRenderer>();
        graphLine.useWorldSpace = false;
        graphLine.loop = false;
        graphLine.positionCount = RingBufferLineGraph.SampleCount;
        graphLine.startWidth = 0.2f;
        graphLine.endWidth = 0.2f;
        graphLine.numCapVertices = 2;
        graphLine.numCornerVertices = 2;
        graphLine.startColor = lineColor;
        graphLine.endColor = lineColor;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            graphLine.material = new Material(shader);
        }

        RingBufferLineGraph graph = graphObject.AddComponent<RingBufferLineGraph>();
        graph.Configure(graphLine, initialValue);
        return graph;
    }

    private void CreateModeOption(
        string label,
        ExaminationMagicMode mode,
        RectTransform parent,
        float minY,
        float maxY)
    {
        Button button = CreateButton(label, parent, new Vector2(0.03f, minY), new Vector2(0.97f, maxY));
        button.onClick.AddListener(() => SelectMagic(mode));
    }

    private void CreatePrototypeLesions()
    {
        AddLesion(
            "S01-V001",
            "左腕に浅い切創と発赤を確認",
            ExaminationMagicMode.Visual,
            "身体表面",
            new Vector2(-1.36f, 1.65f),
            0.34f);

        AddLesion(
            "S01-I001",
            "左腕皮下に白い針状異物を確認",
            ExaminationMagicMode.Internal,
            "表皮・皮下",
            new Vector2(-1.34f, 1.18f),
            0.29f);

        AddLesion(
            "S01-M001",
            "左腕に強い灼熱感を確認",
            ExaminationMagicMode.Mental,
            "主観症状",
            new Vector2(-1.35f, 1.40f),
            0.43f);
    }

    private void AddLesion(
        string id,
        string findingText,
        ExaminationMagicMode requiredMagic,
        string requiredLayer,
        Vector2 position,
        float radius)
    {
        float savedProgress = SavedProgress.TryGetValue(id, out float progress) ? progress : 0f;
        bool discovered = SavedFindings.Contains(id);

        LesionState lesion = new()
        {
            Id = id,
            FindingText = findingText,
            RequiredMagic = requiredMagic,
            RequiredLayer = requiredLayer,
            Position = position,
            Radius = radius,
            RequiredContactSeconds = 2.5f,
            MaxAlpha = 0.82f,
            Progress = discovered ? 1f : savedProgress,
            Discovered = discovered,
            Line = CreatePolygonLine(id, position, radius, 32, 0.07f, 12)
        };

        lesions.Add(lesion);
        UpdateLesionLine(lesion);
    }

    private void UpdateManaDrain()
    {
        if (manaExhausted)
        {
            return;
        }

        manaDrainAccumulator += Time.deltaTime * GetManaDrainPerSecond();
        while (manaDrainAccumulator >= 0.1f)
        {
            if (!manaSource.TrySpendMana(0.1f))
            {
                manaExhausted = true;
                statusText.text = "MPが尽きました。診断へ戻ってください。";
                return;
            }

            manaDrainAccumulator -= 0.1f;
        }
    }

    private void UpdatePointerAndMagnifier()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 screenPosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
#else
        Vector2 screenPosition = Input.mousePosition;
        if (Input.touchCount > 0)
        {
            screenPosition = Input.GetTouch(0).position;
        }
#endif

        magnifierIsInExaminationArea =
            screenPosition.x >= Screen.width * 0.30f &&
            screenPosition.x <= Screen.width * 0.70f &&
            screenPosition.y >= 0f &&
            screenPosition.y <= Screen.height;

        magnifierLine.enabled = magnifierIsInExaminationArea;
        if (!magnifierIsInExaminationArea)
        {
            return;
        }

        Vector3 world = examinationCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 10f));
        magnifierWorldPosition = new Vector3(world.x, world.y, -0.2f);
        magnifierLine.transform.position = magnifierWorldPosition;
    }

    private void UpdateLesions()
    {
        foreach (LesionState lesion in lesions)
        {
            bool isRelevant =
                lesion.RequiredMagic == currentMagic &&
                lesion.RequiredLayer == currentLayer;
            bool ignoresMagnifier =
                isRelevant && currentMagic == ExaminationMagicMode.Mental;
            bool isObserved =
                ignoresMagnifier ||
                (magnifierIsInExaminationArea &&
                 Vector2.Distance(magnifierWorldPosition, lesion.Position) <= 0.62f + lesion.Radius);

            if (isRelevant &&
                !lesion.Discovered &&
                !manaExhausted &&
                isObserved)
            {
                lesion.Progress = Mathf.Clamp01(
                    lesion.Progress + Time.deltaTime / lesion.RequiredContactSeconds);
                SavedProgress[lesion.Id] = lesion.Progress;

                if (lesion.Progress >= 1f)
                {
                    lesion.Discovered = true;
                    SavedFindings.Add(lesion.Id);
                    statusText.text = lesion.FindingText;
                    RefreshFindings();
                    if (communicate != null)
                    {
                        communicate.RegisterExaminationFinding(lesion.Id, lesion.FindingText);
                    }
                }
            }

            UpdateLesionLine(lesion);
        }
    }

    private void UpdateLesionLine(LesionState lesion)
    {
        bool isRelevant =
            lesion.RequiredMagic == currentMagic &&
            lesion.RequiredLayer == currentLayer;
        bool isMentalGlobalReveal =
            isRelevant && currentMagic == ExaminationMagicMode.Mental;
        float alpha = isMentalGlobalReveal
            ? lesion.MaxAlpha
            : isRelevant ? lesion.Progress * lesion.MaxAlpha : 0f;
        SetLineColor(lesion.Line, new Color(1f, 0.05f, 0.05f, alpha));

        float safePeriod = Mathf.Max(0.05f, lesionSawtoothPeriodSeconds);
        float radiusScale = Mathf.Repeat(Time.time / safePeriod, 1f);
        lesion.Line.transform.localScale = new Vector3(radiusScale, radiusScale, 1f);
    }

    private float GetManaDrainPerSecond()
    {
        return currentMagic == ExaminationMagicMode.Mental
            ? MentalManaDrainPerSecond
            : StandardManaDrainPerSecond;
    }

    private void UpdateReadouts()
    {
        float currentMp = manaSource != null ? manaSource.CurrentMP : 0f;
        if (mpText != null && manaSource != null)
        {
            mpText.text = $"MP：{currentMp:0.0}\n維持消費：毎秒{GetManaDrainPerSecond():0.0}";
        }

        ExaminationStageVitalsData vitals = FindStageVitals();
        float heartRate = vitals != null ? vitals.EvaluateHeartRate(currentMp) : 88f;
        float painLevel = vitals != null ? vitals.EvaluatePainLevel(currentMp) : 5f;
        float painVariation = vitals != null ? vitals.EvaluatePainVariation(currentMp) : 2f;
        bool painIsAvailable =
            currentMp > 0.0001f ||
            vitals == null ||
            vitals.mpZeroPolicy == ExaminationMpZeroPolicy.Stable;

        if (!painIsAvailable)
        {
            heartRate = 0f;
            painLevel = 0f;
            painVariation = 0f;
        }

        if (heartText != null)
        {
            heartText.text = $"心拍：{heartRate:0} bpm";
        }

        if (painText != null)
        {
            painText.text = painIsAvailable
                ? $"苦痛：{painLevel:0.0} / 10　ばらつき：{painVariation:0.0}"
                : "苦痛：N/A　ばらつき：0.0";
        }

        if (painGraph != null)
        {
            painGraph.gameObject.SetActive(painIsAvailable);
        }

        painWavePhase = Mathf.Repeat(painWavePhase + Time.deltaTime * 2.2f, Mathf.PI * 2f);

        heartSampleTimer += Time.deltaTime;
        if (heartRate > 0.01f)
        {
            float heartSampleInterval = 60f / heartRate / HeartbeatWaveform.Length;
            while (heartSampleTimer >= heartSampleInterval)
            {
                heartSampleTimer -= heartSampleInterval;
                float heartValue = HeartbeatWaveform[
                    heartPulseSampleIndex % HeartbeatWaveform.Length];
                heartGraph?.AddSample(heartValue);
                heartPulseSampleIndex++;
            }
        }
        else
        {
            while (heartSampleTimer >= PainSampleInterval)
            {
                heartSampleTimer -= PainSampleInterval;
                heartGraph?.AddSample(0f);
            }
        }

        if (painIsAvailable)
        {
            painSampleTimer += Time.deltaTime;
            while (painSampleTimer >= PainSampleInterval)
            {
                painSampleTimer -= PainSampleInterval;
                float painWave =
                    Mathf.Sin(painWavePhase) * 0.65f +
                    Mathf.Sin(painWavePhase * 0.37f) * 0.35f;
                float painValue = painLevel + painWave * painVariation;

                painGraph?.AddSample(painValue);
            }
        }
        else
        {
            painSampleTimer = 0f;
        }
    }

    private ExaminationStageVitalsData FindStageVitals()
    {
        int currentStage = communicate != null ? communicate.stage : 0;
        if (stageVitals == null)
        {
            return null;
        }

        foreach (ExaminationStageVitalsData vitals in stageVitals)
        {
            if (vitals != null && vitals.stage == currentStage)
            {
                return vitals;
            }
        }

        return null;
    }

    private void SelectMagic(ExaminationMagicMode nextMagic)
    {
        if (nextMagic == currentMagic)
        {
            CloseModeMenu();
            return;
        }

        if (manaExhausted || !manaSource.TrySpendMana(ModeChangeManaCost))
        {
            statusText.text = "検査魔法を切り替えるMPが足りません。";
            return;
        }

        currentMagic = nextMagic;
        string[] availableLayers = layersByMagic[currentMagic];
        if (Array.IndexOf(availableLayers, currentLayer) < 0)
        {
            currentLayer = availableLayers[0];
        }

        statusText.text = $"{GetMagicLabel(currentMagic)}へ切り替えました。";
        CloseModeMenu();
        RefreshMagicAndLayerDisplay();
    }

    private void ChangeLayer(int direction)
    {
        string[] availableLayers = layersByMagic[currentMagic];
        int index = Array.IndexOf(availableLayers, currentLayer);
        index = Mathf.Clamp(index + direction, 0, availableLayers.Length - 1);
        currentLayer = availableLayers[index];
        RefreshMagicAndLayerDisplay();
    }

    private void RefreshMagicAndLayerDisplay()
    {
        if (layerText != null)
        {
            layerText.text = $"現在レイヤー：{currentLayer}";
        }

        if (modeText != null)
        {
            modeText.text = $"{GetMagicLabel(currentMagic)} ▲";
        }

        foreach (LesionState lesion in lesions)
        {
            UpdateLesionLine(lesion);
        }
    }

    private void RefreshFindings()
    {
        if (findingsText == null)
        {
            return;
        }

        var lines = new List<string> { "判明した情報" };
        foreach (LesionState lesion in lesions)
        {
            if (lesion.Discovered)
            {
                lines.Add("・" + lesion.FindingText);
            }
        }

        if (lines.Count == 1)
        {
            lines.Add("・まだありません");
        }

        findingsText.text = string.Join("\n", lines);
    }

    private void ToggleModeMenu()
    {
        modeMenuOpen = !modeMenuOpen;
        foreach (GameObject optionObject in modeOptionObjects)
        {
            optionObject.SetActive(modeMenuOpen);
        }
    }

    private void CloseModeMenu()
    {
        modeMenuOpen = false;
        foreach (GameObject optionObject in modeOptionObjects)
        {
            optionObject.SetActive(false);
        }
    }

    private void CloseExamination()
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        RestoreExistingCanvases();
        if (returnInput != null)
        {
            returnInput.isActiveinputfield = true;
        }

        SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    private void RestoreExistingCanvases()
    {
        foreach (Canvas canvas in disabledCanvases)
        {
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        disabledCanvases.Clear();
    }

    private void OnDestroy()
    {
        if (!isClosing)
        {
            RestoreExistingCanvases();
            if (returnInput != null)
            {
                returnInput.isActiveinputfield = true;
            }
        }

        ReleaseGraphTexture(ref heartGraphTexture);
        ReleaseGraphTexture(ref painGraphTexture);
    }

    private static void ReleaseGraphTexture(ref RenderTexture graphTexture)
    {
        if (graphTexture == null)
        {
            return;
        }

        graphTexture.Release();
        Destroy(graphTexture);
        graphTexture = null;
    }

    private static string GetMagicLabel(ExaminationMagicMode mode)
    {
        return mode switch
        {
            ExaminationMagicMode.Visual => "目視",
            ExaminationMagicMode.Internal => "非破壊内部検査",
            ExaminationMagicMode.Mental => "読心",
            _ => mode.ToString()
        };
    }

    private RectTransform CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject panelObject = new(name, typeof(RectTransform));
        panelObject.layer = UiLayer;
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panelObject.AddComponent<Image>();
        image.sprite = CreateRoundedRectangleSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        return rect;
    }

    private TextMeshProUGUI CreateText(
        string text,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        GameObject textObject = new(text.Replace("\n", " ") + " Text", typeof(RectTransform));
        textObject.layer = UiLayer;
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = uiFont;
        label.fontSize = fontSize;
        label.color = new Color(0.91f, 0.96f, 0.98f, 1f);
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private Button CreateButton(
        string label,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        RectTransform rect = CreatePanel(
            label + " Button",
            parent,
            anchorMin,
            anchorMax,
            new Color(0.18f, 0.34f, 0.43f, 1f));

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        CreateText(label, rect, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), 25f, TextAlignmentOptions.Center);
        return button;
    }

    private LineRenderer CreatePolygonLine(
        string name,
        Vector2 center,
        float radius,
        int vertexCount,
        float width,
        int sortingOrder)
    {
        GameObject lineObject = new(name);
        lineObject.layer = UiLayer;
        lineObject.transform.position = new Vector3(center.x, center.y, -0.25f);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = vertexCount;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sortingOrder = sortingOrder;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            line.material = new Material(shader);
        }

        for (int i = 0; i < vertexCount; i++)
        {
            float angle = Mathf.PI * 2f * i / vertexCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }

        return line;
    }

    private static void SetLineColor(LineRenderer line, Color color)
    {
        if (line == null)
        {
            return;
        }

        line.startColor = color;
        line.endColor = color;
    }

    private static Sprite CreateRoundedRectangleSprite()
    {
        const int size = 64;
        const float radius = 12f;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Rounded Rectangle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, 0f, x - (size - 1 - radius));
                float dy = Mathf.Max(radius - y, 0f, y - (size - 1 - radius));
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius + 0.75f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }
}
