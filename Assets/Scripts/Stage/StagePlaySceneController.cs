using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// StagePlayScene 진입 시 현재 스테이지의 Puzzle Prefab을 생성하고,
/// HUD·리소스·튜토리얼을 연결합니다.
/// </summary>
public class StagePlaySceneController : MonoBehaviour
{
    [SerializeField] private Stage1PuzzleController puzzleController;
    [SerializeField] private StageResourceManager resourceManager;
    [SerializeField] private StageHUD stageHud;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private StageBgmPlayer bgmPlayer;

    [Header("Puzzle Spawn")]
    [Tooltip("씬에 미리 배치된 기본 퍼즐(없으면 자동 탐색). Prefab이 지정되면 비활성화됩니다.")]
    [SerializeField] private GameObject defaultPuzzleRoot;

    [Tooltip("퍼즐 Prefab이 생성될 부모. 비우면 씬 루트.")]
    [SerializeField] private Transform puzzleSpawnParent;

    [Header("Retry UI")]
    [SerializeField] private GameObject retryPopup;
    [SerializeField] private Button retryButton;
    [SerializeField] private TextMeshProUGUI retryMessageText;
    [SerializeField] private string retryMessage = "정신력이 모두 소진되었습니다.\n다시 도전하시겠습니까?";

    private GameObject _spawnedPuzzle;
    private Transform _playerTransform;
    private StagePlayerAnimationController _playerAnimation;
    private DemonSummonSkillPanel _demonSummonSkillPanel;

    private void Awake()
    {
        if (resourceManager == null)
            resourceManager = FindAnyObjectByType<StageResourceManager>();

        if (stageHud == null)
            stageHud = FindAnyObjectByType<StageHUD>();

        if (bgmPlayer == null)
            bgmPlayer = FindAnyObjectByType<StageBgmPlayer>();

        if (defaultPuzzleRoot == null)
        {
            var existing = FindAnyObjectByType<Stage1PuzzleController>();
            if (existing != null)
                defaultPuzzleRoot = existing.gameObject;
        }

        CachePlayer();
        SpawnPuzzleForCurrentStage();

        if (puzzleController == null)
            puzzleController = FindAnyObjectByType<Stage1PuzzleController>();
    }

    private void Start()
    {
        ApplyCurrentStageData();

        if (resourceManager != null)
            resourceManager.OnGameOver += GoToGameOverScene;

        if (puzzleController != null)
            puzzleController.UseGameFlowManager = true;

        SetupDemonSummonSkillPanel();

        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage != null && stage.stageNumber == 2 && GetComponent<Stage2PlayIntroController>() == null)
            gameObject.AddComponent<Stage2PlayIntroController>();

        if (stage != null && stage.stageNumber == 1 && GetComponent<Stage1PlayTutorialController>() == null)
            gameObject.AddComponent<Stage1PlayTutorialController>();

        if (stage != null && stage.stageNumber == 4 && GetComponent<Stage4PlayIntroController>() == null)
            gameObject.AddComponent<Stage4PlayIntroController>();

        if (stage != null && stage.stageNumber == 10 && GetComponent<Stage10PlayIntroController>() == null)
            gameObject.AddComponent<Stage10PlayIntroController>();

        if (stage != null && stage.stageNumber == 13 && GetComponent<Stage13PlayIntroController>() == null)
            gameObject.AddComponent<Stage13PlayIntroController>();

        if (stage != null && stage.stageNumber == 21 && GetComponent<Stage21PlayIntroController>() == null)
            gameObject.AddComponent<Stage21PlayIntroController>();

        if (stage != null && stage.stageNumber == 22 && GetComponent<Stage22PlayIntroController>() == null)
            gameObject.AddComponent<Stage22PlayIntroController>();

        if (stage != null && stage.stageNumber == 24 && GetComponent<Stage24PlayIntroController>() == null)
            gameObject.AddComponent<Stage24PlayIntroController>();

        if (stage != null && stage.stageNumber == 27 && GetComponent<Stage27PlayIntroController>() == null)
            gameObject.AddComponent<Stage27PlayIntroController>();

        if (stage != null && stage.stageNumber == 28 && GetComponent<Stage28PlayIntroController>() == null)
            gameObject.AddComponent<Stage28PlayIntroController>();

        if (stage != null && stage.stageNumber == 29 && GetComponent<Stage29PlayIntroController>() == null)
            gameObject.AddComponent<Stage29PlayIntroController>();

