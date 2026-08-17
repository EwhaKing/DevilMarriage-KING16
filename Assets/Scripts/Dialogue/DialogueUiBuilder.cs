using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대화 UI(DialogueCanvas) 계층을 만드는 도우미입니다.
/// 에디터 메뉴와 DialogueManager가 같은 구조를 쓰도록 분리했습니다.
/// Play 모드가 아니어도 씬에 미리 배치해 두고, Inspector에서 위치/크기를 조절할 수 있습니다.
/// </summary>
public static class DialogueUiBuilder
{
    public const string CanvasName = "DialogueCanvas";
    public const string PanelName = "DialoguePanel";

    /// <summary>빌드 결과로 나온 UI 참조들입니다.</summary>
    public struct Result
    {
        public Canvas canvas;
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueBodyText;
        public Button nextButton;
        public Button autoButton;
        public Button skipButton;
        public Button logButton;
        public Button settingButton;
        public GameObject logPanelRoot;
        public TextMeshProUGUI logBodyText;
    }

    /// <summary>
    /// DialogueCanvas 전체를 생성합니다. 이미 같은 이름이 있으면 새로 만들지 않고 null을 반환합니다.
    /// </summary>
    public static Result? BuildNew(TMP_FontAsset font, bool forceReplace = false, string canvasName = null)
    {
        if (string.IsNullOrEmpty(canvasName))
            canvasName = CanvasName;

        var existing = GameObject.Find(canvasName);
        if (existing != null)
        {
            if (!forceReplace)
                return null;

            Object.DestroyImmediate(existing);
        }

        var canvasGo = new GameObject(canvasName);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 캔버스 오른쪽 위 버튼들
        var autoButton = CreateUiButton(canvas.transform, "AutoButton", "Auto",
            new Vector2(0.72f, 0.92f), new Vector2(0.82f, 0.98f), font);
        var skipButton = CreateUiButton(canvas.transform, "SkipButton", "Skip",
            new Vector2(0.83f, 0.92f), new Vector2(0.93f, 0.98f), font);
        var logButton = CreateUiButton(canvas.transform, "LogButton", "Log",
            new Vector2(0.72f, 0.84f), new Vector2(0.82f, 0.90f), font);
        var settingButton = CreateUiButton(canvas.transform, "SettingButton", "Setting",
            new Vector2(0.83f, 0.84f), new Vector2(0.93f, 0.90f), font);

        // 하단 대사 패널
        var panel = new GameObject(PanelName);
        panel.transform.SetParent(canvas.transform, false);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.04f);
        panelRect.anchorMax = new Vector2(0.95f, 0.34f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var speakerNameText = CreateTmp(panel.transform, "SpeakerName", 30, TextAlignmentOptions.Left, font);
        speakerNameText.text = "화자 이름";
        var speakerRect = speakerNameText.rectTransform;
        speakerRect.anchorMin = new Vector2(0.04f, 0.72f);
        speakerRect.anchorMax = new Vector2(0.78f, 0.95f);
        speakerRect.offsetMin = Vector2.zero;
        speakerRect.offsetMax = Vector2.zero;

        var dialogueBodyText = CreateTmp(panel.transform, "DialogueBody", 26, TextAlignmentOptions.TopLeft, font);
        dialogueBodyText.textWrappingMode = TextWrappingModes.Normal;
        dialogueBodyText.text = "여기에 대사가 표시됩니다. Play 전에 위치와 크기를 조절하세요.";
        var bodyRect = dialogueBodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.04f, 0.08f);
        bodyRect.anchorMax = new Vector2(0.78f, 0.70f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        var nextButton = CreateUiButton(panel.transform, "NextButton", "▼ 다음",
            new Vector2(0.82f, 0.08f), new Vector2(0.97f, 0.38f), font);

        // 로그 패널 (기본 비활성)
        var logPanel = new GameObject("DialogueLogPanel");
        logPanel.transform.SetParent(canvas.transform, false);
        var logImg = logPanel.AddComponent<Image>();
        logImg.color = new Color(0f, 0f, 0f, 0.85f);
        var logRect = logPanel.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0.15f, 0.15f);
        logRect.anchorMax = new Vector2(0.85f, 0.85f);
        logRect.offsetMin = Vector2.zero;
        logRect.offsetMax = Vector2.zero;

        var logBody = CreateTmp(logPanel.transform, "LogBody", 22, TextAlignmentOptions.TopLeft, font);
        logBody.text = "대화 로그";

