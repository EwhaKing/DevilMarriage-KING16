using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 두 룬을 잇는 미리 그려진 경로. 플레이어가 지나가면 붉게 빛난다.
/// Inspector에서 Rune A/B를 지정하거나, StagePuzzleLayout으로 자동 생성하세요.
/// 중간 Waypoints를 넣으면 꺾이거나 휘어진 Path를 만들 수 있습니다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[ExecuteAlways]
public class RunePathEdge : MonoBehaviour
{
    [Header("Connection")]
    [Tooltip("연결 시작 룬 (드래그하여 지정)")]
    [SerializeField] private RuneNode runeA;

    [Tooltip("연결 끝 룬 (드래그하여 지정)")]
    [SerializeField] private RuneNode runeB;

    [Tooltip("룬 참조가 없을 때 사용하는 인덱스 (기존 씬 호환)")]
    [SerializeField] private int runeIndexA;

    [Tooltip("룬 참조가 없을 때 사용하는 인덱스 (기존 씬 호환)")]
    [SerializeField] private int runeIndexB;

    [Tooltip("직선이 아닌 Path를 만들 때 사용할 중간 지점들 (순서대로)")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("클리어를 위해 반드시 지나야 하는 Path인지")]
    [SerializeField] private bool isMandatoryPath = true;

    [Header("Visual")]
    [SerializeField] private Color inactiveColor = new Color(0.45f, 0.42f, 0.5f, 0.55f);
    [SerializeField] private Color activeColor = new Color(1f, 0.18f, 0.12f, 1f);
    [SerializeField] private Color glowColor = new Color(1f, 0.35f, 0.2f, 0.55f);
    [SerializeField] private float pathWidth = 0.1f;
    [SerializeField] private float activeWidthMultiplier = 1.2f;
    [SerializeField] private float glowWidthMultiplier = 4f;
    [SerializeField] private float pulseSpeed = 5f;
    [SerializeField] private float pulseIntensity = 0.45f;
    [SerializeField] private float activateFlashDuration = 0.3f;
    [SerializeField] private int sortingOrder = 1;

    private static Material _sharedCoreMaterial;
    private static Material _sharedGlowMaterial;

    private LineRenderer _line;
    private LineRenderer _glowLine;
    private Coroutine _visualCoroutine;
    private bool _traversed;

    public int RuneIndexA => runeA != null ? runeA.RuneIndex : runeIndexA;
    public int RuneIndexB => runeB != null ? runeB.RuneIndex : runeIndexB;
    public RuneNode RuneA => runeA;
    public RuneNode RuneB => runeB;
    public bool IsMandatoryPath => isMandatoryPath;
    public bool IsTraversed => _traversed;

    private void Awake()
    {
        EnsureLine();
        EnsureGlowLine();
        SyncIndicesFromRefs();
        if (!Application.isPlaying)
            RefreshGeometry();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying)
            return;

        // 에디터에서 룬을 옮기면 Path가 따라오도록 갱신
        RefreshGeometry();
    }

    private void OnValidate()
    {
        SyncIndicesFromRefs();
        if (!Application.isPlaying)
            RefreshGeometry();
    }
