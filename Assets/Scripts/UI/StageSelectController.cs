using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StageSelectButtonEntry
{
    public int stageNumber;
    public Button button;
    public GameObject lockOverlay;
    public TextMeshProUGUI labelText;
}

public class StageSelectController : MonoBehaviour
{
    /// <summary>
    /// 실제로 StartStage로 진입 가능한 스테이지 수 (1~33).
    /// Stage 34는 해금만 하고 플레이는 이후 구현.
    /// </summary>
    public const int PlayableStageCount = 33;

    [Header("Stage Buttons")]
    [SerializeField] private StageSelectButtonEntry[] stageButtons;

    [Header("End Of Content")]
    [SerializeField] private GameObject endOfContentPopup;
    [SerializeField] private TextMeshProUGUI endOfContentText;
    [SerializeField] private string endOfContentMessage =
        "Stage 34는 아직 준비 중입니다.\n클리어한 Stage 1~33는 다시 플레이할 수 있습니다.";

    [Header("Settings")]
    [SerializeField] private GameObject settingPopup;

    [Header("Stage Scroll")]
    [SerializeField] private float stageButtonSpacing = 24f;
    [SerializeField] private float stageScrollPadding = 40f;

    private ScrollRect _stageScroll;
    private RectTransform _scrollContent;
    private float _stageButtonWidth = 200f;
    private float _stageButtonHeight = 200f;

    private void Awake()
    {
        GameFlowManager.EnsureExists();

        if (settingPopup == null)
            settingPopup = GameObject.Find("SettingPopup");

        if (settingPopup != null)
            settingPopup.SetActive(false);

        if (endOfContentPopup != null)
            endOfContentPopup.SetActive(false);

        AutoCreateStageButtonsFromTemplate();
        AutoWireStageButtonsIfNeeded();
        WireButtons();
        RefreshStageButtons();
        ScrollToLatestUnlockedStage();
    }

    private void AutoCreateStageButtonsFromTemplate()
    {
        var template = GameObject.Find("Stage1_Button");
        if (template == null)
            return;

        var templateRect = template.GetComponent<RectTransform>();
        if (templateRect == null)
            return;

        _stageButtonWidth = Mathf.Max(1f, templateRect.sizeDelta.x);
        _stageButtonHeight = Mathf.Max(1f, templateRect.sizeDelta.y);

        EnsureStageScrollView(templateRect);

        if (templateRect.parent != _scrollContent)
            PrepareStageButtonRect(templateRect);

        for (int stageNumber = 2; stageNumber <= StageProgressManager.ImplementedStageCount; stageNumber++)
        {
            var existing = GameObject.Find($"Stage{stageNumber}_Button");
            RectTransform cloneRect;

            if (existing != null)
            {
                cloneRect = existing.GetComponent<RectTransform>();
            }
            else
            {
                var clone = Instantiate(template, _scrollContent);
                clone.name = $"Stage{stageNumber}_Button";
                cloneRect = clone.GetComponent<RectTransform>();
            }

            if (cloneRect != null)
                PrepareStageButtonRect(cloneRect);
        }

        RefreshScrollContentSize();
    }

    private void EnsureStageScrollView(RectTransform templateRect)
    {
        if (_stageScroll != null && _scrollContent != null)
            return;

        var canvas = templateRect.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var existing = GameObject.Find("StageScrollView");
        if (existing != null)
        {
            _stageScroll = existing.GetComponent<ScrollRect>();
            _scrollContent = existing.transform.Find("Viewport/Content") as RectTransform;
            if (_stageScroll != null && _scrollContent != null)
                return;
        }

        var scrollObject = new GameObject("StageScrollView", typeof(RectTransform));
        scrollObject.transform.SetParent(canvas.transform, false);
        int backgroundIndex = -1;
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            if (canvas.transform.GetChild(i).name == "BackGround")
            {
                backgroundIndex = i;
                break;
            }
        }

        scrollObject.transform.SetSiblingIndex(backgroundIndex >= 0 ? backgroundIndex + 1 : 0);
        if (settingPopup != null)
            settingPopup.transform.SetAsLastSibling();

