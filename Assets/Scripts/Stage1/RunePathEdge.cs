using System.Collections;
using UnityEngine;

/// <summary>
/// 두 룬을 잇는 미리 그려진 경로. 플레이어가 지나가면 붉게 빛난다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RunePathEdge : MonoBehaviour
{
    [SerializeField] private int runeIndexA;
    [SerializeField] private int runeIndexB;
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

    public int RuneIndexA => runeIndexA;
    public int RuneIndexB => runeIndexB;
    public bool IsTraversed => _traversed;

    private void Awake()
    {
        EnsureLine();
        EnsureGlowLine();
    }

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

    public void Configure(int indexA, int indexB, Vector3 positionA, Vector3 positionB)
    {
        runeIndexA = indexA;
        runeIndexB = indexB;
        EnsureLine();
        EnsureGlowLine();
        SetPositions(positionA, positionB);
        SetTraversed(false);
    }

    public bool Connects(int runeA, int runeB)
    {
        return (runeIndexA == runeA && runeIndexB == runeB)
            || (runeIndexA == runeB && runeIndexB == runeA);
    }

    public void SetPositions(Vector3 positionA, Vector3 positionB)
    {
        EnsureLine();
        EnsureGlowLine();
        _line.SetPosition(0, positionA);
        _line.SetPosition(1, positionB);
        _glowLine.SetPosition(0, positionA);
        _glowLine.SetPosition(1, positionB);
    }

    public void SetTraversed(bool traversed)
    {
        if (_traversed == traversed)
            return;

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
            _visualCoroutine = StartCoroutine(ActivateThenPulse());
            return;
        }

        _glowLine.enabled = false;
        ApplyInactiveVisual();
    }

    private void ApplyInactiveVisual()
    {
        _line.startWidth = pathWidth;
        _line.endWidth = pathWidth;
        _line.startColor = inactiveColor;
        _line.endColor = inactiveColor;
    }

    private void ApplyActiveVisual(float pulse01, float flash01)
    {
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
