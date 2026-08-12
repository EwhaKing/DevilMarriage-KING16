using System;
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
/// 한붓그리기 퍼즐 컨트롤러.
/// Prefab 안의 Rune/Path를 읽어 이동·클리어를 처리합니다. (스테이지 공통)
/// </summary>
public class Stage1PuzzleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RuneNode[] runes;
    [SerializeField] private RunePathEdge[] pathEdges;
    [SerializeField] private Transform player;
    [SerializeField] private StageResourceManager resourceManager;
    [SerializeField] private StagePlayerAnimationController playerAnimation;

    [Header("Movement")]
    [SerializeField] private int bloodCostPerMove = 1;
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Clear")]
    [Tooltip("필수 Path가 하나도 없을 때, 모든 Path를 지나야 클리어할지")]
    [SerializeField] private bool requireAllPathsWhenNoneMarkedMandatory = true;
    [SerializeField] private string stageClearSceneName = "StageClearScene";
    [SerializeField] private float clearDelay = 0.5f;

    public bool UseGameFlowManager { get; set; }
    public bool InputLocked { get; set; }
    public bool AwaitingStartSelection { get; set; }
    public int CurrentRuneIndex => _currentRuneIndex;
    public RuneNode[] Runes => runes;
    public RunePathEdge[] PathEdges => pathEdges;

    public event Action<RuneNode> OnRuneClicked;
    public event Action OnForwardMoveCompleted;

    private readonly Dictionary<long, RunePathEdge> _edgeLookup = new();
    private readonly HashSet<int> _visitedRunes = new();
    private readonly List<int> _visitHistory = new();
    private int _currentRuneIndex = -1;
    private int _startRuneIndex = -1;
    private int _lastRuneIndex = -1;
    private bool _lastMoveWasForward;
    private int _requiredPathCount;
    private bool _isMoving;
    private bool _stageCleared;
    private bool _hasExplicitEndRune;

    private void Awake()
    {
        AutoBindMissingReferences();
        RefreshRuneAndEdgeCache();
    }

    private void Start()
    {
        InitializeStage();
    }

    /// <summary>
    /// 씬의 Player / ResourceManager를 Prefab 인스턴스에 연결합니다.
    /// </summary>
    public void BindExternalReferences(
        Transform playerTransform,
        StageResourceManager resources,
        StagePlayerAnimationController animation = null)
    {
        if (playerTransform != null)
            player = playerTransform;

        if (resources != null)
            resourceManager = resources;

        if (animation != null)
            playerAnimation = animation;
        else if (player != null && playerAnimation == null)
            playerAnimation = player.GetComponent<StagePlayerAnimationController>();
    }

    public void ApplyPlaySettings(StagePlayData playData)
    {
        if (playData == null)
            return;

        bloodCostPerMove = Mathf.Max(0, playData.bloodCostPerMove);
    }

    public void RefreshRuneAndEdgeCache()
    {
        if (runes == null || runes.Length == 0 || HasNullEntries(runes))
            runes = GetComponentsInChildren<RuneNode>(true);

        if (pathEdges == null || pathEdges.Length == 0 || HasNullEntries(pathEdges))
            pathEdges = GetComponentsInChildren<RunePathEdge>(true);

        foreach (var edge in pathEdges)
        {
            if (edge == null)
                continue;

            edge.ResolveRuneRefs(runes);
            edge.SyncIndicesFromRefs();
            edge.RefreshGeometry();
        }

        foreach (var rune in runes)
        {
            if (rune != null)
                rune.Initialize(this);
        }

        BuildEdgeLookup();
    }

    private static bool HasNullEntries<T>(T[] items) where T : UnityEngine.Object
    {
        if (items == null)
            return true;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                return true;
        }

        return false;
    }

    private void AutoBindMissingReferences()
    {
        if (resourceManager == null)
            resourceManager = StageResourceManager.Instance ?? FindAnyObjectByType<StageResourceManager>();

        if (player == null)
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerAnimation == null && player != null)
            playerAnimation = player.GetComponent<StagePlayerAnimationController>();
    }

    private void BuildEdgeLookup()
    {
        _edgeLookup.Clear();
        _requiredPathCount = 0;

        if (pathEdges == null)
            return;

        bool anyMandatoryMarked = false;
        foreach (var edge in pathEdges)
        {
            if (edge == null)
                continue;
            if (edge.IsMandatoryPath)
                anyMandatoryMarked = true;
        }

        foreach (var edge in pathEdges)
        {
            if (edge == null)
                continue;

            long key = MakeEdgeKey(edge.RuneIndexA, edge.RuneIndexB);
            _edgeLookup[key] = edge;

            bool countsAsRequired = edge.IsMandatoryPath
                || (!anyMandatoryMarked && requireAllPathsWhenNoneMarkedMandatory);
            if (countsAsRequired)
                _requiredPathCount++;
        }
    }

    private void InitializeStage()
    {
        _startRuneIndex = -1;
        _hasExplicitEndRune = false;

        foreach (var rune in runes)
        {
            if (rune == null)
                continue;

            if (rune.IsStartRune && _startRuneIndex < 0)
                _startRuneIndex = rune.RuneIndex;

            if (rune.IsEndRune)
                _hasExplicitEndRune = true;
        }

        if (_startRuneIndex < 0 && runes.Length > 0 && runes[0] != null)
            _startRuneIndex = runes[0].RuneIndex;

        foreach (var edge in pathEdges)
        {
            if (edge != null)
                edge.SetTraversed(false);
        }

        var startRune = GetRune(_startRuneIndex);
        _currentRuneIndex = _startRuneIndex;

        _visitHistory.Clear();
        _visitHistory.Add(_startRuneIndex);
        _visitedRunes.Clear();
        _visitedRunes.Add(_startRuneIndex);
        _lastRuneIndex = -1;
        _lastMoveWasForward = false;
        _isMoving = false;
        _stageCleared = false;
        InputLocked = false;

        if (player != null && startRune != null)
            player.position = startRune.WorldPosition;
        if (playerAnimation != null)
            playerAnimation.ResetToIdle();
    }

    public void RestartStage()
    {
        StopAllCoroutines();
        InitializeStage();
    }

    /// <summary>
    /// Stage4 폴백: Prefab에 위험 룬이 하나도 없을 때만 홀수 인덱스에 위험 룬을 붙입니다.
    /// </summary>
    public void ConfigureSanityHazardsForStage4()
    {
        if (runes == null)
            return;

        if (HasAnySanityHazard())
            return;

        foreach (var rune in runes)
        {
            if (rune == null)
                continue;

            bool hazard = !rune.IsStartRune && (rune.RuneIndex % 2 == 1);
            rune.SetSanityHazard(hazard);
        }
    }

    public bool HasAnySanityHazard()
    {
        if (runes == null)
            return false;

        foreach (var rune in runes)
        {
            if (rune != null && rune.IsSanityHazard)
                return true;
        }

        return false;
    }

    public void ClearSanityHazards()
    {
        if (runes == null)
            return;

        foreach (var rune in runes)
        {
            if (rune != null)
                rune.SetSanityHazard(false);
        }
    }

    public void HandleRuneClicked(RuneNode target)
    {
        OnRuneClicked?.Invoke(target);

        if (AwaitingStartSelection)
            return;

        if (InputLocked || target == null)
            return;

        TryMoveToRune(target);
    }

    public void SelectStartRune(RuneNode startRune)
    {
        if (startRune == null)
            return;

        _startRuneIndex = startRune.RuneIndex;
        _currentRuneIndex = _startRuneIndex;
        _visitHistory.Clear();
        _visitHistory.Add(_startRuneIndex);
        _visitedRunes.Clear();
        _visitedRunes.Add(_startRuneIndex);
        _lastRuneIndex = -1;
        _lastMoveWasForward = false;
        _stageCleared = false;

        foreach (var edge in pathEdges)
        {
            if (edge != null)
                edge.SetTraversed(false);
        }

        if (player != null)
            player.position = startRune.WorldPosition;
    }

    public bool TryMoveToRune(RuneNode target)
    {
        if (InputLocked || AwaitingStartSelection)
            return false;

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

        if (playerAnimation != null)
            playerAnimation.SetMoving(true);

        int fromRuneIndex = _currentRuneIndex;
        yield return AnimateAlongEdge(edge, fromRuneIndex);

        if (playerAnimation != null)
            playerAnimation.SetMoving(false);

        _currentRuneIndex = target.RuneIndex;
        _visitHistory.Add(target.RuneIndex);
        _visitedRunes.Add(target.RuneIndex);
        _lastRuneIndex = fromRuneIndex;
        _lastMoveWasForward = true;
        edge.SetTraversed(true);
        _isMoving = false;

        if (target.IsSanityHazard && resourceManager != null)
            resourceManager.OnSanityHazard();

        OnForwardMoveCompleted?.Invoke();

        if (CheckStageClear())
            StartCoroutine(LoadClearSceneAfterDelay());
    }

    private IEnumerator BacktrackCoroutine(RuneNode target, RunePathEdge edge)
    {
        _isMoving = true;

        if (playerAnimation != null)
            playerAnimation.SetMoving(true);

        int fromRuneIndex = _currentRuneIndex;
        yield return AnimateAlongEdge(edge, fromRuneIndex);

        if (player != null)
            player.position = target.WorldPosition;
        if (playerAnimation != null)
        {
            playerAnimation.SetMoving(false);
            playerAnimation.PlayDamaged();
        }

        _visitHistory.RemoveAt(_visitHistory.Count - 1);
        _currentRuneIndex = target.RuneIndex;
        RebuildVisitedFromHistory();
        _lastRuneIndex = fromRuneIndex;
        _lastMoveWasForward = false;
        edge.SetTraversed(false);
        _isMoving = false;
    }

    private IEnumerator RetraceCoroutine(RuneNode target, RunePathEdge edge)
    {
        _isMoving = true;

        if (playerAnimation != null)
            playerAnimation.SetMoving(true);

        int fromRuneIndex = _currentRuneIndex;
        yield return AnimateAlongEdge(edge, fromRuneIndex);

        if (playerAnimation != null)
        {
            playerAnimation.SetMoving(false);
            playerAnimation.PlayDamaged();
        }

        _currentRuneIndex = target.RuneIndex;
        _visitHistory.Add(target.RuneIndex);
        _visitedRunes.Add(target.RuneIndex);
        _lastRuneIndex = fromRuneIndex;
        _lastMoveWasForward = false;
        edge.SetTraversed(false);
        _isMoving = false;
    }

    private IEnumerator AnimateAlongEdge(RunePathEdge edge, int fromRuneIndex)
    {
        var points = edge != null ? edge.GetPathPointsFrom(fromRuneIndex) : null;
        if (points == null || points.Length < 2)
        {
            int otherIndex = edge != null && edge.RuneIndexA == fromRuneIndex
                ? edge.RuneIndexB
                : edge != null ? edge.RuneIndexA : fromRuneIndex;
            var other = GetRune(otherIndex);
            Vector3 from = player != null ? player.position : Vector3.zero;
            Vector3 to = other != null ? other.WorldPosition : from;
            yield return LerpPlayer(from, to);
            yield break;
        }

        // 시작점은 현재 플레이어 위치(이미 룬 위)로 맞춰 끊김을 줄임
        if (player != null)
            points[0] = player.position;

        float totalLength = 0f;
        for (int i = 1; i < points.Length; i++)
            totalLength += Vector3.Distance(points[i - 1], points[i]);

        if (totalLength < 0.0001f)
        {
            if (player != null)
                player.position = points[points.Length - 1];
            yield break;
        }

        for (int i = 1; i < points.Length; i++)
        {
            float segment = Vector3.Distance(points[i - 1], points[i]);
            float duration = moveDuration * (segment / totalLength);
            yield return LerpPlayer(points[i - 1], points[i], duration);
        }

        if (player != null)
            player.position = points[points.Length - 1];
    }

    private IEnumerator LerpPlayer(Vector3 from, Vector3 to, float duration = -1f)
    {
        if (duration < 0f)
            duration = moveDuration;

        if (duration <= 0.0001f)
        {
            if (player != null)
                player.position = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = moveEase.Evaluate(Mathf.Clamp01(elapsed / duration));
            if (player != null)
                player.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        if (player != null)
            player.position = to;
    }

    private void RebuildVisitedFromHistory()
    {
        _visitedRunes.Clear();
        foreach (var index in _visitHistory)
            _visitedRunes.Add(index);
    }

    private bool CheckStageClear()
    {
        if (_requiredPathCount == 0 && (pathEdges == null || pathEdges.Length == 0))
            return false;

        // 종료 조건: End 룬이 지정되어 있으면 그 위, 아니면 시작 룬으로 복귀
        if (_hasExplicitEndRune)
        {
            var current = GetRune(_currentRuneIndex);
            if (current == null || !current.IsEndRune)
                return false;
        }
        else if (_currentRuneIndex != _startRuneIndex)
        {
            return false;
        }

        bool anyMandatoryPath = false;
        foreach (var edge in pathEdges)
        {
            if (edge != null && edge.IsMandatoryPath)
            {
                anyMandatoryPath = true;
                break;
            }
        }

        foreach (var edge in pathEdges)
        {
            if (edge == null)
                continue;

            bool required = edge.IsMandatoryPath
                || (!anyMandatoryPath && requireAllPathsWhenNoneMarkedMandatory);
            if (required && !edge.IsTraversed)
                return false;
        }

        foreach (var rune in runes)
        {
            if (rune == null || !rune.IsMandatory)
                continue;

            if (!_visitedRunes.Contains(rune.RuneIndex))
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

        if (UseGameFlowManager && GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnStagePlayCleared();
            yield break;
        }

        if (!string.IsNullOrEmpty(stageClearSceneName))
            SceneManager.LoadScene(stageClearSceneName);
    }

    private RuneNode GetRune(int index)
    {
        foreach (var rune in runes)
        {
            if (rune != null && rune.RuneIndex == index)
                return rune;
        }

        return null;
    }
}