        var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0.18f);
        scrollRectTransform.anchorMax = new Vector2(1f, 0.58f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);

        var scrollBackground = scrollObject.AddComponent<Image>();
        scrollBackground.color = new Color(0f, 0f, 0f, 0f);
        scrollBackground.raycastTarget = true;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0.18f);
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);

        var viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;

        var mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform, false);
        _scrollContent = contentObject.GetComponent<RectTransform>();
        _scrollContent.anchorMin = new Vector2(0f, 0.5f);
        _scrollContent.anchorMax = new Vector2(0f, 0.5f);
        _scrollContent.pivot = new Vector2(0f, 0.5f);
        _scrollContent.anchoredPosition = Vector2.zero;
        _scrollContent.sizeDelta = new Vector2(0f, _stageButtonHeight + 24f);

        var scrollbar = CreateHorizontalScrollbar(scrollObject.transform);

        _stageScroll = scrollObject.AddComponent<ScrollRect>();
        _stageScroll.content = _scrollContent;
        _stageScroll.viewport = viewportRect;
        _stageScroll.horizontal = true;
        _stageScroll.vertical = false;
        _stageScroll.movementType = ScrollRect.MovementType.Clamped;
        _stageScroll.inertia = true;
        _stageScroll.decelerationRate = 0.135f;
        _stageScroll.scrollSensitivity = 40f;
        _stageScroll.horizontalScrollbar = scrollbar;
        _stageScroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        _stageScroll.horizontalScrollbarSpacing = 8f;
    }

    private Scrollbar CreateHorizontalScrollbar(Transform parent)
    {
        var barObject = new GameObject("Scrollbar", typeof(RectTransform));
        barObject.transform.SetParent(parent, false);

        var barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.08f, 0f);
        barRect.anchorMax = new Vector2(0.92f, 0.16f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;
        barRect.pivot = new Vector2(0.5f, 0.5f);

        var barImage = barObject.AddComponent<Image>();
        barImage.color = new Color(0.12f, 0.1f, 0.14f, 0.85f);

        var slidingObject = new GameObject("Sliding Area", typeof(RectTransform));
        slidingObject.transform.SetParent(barObject.transform, false);
        var slidingRect = slidingObject.GetComponent<RectTransform>();
        slidingRect.anchorMin = Vector2.zero;
        slidingRect.anchorMax = Vector2.one;
        slidingRect.offsetMin = new Vector2(10f, 6f);
        slidingRect.offsetMax = new Vector2(-10f, -6f);

        var handleObject = new GameObject("Handle", typeof(RectTransform));
        handleObject.transform.SetParent(slidingObject.transform, false);
        var handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        var handleImage = handleObject.AddComponent<Image>();
        handleImage.color = new Color(0.82f, 0.72f, 0.55f, 0.95f);

        var scrollbar = barObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.LeftToRight;
        scrollbar.size = 0.35f;
        scrollbar.numberOfSteps = 0;
        return scrollbar;
    }

    private void PrepareStageButtonRect(RectTransform buttonRect)
    {
        if (buttonRect == null || _scrollContent == null)
            return;

        buttonRect.SetParent(_scrollContent, false);
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(0f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(_stageButtonWidth, _stageButtonHeight);
        buttonRect.localScale = Vector3.one;
        buttonRect.localRotation = Quaternion.identity;
    }

    private void RefreshScrollContentSize()
    {
        if (_scrollContent == null)
            return;

        int count = StageProgressManager.ImplementedStageCount;
        float width = stageScrollPadding * 2f
                      + count * _stageButtonWidth
                      + Mathf.Max(0, count - 1) * stageButtonSpacing;
        _scrollContent.sizeDelta = new Vector2(width, _stageButtonHeight + 24f);

        for (int i = 0; i < _scrollContent.childCount; i++)
        {
            var child = _scrollContent.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            float x = stageScrollPadding + _stageButtonWidth * 0.5f + i * (_stageButtonWidth + stageButtonSpacing);
            child.anchoredPosition = new Vector2(x, 0f);
        }
    }

    private void ScrollToLatestUnlockedStage()
    {
        if (_stageScroll == null || _scrollContent == null)
            return;

        Canvas.ForceUpdateCanvases();
        RefreshScrollContentSize();

        var viewport = _stageScroll.viewport;
        float viewportWidth = viewport != null ? viewport.rect.width : 0f;
        float contentWidth = _scrollContent.rect.width;
        float overflow = contentWidth - viewportWidth;
        if (overflow <= 1f)
        {
            _stageScroll.horizontalNormalizedPosition = 0f;
            return;
        }

        int unlocked = Mathf.Clamp(
            StageProgressManager.HighestUnlockedStage,
            1,
            StageProgressManager.ImplementedStageCount);

        float targetCenter = stageScrollPadding
                             + _stageButtonWidth * 0.5f
                             + (unlocked - 1) * (_stageButtonWidth + stageButtonSpacing);
        float desiredContentX = Mathf.Clamp(targetCenter - viewportWidth * 0.5f, 0f, overflow);
        _stageScroll.horizontalNormalizedPosition = desiredContentX / overflow;
    }

    private void AutoWireStageButtonsIfNeeded()
    {
        if (stageButtons != null && stageButtons.Length > 0)
            return;

        var entries = new System.Collections.Generic.List<StageSelectButtonEntry>();
        for (int i = 1; i <= StageProgressManager.ImplementedStageCount; i++)
        {
            var buttonObject = GameObject.Find($"Stage{i}_Button");
            if (buttonObject == null)
                continue;

            entries.Add(new StageSelectButtonEntry
            {
                stageNumber = i,
                button = buttonObject.GetComponent<Button>(),
                lockOverlay = buttonObject.transform.Find("LockOverlay")?.gameObject,
                labelText = buttonObject.GetComponentInChildren<TextMeshProUGUI>()
            });
        }

        stageButtons = entries.ToArray();
    }

    private void WireButtons()
    {
        BindNamedButton("Title_Button", () =>
        {
            GameFlowManager.EnsureExists()?.GoToTitle();
        });

        BindNamedButton("Option_Button", OpenSettingsPopup);

        if (stageButtons == null)
            return;

        foreach (var entry in stageButtons)
        {
            if (entry?.button == null)
                continue;

            int stageNumber = entry.stageNumber;
            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => OnStageButtonClicked(stageNumber));
        }
    }

    private void BindNamedButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
            return;

        var button = buttonObject.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void RefreshStageButtons()
    {
        if (stageButtons == null)
            return;

        foreach (var entry in stageButtons)
        {
            if (entry == null)
                continue;

            bool isUnlocked = StageProgressManager.IsStageUnlocked(entry.stageNumber);
            bool isCleared = StageProgressManager.IsStageCleared(entry.stageNumber);

            // Stage 2~5도 해금되면 버튼은 눌러볼 수 있게 둡니다.
            // (실제 스테이지 시작은 OnStageButtonClicked에서 PlayableStageCount로 제한)
            if (entry.button != null)
                entry.button.interactable = isUnlocked;

            if (entry.lockOverlay != null)
                entry.lockOverlay.SetActive(!isUnlocked);

            if (entry.labelText != null)
            {
                var stageData = GameFlowManager.Instance?.GetStage(entry.stageNumber);
                string title = stageData != null
                    ? $"Stage {entry.stageNumber}\n{stageData.stageName}"
                    : $"Stage {entry.stageNumber}";

                if (isCleared)
                    title += "\n(CLEAR · 재도전 가능)";

                entry.labelText.text = title;
            }
        }

        // 전체 클리어 후에도 스테이지 선택/재도전은 막지 않습니다.
        // (미구현 스테이지 클릭 시에만 안내 팝업을 띄웁니다.)
    }

    private void OnStageButtonClicked(int stageNumber)
    {
        if (!StageProgressManager.IsStageUnlocked(stageNumber))
            return;

        // 클리어한 스테이지도 다시 플레이 가능
        if (stageNumber > PlayableStageCount)
        {
            ShowEndOfContentPopup();
            return;
        }

        if (GameFlowManager.EnsureExists() != null)
            GameFlowManager.Instance.StartStage(stageNumber);
        else
            Debug.LogError("[StageSelectController] GameFlowManager를 생성할 수 없습니다.");
    }

    public void ShowEndOfContentPopup()
    {
        EnsureEndOfContentPopup();

        if (endOfContentPopup == null)
            return;

        if (endOfContentText != null)
            endOfContentText.text = endOfContentMessage;

        endOfContentPopup.SetActive(true);
    }

    private void EnsureEndOfContentPopup()
    {
        if (endOfContentPopup != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        endOfContentPopup = new GameObject("EndOfContentPopup");
        endOfContentPopup.transform.SetParent(canvas.transform, false);

        var panelImage = endOfContentPopup.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        var panelRect = endOfContentPopup.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var textObject = new GameObject("Message");
        textObject.transform.SetParent(endOfContentPopup.transform, false);
        endOfContentText = textObject.AddComponent<TextMeshProUGUI>();
        endOfContentText.alignment = TextAlignmentOptions.Center;
        endOfContentText.fontSize = 28;
        endOfContentText.color = Color.white;
        endOfContentText.text = endOfContentMessage;

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.35f);
        textRect.anchorMax = new Vector2(0.9f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var closeButtonObject = new GameObject("CloseButton");
        closeButtonObject.transform.SetParent(endOfContentPopup.transform, false);
        var closeButton = closeButtonObject.AddComponent<Button>();
        closeButton.onClick.AddListener(CloseEndOfContentPopup);

        var closeButtonRect = closeButtonObject.GetComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(0.4f, 0.2f);
        closeButtonRect.anchorMax = new Vector2(0.6f, 0.28f);
        closeButtonRect.offsetMin = Vector2.zero;
        closeButtonRect.offsetMax = Vector2.zero;

        var closeButtonImage = closeButtonObject.AddComponent<Image>();
        closeButtonImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var closeLabelObject = new GameObject("Label");
        closeLabelObject.transform.SetParent(closeButtonObject.transform, false);
        var closeLabel = closeLabelObject.AddComponent<TextMeshProUGUI>();
        closeLabel.text = "OK";
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.fontSize = 24;
        closeLabel.color = Color.white;

        var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
        closeLabelRect.anchorMin = Vector2.zero;
        closeLabelRect.anchorMax = Vector2.one;
        closeLabelRect.offsetMin = Vector2.zero;
        closeLabelRect.offsetMax = Vector2.zero;

        endOfContentPopup.SetActive(false);
    }

    public void CloseEndOfContentPopup()
    {
        if (endOfContentPopup != null)
            endOfContentPopup.SetActive(false);
    }

    public void OpenSettingsPopup()
    {
        if (settingPopup != null)
            settingPopup.SetActive(true);
    }

    public void CloseSettingsPopup()
    {
        if (settingPopup != null)
            settingPopup.SetActive(false);

        var soundSettings = FindAnyObjectByType<SoundSettings>();
        if (soundSettings != null)
            soundSettings.SaveSoundSettings();
    }
}
