using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MoveFailReason
{
    None,
    Busy,
    SameRune,
    NoPath,
    ForbiddenRune,
    NoBlood,
    StageEnded,
    GameOver
}

/// <summary>
/// 스테이지1: 미리 그려진 경로만 따라 이동.
/// 이동 시 쥐의 피 -1, 바로 직전 룬으로 되돌아가면 경로 해제·피 +1·정신력 -10.
/// 이미 붉은 경로를 다시 지나가면 경로 해제·피 +1·정신력 -10 (피는 추가로 깎이지 않음).
/// </summary>
public class Stage1PuzzleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RuneNode[] runes;
    [SerializeField] private RunePathEdge[] pathEdges;
    [SerializeField] private Transform player;
    [SerializeField] private StageResourceManager resourceManager;

    [Header("Movement")]
    [SerializeField] private int bloodCostPerMove = 1;
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Clear")]
    [SerializeField] private string stageClearSceneName = "StageClearScene";
    [SerializeField] private float clearDelay = 0.5f;

    private readonly Dictionary<long, RunePathEdge> _edgeLookup = new();
    private readonly List<int> _visitHistory = new();
    private int _currentRuneIndex = -1;
    private int _startRuneIndex = -1;
    private int _lastRuneIndex = -1;
    private bool _lastMoveWasForward;
    private int _totalPathCount;
    private bool _isMoving;
    private bool _stageCleared;

    private void Awake()
    {
        if (resourceManager == null)
            resourceManager = StageResourceManager.Instance ?? FindFirstObjectByType<StageResourceManager>();

        if (runes == null || runes.Length == 0)
            runes = GetComponentsInChildren<RuneNode>();

        if (pathEdges == null || pathEdges.Length == 0)
            pathEdges = GetComponentsInChildren<RunePathEdge>();

        foreach (var rune in runes)
            rune.Initialize(this);

        BuildEdgeLookup();
    }

    private void Start()
    {
        InitializeStage();
    }

    private void BuildEdgeLookup()
    {
        _edgeLookup.Clear();
        _totalPathCount = pathEdges.Length;

        foreach (var edge in pathEdges)
        {
            long key = MakeEdgeKey(edge.RuneIndexA, edge.RuneIndexB);
            _edgeLookup[key] = edge;
        }
    }

    private void InitializeStage()
    {
        _startRuneIndex = -1;

        foreach (var rune in runes)
        {
            if (rune.IsStartRune)
                _startRuneIndex = rune.RuneIndex;
        }

        if (_startRuneIndex < 0 && runes.Length > 0)
            _startRuneIndex = runes[0].RuneIndex;

        foreach (var edge in pathEdges)
            edge.SetTraversed(false);

        var startRune = GetRune(_startRuneIndex);
        _currentRuneIndex = _startRuneIndex;

        _visitHistory.Clear();
        _visitHistory.Add(_startRuneIndex);
        _lastRuneIndex = -1;
        _lastMoveWasForward = false;

        if (player != null && startRune != null)
            player.position = startRune.WorldPosition;
    }

    public bool TryMoveToRune(RuneNode target)
    {
        if (CanBacktrackTo(target))
            return TryBacktrackTo(target);

        var reason = GetMoveFailReason(target);
        if (reason != MoveFailReason.None)
        {
            HandleMoveFailure(reason);
            return false;
        }

        if (!TryGetEdge(_currentRuneIndex, target.RuneIndex, out var edge))
            return false;

        // 붉은 경로 재통과: 되돌아가기와 같은 자원 처리, 경로 지움
        if (edge.IsTraversed)
            return TryRetraceTo(target, edge);

        if (resourceManager != null && !resourceManager.TrySpendRatBlood(bloodCostPerMove))
        {
            HandleMoveFailure(MoveFailReason.NoBlood);
            return false;
        }

        StartCoroutine(MoveToRuneCoroutine(target, edge));
        return true;
    }

    private bool TryRetraceTo(RuneNode target, RunePathEdge edge)
    {
        if (resourceManager != null)
        {
            resourceManager.OnUndo();
            resourceManager.RestoreRatBlood(bloodCostPerMove);
        }

        StartCoroutine(RetraceCoroutine(target, edge));
        return true;
    }

    private bool CanBacktrackTo(RuneNode target)
    {
        if (_isMoving || _stageCleared || target == null)
            return false;

        if (resourceManager != null && resourceManager.IsGameOver)
            return false;

        if (!_lastMoveWasForward || _lastRuneIndex < 0)
            return false;

        return target.RuneIndex == _lastRuneIndex;
    }

    private bool TryBacktrackTo(RuneNode target)
    {
        if (!TryGetEdge(_currentRuneIndex, target.RuneIndex, out var edge))
            return false;

        if (resourceManager != null)
        {
            resourceManager.OnUndo();
            resourceManager.RestoreRatBlood(bloodCostPerMove);
        }

        StartCoroutine(BacktrackCoroutine(target, edge));
        return true;
    }

    private MoveFailReason GetMoveFailReason(RuneNode target)
    {
        if (_stageCleared)
            return MoveFailReason.StageEnded;

        if (resourceManager != null && resourceManager.IsGameOver)
            return MoveFailReason.GameOver;

        if (_isMoving)
            return MoveFailReason.Busy;

        if (target == null)
            return MoveFailReason.NoPath;

        if (target.RuneIndex == _currentRuneIndex)
            return MoveFailReason.SameRune;

        if (target.IsForbidden)
            return MoveFailReason.ForbiddenRune;

        if (!TryGetEdge(_currentRuneIndex, target.RuneIndex, out var edge))
            return MoveFailReason.NoPath;

        // 붉은 경로 재통과는 피가 필요 없음
        if (edge.IsTraversed)
            return MoveFailReason.None;

        if (resourceManager != null && !resourceManager.HasRatBlood(bloodCostPerMove))
            return MoveFailReason.NoBlood;

        return MoveFailReason.None;
    }

    private void HandleMoveFailure(MoveFailReason reason)
    {
        if (resourceManager == null)
            return;

        switch (reason)
        {
            case MoveFailReason.NoPath:
                resourceManager.OnWrongStroke();
                break;
            case MoveFailReason.ForbiddenRune:
                resourceManager.OnForbiddenRune();
                break;
        }
    }

    private IEnumerator MoveToRuneCoroutine(RuneNode target, RunePathEdge edge)
    {
        _isMoving = true;

        int fromRuneIndex = _currentRuneIndex;
        var from = player != null ? player.position : GetRune(fromRuneIndex).WorldPosition;
        var to = target.WorldPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveEase.Evaluate(Mathf.Clamp01(elapsed / moveDuration));

            if (player != null)
                player.position = Vector3.Lerp(from, to, t);

            yield return null;
        }

        if (player != null)
            player.position = to;

        _currentRuneIndex = target.RuneIndex;
        _visitHistory.Add(target.RuneIndex);
        _lastRuneIndex = fromRuneIndex;
        _lastMoveWasForward = true;
        edge.SetTraversed(true);
        _isMoving = false;

        if (CheckStageClear())
            StartCoroutine(LoadClearSceneAfterDelay());
    }

    private IEnumerator BacktrackCoroutine(RuneNode target, RunePathEdge edge)
    {
        _isMoving = true;

        int fromRuneIndex = _currentRuneIndex;
        var from = player != null ? player.position : GetRune(fromRuneIndex).WorldPosition;
        var to = target.WorldPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveEase.Evaluate(Mathf.Clamp01(elapsed / moveDuration));

            if (player != null)
                player.position = Vector3.Lerp(from, to, t);

            yield return null;
        }

        if (player != null)
            player.position = to;

        _visitHistory.RemoveAt(_visitHistory.Count - 1);
        _currentRuneIndex = target.RuneIndex;
        _lastRuneIndex = fromRuneIndex;
        _lastMoveWasForward = false;
        edge.SetTraversed(false);
        _isMoving = false;
    }

    private IEnumerator RetraceCoroutine(RuneNode target, RunePathEdge edge)
    {
        _isMoving = true;

        int fromRuneIndex = _currentRuneIndex;
        var from = player != null ? player.position : GetRune(fromRuneIndex).WorldPosition;
        var to = target.WorldPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveEase.Evaluate(Mathf.Clamp01(elapsed / moveDuration));

            if (player != null)
                player.position = Vector3.Lerp(from, to, t);

            yield return null;
        }

        if (player != null)
            player.position = to;

        _currentRuneIndex = target.RuneIndex;
        _visitHistory.Add(target.RuneIndex);
        _lastRuneIndex = fromRuneIndex;
        _lastMoveWasForward = false;
        edge.SetTraversed(false);
        _isMoving = false;
    }

    private bool CheckStageClear()
    {
        if (_currentRuneIndex != _startRuneIndex)
            return false;

        if (_totalPathCount == 0)
            return false;

        foreach (var edge in pathEdges)
        {
            if (!edge.IsTraversed)
                return false;
        }

        return true;
    }

    private bool TryGetEdge(int runeA, int runeB, out RunePathEdge edge)
    {
        long key = MakeEdgeKey(runeA, runeB);
        return _edgeLookup.TryGetValue(key, out edge);
    }

    private static long MakeEdgeKey(int runeA, int runeB)
    {
        int min = Mathf.Min(runeA, runeB);
        int max = Mathf.Max(runeA, runeB);
        return ((long)min << 32) | (uint)max;
    }

    private IEnumerator LoadClearSceneAfterDelay()
    {
        _stageCleared = true;
        yield return new WaitForSeconds(clearDelay);
        SceneManager.LoadScene(stageClearSceneName);
    }

    private RuneNode GetRune(int index)
    {
        foreach (var rune in runes)
        {
            if (rune.RuneIndex == index)
                return rune;
        }

        return null;
    }
}
