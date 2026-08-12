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
    /// 실제로 StartStage로 진입 가능한 스테이지 수 (1~5).
    /// Stage 6는 해금만 하고 플레이는 이후 구현.
    /// </summary>
    public const int PlayableStageCount = 5;

    [Header("Stage Buttons")]
    [SerializeField] private StageSelectButtonEntry[] stageButtons;

    [Header("End Of Content")]
    [SerializeField] private GameObject endOfContentPopup;
    [SerializeField] private TextMeshProUGUI endOfContentText;
    [SerializeField] private string endOfContentMessage =
        "Stage 6는 아직 준비 중입니다.\n클리어한 Stage 1~5는 다시 플레이할 수 있습니다.";

    [Header("Settings")]
    [SerializeField] private GameObject settingPopup;

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
    }

    private void AutoCreateStageButtonsFromTemplate()
    {
        var template = GameObject.Find("Stage1_Button");
        if (template == null)
            return;

        var templateRect = template.GetComponent<RectTransform>();
        if (templateRect == null)
            return;

        for (int stageNumber = 2; stageNumber <= StageProgressManager.ImplementedStageCount; stageNumber++)
        {
            if (GameObject.Find($"Stage{stageNumber}_Button") != null)
                continue;

            var clone = Instantiate(template, templateRect.parent);
            clone.name = $"Stage{stageNumber}_Button";

            var cloneRect = clone.GetComponent<RectTransform>();
            if (cloneRect != null)
            {
                cloneRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(
                    (stageNumber - 1) * (templateRect.sizeDelta.x + 24f),
                    0f);
            }
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
