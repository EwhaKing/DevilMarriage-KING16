using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 자원(정신력, 쥐의 피)을 관리한다.
/// </summary>
public class StageResourceManager : MonoBehaviour
{
    public static StageResourceManager Instance { get; private set; }

    [Header("Sanity")]
    [SerializeField] private int maxSanity = 100;
    [SerializeField] private int sanityLossWrongStroke = 10;
    [SerializeField] private int sanityLossForbiddenRune = 15;
    [SerializeField] private int sanityLossUndo = 10;

    [Header("Rat Blood")]
    [SerializeField] private int maxRatBlood = 15;

    [Header("Game Over")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";
    [SerializeField] private float gameOverDelay = 0.5f;

    public int MaxSanity => maxSanity;
    public int CurrentSanity { get; private set; }
    public int MaxRatBlood => maxRatBlood;
    public int CurrentRatBlood { get; private set; }
    public bool IsGameOver => _isGameOver;

    public event Action<int, int> OnSanityChanged;
    public event Action<int, int> OnRatBloodChanged;
    public event Action OnGameOver;

    private bool _isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetResources();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResetResources()
    {
        CurrentSanity = maxSanity;
        CurrentRatBlood = maxRatBlood;
        _isGameOver = false;

        OnSanityChanged?.Invoke(CurrentSanity, maxSanity);
        OnRatBloodChanged?.Invoke(CurrentRatBlood, maxRatBlood);
    }

    public bool HasRatBlood(int amount = 1)
    {
        return CurrentRatBlood >= amount;
    }

    public bool TrySpendRatBlood(int amount = 1)
    {
        if (amount <= 0)
            return true;

        if (CurrentRatBlood < amount)
            return false;

        CurrentRatBlood -= amount;
        OnRatBloodChanged?.Invoke(CurrentRatBlood, maxRatBlood);
        return true;
    }

    public void RestoreRatBlood(int amount)
    {
        if (amount <= 0)
            return;

        CurrentRatBlood = Mathf.Min(maxRatBlood, CurrentRatBlood + amount);
        OnRatBloodChanged?.Invoke(CurrentRatBlood, maxRatBlood);
    }

    public void ReduceSanity(int amount)
    {
        if (_isGameOver || amount <= 0)
            return;

        CurrentSanity = Mathf.Max(0, CurrentSanity - amount);
        OnSanityChanged?.Invoke(CurrentSanity, maxSanity);

        if (CurrentSanity <= 0)
            TriggerGameOver();
    }

    public void OnWrongStroke()
    {
        ReduceSanity(sanityLossWrongStroke);
    }

    public void OnForbiddenRune()
    {
        ReduceSanity(sanityLossForbiddenRune);
    }

    public void OnUndo()
    {
        ReduceSanity(sanityLossUndo);
    }

    private void TriggerGameOver()
    {
        if (_isGameOver)
            return;

        _isGameOver = true;
        OnGameOver?.Invoke();
        Invoke(nameof(LoadGameOverScene), gameOverDelay);
    }

    private void LoadGameOverScene()
    {
        if (Application.CanStreamedLevelBeLoaded(gameOverSceneName))
        {
            SceneManager.LoadScene(gameOverSceneName);
            return;
        }

#if UNITY_EDITOR
        var scenePath = $"Assets/Scenes/{gameOverSceneName}.unity";
        if (System.IO.File.Exists(scenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        Debug.LogError(
            $"[StageResourceManager] '{gameOverSceneName}' 씬을 불러올 수 없습니다. " +
            "Unity에서 GameOverScene.unity를 연 뒤 Build Profiles → Add Open Scenes를 눌러 등록하세요.");
    }
}
