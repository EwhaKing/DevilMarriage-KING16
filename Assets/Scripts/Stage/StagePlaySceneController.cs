using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StagePlaySceneController : MonoBehaviour
{
    [SerializeField] private Stage1PuzzleController puzzleController;
    [SerializeField] private StageResourceManager resourceManager;
    [SerializeField] private StageHUD stageHud;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private StageBgmPlayer bgmPlayer;

    [Header("Retry UI")]
    [SerializeField] private GameObject retryPopup;
    [SerializeField] private Button retryButton;
    [SerializeField] private TextMeshProUGUI retryMessageText;
    [SerializeField] private string retryMessage = "정신력이 모두 소진되었습니다.\n다시 도전하시겠습니까?";

    private void Awake()
    {
        if (puzzleController == null)
            puzzleController = FindAnyObjectByType<Stage1PuzzleController>();

        if (resourceManager == null)
            resourceManager = FindAnyObjectByType<StageResourceManager>();

        if (stageHud == null)
            stageHud = FindAnyObjectByType<StageHUD>();

        if (bgmPlayer == null)
            bgmPlayer = FindAnyObjectByType<StageBgmPlayer>();
    }

    private void Start()
    {
        ApplyCurrentStageData();
        EnsureRetryPopup();

        if (retryPopup != null)
            retryPopup.SetActive(false);

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryCurrentStage);
        }

        if (resourceManager != null)
            resourceManager.OnGameOver += ShowRetryPopup;

        if (puzzleController != null)
            puzzleController.UseGameFlowManager = true;

        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage != null && stage.stageNumber == 1 && GetComponent<Stage1PlayTutorialController>() == null)
            gameObject.AddComponent<Stage1PlayTutorialController>();

        if (stage != null && stage.stageNumber == 4 && GetComponent<Stage4PlayIntroController>() == null)
            gameObject.AddComponent<Stage4PlayIntroController>();
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
            resourceManager.OnGameOver -= ShowRetryPopup;
    }

    private void ApplyCurrentStageData()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage == null)
            return;

        if (backgroundImage != null && stage.backgroundImage != null)
            backgroundImage.sprite = stage.backgroundImage;

        if (bgmPlayer != null && stage.backgroundMusic != null)
            bgmPlayer.SetBgmClip(stage.backgroundMusic);

        var playData = stage.playData;
        if (playData == null)
            return;

        if (stageHud != null)
            stageHud.ApplyPlayData(playData);

        if (resourceManager != null)
            resourceManager.ApplyPlayData(playData);
    }

    private void ShowRetryPopup()
    {
        EnsureRetryPopup();

        if (puzzleController != null)
            puzzleController.InputLocked = true;

        if (retryMessageText != null)
            retryMessageText.text = retryMessage;

        if (retryPopup != null)
            retryPopup.SetActive(true);
    }

    private void RetryCurrentStage()
    {
        if (retryPopup != null)
            retryPopup.SetActive(false);

        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (resourceManager != null)
        {
            if (stage != null && stage.playData != null)
                resourceManager.ApplyPlayData(stage.playData);
            else
                resourceManager.ResetResources();
        }

        if (puzzleController != null)
        {
            puzzleController.RestartStage();
            var stageNum = stage != null ? stage.stageNumber : 0;
            if (stageNum == 4)
                puzzleController.ConfigureSanityHazardsForStage4();
        }
    }

    private void EnsureRetryPopup()
    {
        if (retryPopup != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        retryPopup = new GameObject("RetryPopup", typeof(RectTransform), typeof(Image));
        retryPopup.transform.SetParent(canvas.transform, false);

        var panelRect = retryPopup.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = retryPopup.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(retryPopup.transform, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(520f, 240f);
        box.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.14f, 0.95f);

        var messageObject = new GameObject("Message", typeof(RectTransform));
        messageObject.transform.SetParent(box.transform, false);
        var messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.1f, 0.4f);
        messageRect.anchorMax = new Vector2(0.9f, 0.9f);
        messageRect.offsetMin = Vector2.zero;
        messageRect.offsetMax = Vector2.zero;
        retryMessageText = messageObject.AddComponent<TextMeshProUGUI>();
        retryMessageText.text = retryMessage;
        retryMessageText.alignment = TextAlignmentOptions.Center;
        retryMessageText.fontSize = 28f;
        retryMessageText.color = Color.white;

        var buttonObject = new GameObject("RetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(box.transform, false);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.12f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.12f);
        buttonRect.sizeDelta = new Vector2(180f, 48f);
        buttonObject.GetComponent<Image>().color = new Color(0.55f, 0.2f, 0.25f, 1f);
        retryButton = buttonObject.GetComponent<Button>();

        var buttonLabelObject = new GameObject("Label", typeof(RectTransform));
        buttonLabelObject.transform.SetParent(buttonObject.transform, false);
        var buttonLabelRect = buttonLabelObject.GetComponent<RectTransform>();
        buttonLabelRect.anchorMin = Vector2.zero;
        buttonLabelRect.anchorMax = Vector2.one;
        buttonLabelRect.offsetMin = Vector2.zero;
        buttonLabelRect.offsetMax = Vector2.zero;
        var buttonLabel = buttonLabelObject.AddComponent<TextMeshProUGUI>();
        buttonLabel.text = "재도전";
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.fontSize = 24f;
        buttonLabel.color = Color.white;
    }
}