        var closeLog = CreateUiButton(logPanel.transform, "CloseLog", "Close",
            new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.12f), font);
        closeLog.onClick.AddListener(() => logPanel.SetActive(false));
        SetupLogPanelScrolling(logPanel, ref logBody);
        logPanel.SetActive(false);

        return new Result
        {
            canvas = canvas,
            speakerNameText = speakerNameText,
            dialogueBodyText = dialogueBodyText,
            nextButton = nextButton,
            autoButton = autoButton,
            skipButton = skipButton,
            logButton = logButton,
            settingButton = settingButton,
            logPanelRoot = logPanel,
            logBodyText = logBody
        };
    }

    /// <summary>
    /// 씬에 있는 DialogueCanvas에서 이름 기준으로 UI를 찾아 연결합니다.
    /// </summary>
    public static bool TryFindExisting(out Result result)
    {
        result = default;
        var canvasGo = GameObject.Find(CanvasName);
        if (canvasGo == null)
            return false;

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null)
            return false;

        result.canvas = canvas;
        result.speakerNameText = FindTmp(canvasGo.transform, "SpeakerName");
        result.dialogueBodyText = FindTmp(canvasGo.transform, "DialogueBody");
        result.nextButton = FindButton(canvasGo.transform, "NextButton");
        result.autoButton = FindButton(canvasGo.transform, "AutoButton");
        result.skipButton = FindButton(canvasGo.transform, "SkipButton");
        result.logButton = FindButton(canvasGo.transform, "LogButton");
        result.settingButton = FindButton(canvasGo.transform, "SettingButton");

        var logPanel = canvasGo.transform.Find("DialogueLogPanel");
        if (logPanel != null)
        {
            result.logPanelRoot = logPanel.gameObject;
            result.logBodyText = FindLogBodyText(logPanel);
            SetupLogPanelScrolling(result.logPanelRoot, ref result.logBodyText);
        }

        return result.speakerNameText != null && result.dialogueBodyText != null;
    }

    public static TextMeshProUGUI FindLogBodyText(Transform logPanel)
    {
        if (logPanel == null)
            return null;

        return FindTmp(logPanel, "LogBody")
            ?? FindTmp(logPanel, "Bodytext")
            ?? FindTmp(logPanel, "BodyText")
            ?? FindTmp(logPanel, "Body");
    }

    /// <summary>
    /// 로그 본문이 DialogueLogPanel 밖으로 새지 않도록 마스크하고, 세로 스크롤로 전체를 볼 수 있게 합니다.
    /// </summary>
    public static void SetupLogPanelScrolling(GameObject logPanel, ref TextMeshProUGUI logBody)
    {
        if (logPanel == null)
            return;

        if (logBody == null)
            logBody = FindLogBodyText(logPanel.transform);
        if (logBody == null)
            return;

        var viewport = logPanel.transform.Find("LogViewport") as RectTransform;
        if (viewport == null)
        {
            var viewportGo = new GameObject("LogViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport = viewportGo.GetComponent<RectTransform>();
            viewport.SetParent(logPanel.transform, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.offsetMin = new Vector2(16f, 16f);
            viewport.offsetMax = new Vector2(-16f, -16f);

            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;
        }

        var closeLog = FindDeep(logPanel.transform, "CloseLog");
        if (closeLog != null && closeLog.parent == logPanel.transform)
        {
            viewport.anchorMin = new Vector2(0f, 0.14f);
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(16f, 8f);
            viewport.offsetMax = new Vector2(-16f, -16f);
            closeLog.SetAsLastSibling();
        }

        if (logBody.transform.parent != viewport)
            logBody.transform.SetParent(viewport, false);

        var bodyRect = logBody.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = new Vector2(0f, Mathf.Max(bodyRect.sizeDelta.y, 1f));

        logBody.alignment = TextAlignmentOptions.TopLeft;
        logBody.textWrappingMode = TextWrappingModes.Normal;
        logBody.overflowMode = TextOverflowModes.Overflow;
        logBody.enableAutoSizing = false;
        logBody.raycastTarget = true;
        logBody.maskable = true;

        var fitter = logBody.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = logBody.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = logPanel.GetComponent<ScrollRect>();
        if (scroll == null)
            scroll = logPanel.AddComponent<ScrollRect>();
        scroll.content = bodyRect;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = null;
        scroll.horizontalScrollbar = null;
    }

    public static void RefreshLogContentSize(TextMeshProUGUI logBody)
    {
        if (logBody == null)
            return;

        logBody.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(logBody.rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    public static TextMeshProUGUI CreateTmp(Transform parent, string name, float size, TextAlignmentOptions align, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = align;
        if (font != null)
            tmp.font = font;
        else if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        return tmp;
    }

    public static Button CreateUiButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.3f, 0.9f);
        var button = go.AddComponent<Button>();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = CreateTmp(go.transform, "Label", 20, TextAlignmentOptions.Center, font);
        text.text = label;
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static TextMeshProUGUI FindTmp(Transform root, string name)
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Button FindButton(Transform root, string name)
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