#endif

    private void OnDestroy()
    {
        if (_visualCoroutine != null)
            StopCoroutine(_visualCoroutine);
    }

    private void EnsureLine()
    {
        if (_line != null)
            return;

        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.startWidth = pathWidth;
        _line.endWidth = pathWidth;
        _line.sortingOrder = sortingOrder;
        _line.textureMode = LineTextureMode.Stretch;
        _line.numCapVertices = 4;
        _line.numCornerVertices = 4;
        _line.material = GetCoreMaterial();
    }

    private void EnsureGlowLine()
    {
        if (_glowLine != null)
            return;

        var glowObject = transform.Find("Glow");
        if (glowObject == null)
        {
            glowObject = new GameObject("Glow").transform;
            glowObject.SetParent(transform, false);
        }

        _glowLine = glowObject.GetComponent<LineRenderer>();
        if (_glowLine == null)
            _glowLine = glowObject.gameObject.AddComponent<LineRenderer>();

        _glowLine.useWorldSpace = true;
        _glowLine.positionCount = 2;
        _glowLine.startWidth = pathWidth * glowWidthMultiplier;
        _glowLine.endWidth = pathWidth * glowWidthMultiplier;
        _glowLine.sortingOrder = sortingOrder - 1;
        _glowLine.textureMode = LineTextureMode.Stretch;
        _glowLine.numCapVertices = 6;
        _glowLine.numCornerVertices = 6;
        _glowLine.material = GetGlowMaterial();
        _glowLine.enabled = false;
    }

    private static Material GetCoreMaterial()
    {
        if (_sharedCoreMaterial != null)
            return _sharedCoreMaterial;

        var shader = Shader.Find("Sprites/Default");
        _sharedCoreMaterial = new Material(shader);
        _sharedCoreMaterial.color = Color.white;
        return _sharedCoreMaterial;
    }

    private static Material GetGlowMaterial()
    {
        if (_sharedGlowMaterial != null)
            return _sharedGlowMaterial;

        var shader = Shader.Find("DevilMarriage/LineGlow");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        _sharedGlowMaterial = new Material(shader);
        _sharedGlowMaterial.color = Color.white;
        return _sharedGlowMaterial;
    }

    public void SetRuneReferences(RuneNode a, RuneNode b)
    {
        runeA = a;
        runeB = b;
        SyncIndicesFromRefs();
        RefreshGeometry();
    }

    public void ResolveRuneRefs(RuneNode[] runes)
    {
        if (runes == null)
            return;

        if (runeA == null || runeB == null)
        {
            foreach (var rune in runes)
            {
                if (rune == null)
                    continue;
                if (runeA == null && rune.RuneIndex == runeIndexA)
                    runeA = rune;
                if (runeB == null && rune.RuneIndex == runeIndexB)
                    runeB = rune;
            }
        }

        SyncIndicesFromRefs();
    }

    public void SetWaypoints(Transform[] points)
    {
        waypoints = points;
        RefreshGeometry();
    }

    public void SetMandatory(bool mandatory)
    {
        isMandatoryPath = mandatory;
    }

    public void Configure(int indexA, int indexB, Vector3 positionA, Vector3 positionB)
    {
        runeIndexA = indexA;
        runeIndexB = indexB;
        EnsureLine();
        EnsureGlowLine();
        SetPositions(positionA, positionB);
        SetTraversed(false);
    }

    public bool Connects(int runeAIndex, int runeBIndex)
    {
        int a = RuneIndexA;
        int b = RuneIndexB;
        return (a == runeAIndex && b == runeBIndex) || (a == runeBIndex && b == runeAIndex);
    }

    public void SyncIndicesFromRefs()
    {
        if (runeA != null)
            runeIndexA = runeA.RuneIndex;
        if (runeB != null)
            runeIndexB = runeB.RuneIndex;
    }

    /// <summary>
    /// 룬 위치 + Waypoints 기준으로 LineRenderer를 갱신합니다.
    /// </summary>
    public void RefreshGeometry()
    {
        var points = GetWorldPoints();
        if (points == null || points.Length < 2)
            return;

        EnsureLine();
        EnsureGlowLine();
        ApplyPointArray(points);
        gameObject.name = $"Path_{RuneIndexA}_{RuneIndexB}";
    }

    public void SetPositions(Vector3 positionA, Vector3 positionB)
    {
        EnsureLine();
        EnsureGlowLine();
        ApplyPointArray(new[] { positionA, positionB });
    }

    /// <summary>
    /// 이동 보간에 사용할 월드 좌표 경로 (시작 룬 → waypoints → 끝 룬).
    /// fromIndex가 B이면 경로를 뒤집습니다.
    /// </summary>
    public Vector3[] GetPathPointsFrom(int fromRuneIndex)
    {
        var points = GetWorldPoints();
        if (points == null || points.Length < 2)
            return points;

        if (fromRuneIndex == RuneIndexB)
        {
            System.Array.Reverse(points);
        }

        return points;
    }

    public Vector3[] GetWorldPoints()
    {
        Vector3? start = null;
        Vector3? end = null;

        if (runeA != null)
            start = runeA.WorldPosition;
        if (runeB != null)
            end = runeB.WorldPosition;

        if (start == null || end == null)
        {
            // 인덱스만 있는 기존 Path: LineRenderer 현재 값 또는 직선 2점
            EnsureLine();
            if (_line != null && _line.positionCount >= 2)
            {
                start ??= _line.GetPosition(0);
                end ??= _line.GetPosition(_line.positionCount - 1);
            }
        }

        if (start == null || end == null)
            return null;

        var list = new List<Vector3> { start.Value };
        if (waypoints != null)
        {
            foreach (var wp in waypoints)
            {
                if (wp != null)
                    list.Add(wp.position);
            }
        }

        list.Add(end.Value);
        return list.ToArray();
    }

    private void ApplyPointArray(Vector3[] points)
    {
        _line.positionCount = points.Length;
        _line.SetPositions(points);
        _glowLine.positionCount = points.Length;
        _glowLine.SetPositions(points);
    }

    public void SetTraversed(bool traversed)
    {
        if (_traversed == traversed)
        {
            if (!traversed)
                ApplyInactiveVisual();
            return;
        }

        _traversed = traversed;
        EnsureLine();
        EnsureGlowLine();

        if (_visualCoroutine != null)
        {
            StopCoroutine(_visualCoroutine);
            _visualCoroutine = null;
        }

        if (traversed)
        {
            _glowLine.enabled = true;
            if (Application.isPlaying && isActiveAndEnabled)
                _visualCoroutine = StartCoroutine(ActivateThenPulse());
            else
                ApplyActiveVisual(0.5f, 0f);
            return;
        }

        _glowLine.enabled = false;
        ApplyInactiveVisual();
    }

    private void ApplyInactiveVisual()
    {
        if (_line == null)
            return;

        _line.startWidth = pathWidth;
        _line.endWidth = pathWidth;
        _line.startColor = inactiveColor;
        _line.endColor = inactiveColor;
    }

    private void ApplyActiveVisual(float pulse01, float flash01)
    {
        if (_line == null || _glowLine == null)
            return;

        float width = pathWidth * Mathf.Lerp(activeWidthMultiplier, activeWidthMultiplier * 1.15f, pulse01);
        _line.startWidth = width;
        _line.endWidth = width;

        var core = Color.Lerp(activeColor, Color.white, flash01 * 0.65f + pulse01 * 0.15f);
        _line.startColor = core;
        _line.endColor = core;

        float glowWidth = pathWidth * glowWidthMultiplier * (1f + pulse01 * 0.25f + flash01 * 0.35f);
        _glowLine.startWidth = glowWidth;
        _glowLine.endWidth = glowWidth;

        var glow = glowColor;
        glow.a *= 0.75f + pulse01 * pulseIntensity + flash01 * 0.8f;
        _glowLine.startColor = glow;
        _glowLine.endColor = glow;
    }

    private IEnumerator ActivateThenPulse()
    {
        float elapsed = 0f;
        while (elapsed < activateFlashDuration)
        {
            elapsed += Time.deltaTime;
            float flash01 = 1f - Mathf.Clamp01(elapsed / activateFlashDuration);
            float pulse01 = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            ApplyActiveVisual(pulse01, flash01);
            yield return null;
        }

        while (_traversed)
        {
            float pulse01 = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            ApplyActiveVisual(pulse01, 0f);
            yield return null;
        }
    }
}
