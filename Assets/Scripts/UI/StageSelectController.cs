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
    /// 실제로 StartStage로 진입 가능한 스테이지 수.
    /// 현재는 Stage 1만 플레이 흐름이 구현되어 있습니다.
    /// Stage 2는 해금·선택 표시만 하고, 클릭 시에는 아직 진행하지 않습니다.
    /// </summary>
    public const int PlayableStageCount = 1;

    [Header("Stage Buttons")]
    [SerializeField] private StageSelectButtonEntry[] stageButtons;

    [Header("End Of Content")]
    [SerializeField] private GameObject endOfContentPopup;
    [SerializeField] private TextMeshProUGUI endOfContentText;
    [SerializeField] private string endOfContentMessage =
        "Current content ends at Stage 1-3.\nPlease wait for future updates.";

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

            // Stage 2/3도 해금되면 버튼은 눌러볼 수 있게 둡니다.
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
                    title += "\n(CLEAR)";

                entry.labelText.text = title;
            }
        }

        // Stage 1~3 전체를 다 깬 뒤에만 end-of-content (지금은 Stage1만 플레이 가능해도 팝업 안 띄움)
        if (StageProgressManager.HasCompletedAllImplementedStages())
            ShowEndOfContentPopup();
    }

    private void OnStageButtonClicked(int stageNumber)
    {
        if (!StageProgressManager.IsStageUnlocked(stageNumber))
            return;

        // 이번 범위: Stage 1만 실제 진행. Stage 2는 해금 확인용으로만 선택 가능.
        if (stageNumber > PlayableStageCount)
        {
            Debug.Log($"[StageSelect] Stage {stageNumber}는 해금됐지만, 아직 플레이 흐름은 구현되지 않았습니다.");
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
