using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 씬 흐름을 관리합니다.
/// Title → Prologue → StageSelect → Story(Open) → StagePlay → Story(Close) → StageSelect
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [SerializeField] private StageDatabase stageDatabase;

    public StageData CurrentStage { get; private set; }
    public StoryPhase CurrentStoryPhase { get; private set; }

    /// <summary>
    /// DDOL 인스턴스가 없으면 생성합니다.
    /// Title 없이 Prologue/StageSelect부터 Play해도 흐름이 이어지도록 합니다.
    /// </summary>
    public static GameFlowManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var existing = FindAnyObjectByType<GameFlowManager>();
        if (existing != null)
            return existing;

        var go = new GameObject(nameof(GameFlowManager));
        return go.AddComponent<GameFlowManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (stageDatabase == null)
            stageDatabase = Resources.Load<StageDatabase>("StageDatabase");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetStageDatabase(StageDatabase database)
    {
        if (database != null)
            stageDatabase = database;
    }

    public StageData GetStage(int stageNumber)
    {
        return stageDatabase != null ? stageDatabase.GetStage(stageNumber) : null;
    }

    public IReadOnlyList<StageData> GetAllStages()
    {
        return stageDatabase != null ? stageDatabase.Stages : System.Array.Empty<StageData>();
    }

    public void StartStage(int stageNumber)
    {
        CurrentStage = GetStage(stageNumber);

        if (CurrentStage == null)
        {
            Debug.LogError($"[GameFlowManager] Stage {stageNumber} 데이터를 찾을 수 없습니다.");
            return;
        }

        CurrentStoryPhase = StoryPhase.Opening;
        LoadScene(SceneNames.Story);
    }

    public void BeginOpeningStory()
    {
        CurrentStoryPhase = StoryPhase.Opening;
        LoadScene(SceneNames.Story);
    }

    public void BeginClosingStory()
    {
        CurrentStoryPhase = StoryPhase.Closing;
        LoadScene(SceneNames.Story);
    }

    public void GoToStagePlay()
    {
        if (CurrentStage == null)
        {
            Debug.LogError("[GameFlowManager] CurrentStage가 없어 StagePlayScene으로 이동할 수 없습니다.");
            return;
        }

        CurrentStoryPhase = StoryPhase.Playing;

        var sceneName = string.IsNullOrWhiteSpace(CurrentStage.playSceneName)
            ? SceneNames.StagePlay
            : CurrentStage.playSceneName;

        LoadScene(sceneName);
    }

    public void OnStagePlayCleared()
    {
        CurrentStoryPhase = StoryPhase.Closing;
        BeginClosingStory();
    }

    public void OnOpeningStoryFinished()
    {
        // Opening → Playing + StagePlayScene
        GoToStagePlay();
    }

    public void OnClosingStoryFinished()
    {
        if (CurrentStage == null)
        {
            LoadScene(SceneNames.StageSelect);
            return;
        }

        // Stage 1 클리어 저장 + 다음 스테이지(2) 해금
        StageProgressManager.MarkStageCleared(CurrentStage.stageNumber);
        LoadScene(SceneNames.StageSelect);
    }

    public void GoToStageSelect()
    {
        LoadScene(SceneNames.StageSelect);
    }

    public void GoToPrologue()
    {
        LoadScene(SceneNames.Prologue);
    }

    public void GoToTitle()
    {
        LoadScene(SceneNames.Title);
    }

    public string GetCurrentStagePlaySceneName()
    {
        if (CurrentStage != null && !string.IsNullOrWhiteSpace(CurrentStage.playSceneName))
            return CurrentStage.playSceneName;

        return SceneNames.StagePlay;
    }

    /// <summary>
    /// 현재 스테이지의 대화 데이터를 반환합니다.
    /// 에셋이 없으면 Stage 1~3 기본 대사를 런타임으로 만들어 줍니다.
    /// </summary>
    public StageDialogueData GetCurrentStageDialogueData()
    {
        if (CurrentStage != null && CurrentStage.dialogueData != null)
            return CurrentStage.dialogueData;

        if (CurrentStage != null)
        {
            Debug.LogWarning($"[GameFlowManager] Stage {CurrentStage.stageNumber}의 dialogueData가 비어 있어 기본 대사를 사용합니다. DevilMarriage/Create Dialogue Data 메뉴를 실행하세요.");
            return DialogueContentLibrary.CreateStageRuntime(CurrentStage.stageNumber);
        }

        return null;
    }

    /// <summary>
    /// 지금이 Opening(오픈) 대사인지 여부입니다.
    /// </summary>
    public bool IsOpeningStory => CurrentStoryPhase == StoryPhase.Opening;

    private static void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
