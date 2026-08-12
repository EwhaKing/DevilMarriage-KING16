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
    /// 실제로 StartStage로 진입 가능한 스테이지 수 (1~10).
    /// Stage 11는 해금만 하고 플레이는 이후 구현.
    /// </summary>
    public const int PlayableStageCount = 10;

    [Header("Stage Buttons")]
    [SerializeField] private StageSelectButtonEntry[] stageButtons;

    [Header("End Of Content")]
    [SerializeField] private GameObject endOfContentPopup;
    [SerializeField] private TextMeshProUGUI endOfContentText;
    [SerializeField] private string endOfContentMessage =
        "Stage 11는 아직 준비 중입니다.\n클리어한 Stage 1~10는 다시 플레이할 수 있습니다.";

    [Header("Settings")]
    [SerializeField] private GameObject settingPopup;

    [Header("View Pan")]
    [Tooltip("한 화면에 스크롤 없이 보이는 스테이지 개수. 이보다 많이 해금되면 뷰가 우측으로 이동합니다.")]
    [SerializeField] private int visibleStageSlots = 5;
    [Tooltip("카메라 X 이동량(월드 단위) / 스테이지 1칸. Screen Space UI는 버튼 루트도 함께 이동합니다.")]
    [SerializeField] private float cameraUnitsPerStage = 2.24f;

    private RectTransform _stagesRoot;
    private Vector3 _baseCameraPosition;
    private float _stageSpacing = 224f;
    private Vector2 _stagesRootBasePosition;

    private void Awake()
    {
        GameFlowManager.EnsureExists();

        if (settingPopup == null)
            settingPopup = GameObject.Find("SettingPopup");

        if (settingPopup != null)
            settingPopup.SetActive(false);

        if (endOfContentPopup != null)
            endOfContentPopup.SetActive(false);

        if (Camera.main != null)
            _baseCameraPosition = Camera.main.transform.position;

        AutoCreateStageButtonsFromTemplate();
        AutoWireStageButtonsIfNeeded();
        WireButtons();
        RefreshStageButtons();
        AlignViewToUnlockProgress();
    }

    private void AutoCreateStageButtonsFromTemplate()
    {
        var template = GameObject.Find("Stage1_Button");
        if (template == null)
            return;

        var templateRect = template.GetComponent<RectTransform>();
        if (templateRect == null)
            return;

        EnsureStagesRoot(templateRect);
        _stageSpacing = templateRect.sizeDelta.x + 24f;

        var stage1BaseLocal = _stagesRoot.InverseTransformPoint(templateRect.position);
        // Stage1이 아직 BackGround 자식이면 StagesRoot로 옮기고 위치를 유지합니다.
        if (templateRect.parent != _stagesRoot)
        {
            templateRect.SetParent(_stagesRoot, true);
            templateRect.anchoredPosition = stage1BaseLocal;
        }

        Vector2 stage1Pos = templateRect.anchoredPosition;

        for (int stageNumber = 2; stageNumber <= StageProgressManager.ImplementedStageCount; stageNumber++)
        {
            var existing = GameObject.Find($"Stage{stageNumber}_Button");
            RectTransform cloneRect;

            if (existing != null)
            {
                cloneRect = existing.GetComponent<RectTransform>();
                if (cloneRect != null && cloneRect.parent != _stagesRoot)
                    cloneRect.SetParent(_stagesRoot, true);
            }
            else
            {
                var clone = Instantiate(template, _stagesRoot);
                clone.name = $"Stage{stageNumber}_Button";
                cloneRect = clone.GetComponent<RectTransform>();
            }

            if (cloneRect != null)
            {
                cloneRect.anchoredPosition = stage1Pos + new Vector2(
                    (stageNumber - 1) * _stageSpacing,
                    0f);
            }
        }
    }

    private void EnsureStagesRoot(RectTransform templateRect)
    {
        if (_stagesRoot != null)
            return;

        var existing = GameObject.Find("StagesRoot");
        if (existing != null)
        {
            _stagesRoot = existing.GetComponent<RectTransform>();
            if (_stagesRoot != null)
            {
                _stagesRootBasePosition = _stagesRoot.anchoredPosition;
                return;
            }
        }

        var rootObject = new GameObject("StagesRoot", typeof(RectTransform));
        _stagesRoot = rootObject.GetComponent<RectTransform>();
        _stagesRoot.SetParent(templateRect.parent, false);
        _stagesRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _stagesRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _stagesRoot.pivot = new Vector2(0.5f, 0.5f);
        _stagesRoot.sizeDelta = Vector2.zero;
        _stagesRoot.anchoredPosition = Vector2.zero;
        _stagesRootBasePosition = Vector2.zero;
        _stagesRoot.SetSiblingIndex(templateRect.GetSiblingIndex());
    }

    /// <summary>
    /// 해금된 최신 스테이지가 보이도록 스테이지 버튼 행을 왼쪽으로 밀고,
    /// 카메라도 같은 진행도만큼 오른쪽으로 옮깁니다.
    /// </summary>
    private void AlignViewToUnlockProgress()
    {
        int unlocked = Mathf.Clamp(
            StageProgressManager.HighestUnlockedStage,
            1,
            StageProgressManager.ImplementedStageCount);

        int slots = Mathf.Max(1, visibleStageSlots);
        int scrollSteps = Mathf.Max(0, unlocked - slots);
        float uiOffset = scrollSteps * _stageSpacing;

        if (_stagesRoot != null)
            _stagesRoot.anchoredPosition = _stagesRootBasePosition + new Vector2(-uiOffset, 0f);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(
                _baseCameraPosition.x + scrollSteps * cameraUnitsPerStage,
                _baseCameraPosition.y,
                _baseCameraPosition.z);
        }
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