        if (stage != null && stage.stageNumber == 31 && GetComponent<Stage31PlayIntroController>() == null)
            gameObject.AddComponent<Stage31PlayIntroController>();

        if (stage != null && stage.stageNumber == 33 && GetComponent<Stage33PlayIntroController>() == null)
            gameObject.AddComponent<Stage33PlayIntroController>();
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
            resourceManager.OnGameOver -= GoToGameOverScene;
    }

    private void CachePlayer()
    {
        var playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            _playerTransform = playerObject.transform;
            _playerAnimation = playerObject.GetComponent<StagePlayerAnimationController>();
        }
    }

    /// <summary>
    /// StagePlayData.puzzlePrefab이 있으면 해당 Prefab만 생성하고,
    /// 씬 기본 퍼즐은 끕니다. Prefab이 없으면 씬 기본 퍼즐을 그대로 씁니다.
    /// </summary>
    private void SpawnPuzzleForCurrentStage()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        var playData = stage != null ? stage.playData : null;
        var prefab = playData != null ? playData.puzzlePrefab : null;

        if (prefab == null)
        {
            Debug.LogWarning(
                $"[StagePlay] Stage {(stage != null ? stage.stageNumber.ToString() : "?")} PlayData에 Puzzle Prefab이 없습니다. " +
                "씬 기본 Stage1Puzzle을 사용합니다. Assets/Data/PlayData 에서 Puzzle Prefab을 연결하세요.");

            if (defaultPuzzleRoot != null)
                defaultPuzzleRoot.SetActive(true);

            puzzleController = defaultPuzzleRoot != null
                ? defaultPuzzleRoot.GetComponent<Stage1PuzzleController>()
                : FindAnyObjectByType<Stage1PuzzleController>();

            if (puzzleController != null)
                puzzleController.BindExternalReferences(_playerTransform, resourceManager, _playerAnimation);

            return;
        }

        if (defaultPuzzleRoot != null)
            defaultPuzzleRoot.SetActive(false);

        if (_spawnedPuzzle != null)
            Destroy(_spawnedPuzzle);

        _spawnedPuzzle = Instantiate(prefab, puzzleSpawnParent);
        _spawnedPuzzle.name = prefab.name;
        puzzleController = _spawnedPuzzle.GetComponent<Stage1PuzzleController>();
        if (puzzleController == null)
            puzzleController = _spawnedPuzzle.GetComponentInChildren<Stage1PuzzleController>();

        if (puzzleController == null)
        {
            Debug.LogError($"[StagePlay] Prefab '{prefab.name}'에 Stage1PuzzleController가 없습니다.", prefab);
            return;
        }

        puzzleController.BindExternalReferences(_playerTransform, resourceManager, _playerAnimation);
        puzzleController.RefreshRuneAndEdgeCache();
    }

    private void SetupDemonSummonSkillPanel()
    {
        var canvas = stageHud != null ? stageHud.GetComponent<Canvas>() : null;
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();

        _demonSummonSkillPanel = DemonSummonSkillPanel.EnsureOnCanvas(canvas, DemonSkillCatalog.Load());
        if (_demonSummonSkillPanel == null)
            return;

        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        int stageNumber = stage != null ? stage.stageNumber : 0;
        var filter = stage != null && stage.playData != null ? stage.playData.availableDemonSkills : null;
        _demonSummonSkillPanel.BindForStage(stageNumber, filter);
        _demonSummonSkillPanel.transform.SetAsLastSibling();
        if (retryPopup != null)
            retryPopup.transform.SetAsLastSibling();
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

        if (puzzleController != null)
            puzzleController.ApplyPlaySettings(playData);

        SyncRatBloodToPathCount();
    }

    private void SyncRatBloodToPathCount()
    {
        if (resourceManager == null || puzzleController == null)
            return;

        puzzleController.RefreshRuneAndEdgeCache();
        int pathCount = puzzleController.CountPaths();
        if (pathCount <= 0)
            return;

        resourceManager.SetRatBloodCapacity(pathCount);
    }

    private void GoToGameOverScene()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnStagePlayFailed();
        else
            SceneManager.LoadScene(SceneNames.GameOver);
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

            SyncRatBloodToPathCount();
        }

        if (puzzleController != null)
        {
            puzzleController.RestartStage();
            var stageNum = stage != null ? stage.stageNumber : 0;
            if (stageNum == 4)
                puzzleController.ConfigureSanityHazardsForStage4();
        }

        if (_demonSummonSkillPanel != null)
            _demonSummonSkillPanel.ResetUsesForCurrentAttempt();
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
