using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StageSelect 버튼 두 개를 잇는 Path.
/// 기본은 어두운 색이며, 플레이어가 한 번 지나가면 붉은 빛으로 영구 활성화됩니다.
/// </summary>
public class StageSelectPathView : MonoBehaviour
{
    [SerializeField] private RectTransform from;
    [SerializeField] private RectTransform to;
    [SerializeField] private RectTransform[] waypoints;
    [SerializeField] private float pathWidth = 12f;
    [SerializeField] private Color inactiveColor = new Color(0.18f, 0.16f, 0.2f, 0.9f);
    [SerializeField] private Color activeColor = new Color(1f, 0.18f, 0.12f, 1f);
    [SerializeField] private Color glowColor = new Color(1f, 0.35f, 0.2f, 0.45f);

    private bool _activated;
    private int _revealedSegments;

    public RectTransform From
    {
        get => from;
        set => from = value;
    }

    public RectTransform To
    {
        get => to;
        set => to = value;
    }

    public RectTransform[] Waypoints
    {
        get => waypoints;
        set => waypoints = value;
    }

    public float PathWidth
    {
        get => pathWidth;
        set => pathWidth = value;
    }

    public Color InactiveColor
    {
        get => inactiveColor;
        set => inactiveColor = value;
    }

    public Color ActiveColor
    {
        get => activeColor;
        set => activeColor = value;
    }

    public Color GlowColor
    {
        get => glowColor;
        set => glowColor = value;
    }

    public bool IsActivated => _activated;
    public int StageA => StageSelectPathLayout.ParseStageNumber(from);
    public int StageB => StageSelectPathLayout.ParseStageNumber(to);

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        RefreshGeometry();
    }

    public void ApplySavedActivation()
    {
        _activated = StageProgressManager.IsPathActivated(StageA, StageB);
        if (_activated)
            _revealedSegments = int.MaxValue;
        RefreshGeometry();
    }

    public void RevealSegment(int segmentIndex)
    {
        if (_activated)
            return;

        int revealed = Mathf.Max(_revealedSegments, segmentIndex + 1);
        if (revealed == _revealedSegments)
            return;

        _revealedSegments = revealed;
        RefreshGeometry();
    }

    public void ActivatePermanently()
    {
        StageProgressManager.ActivatePath(StageA, StageB);
        _activated = true;
        RefreshGeometry();
    }

    public Vector3[] GetWorldPoints()
    {
        return BuildPoints();
    }

    public void RefreshGeometry()
    {
        if (this == null)
            return;

        var points = BuildPoints();
        if (points == null || points.Length < 2)
            return;

        int segmentCount = points.Length - 1;
        if (!EnsureSegmentCount(segmentCount))
            return;

        for (int i = 0; i < segmentCount; i++)
            PlaceSegment(i, points[i], points[i + 1]);
    }

    private Vector3[] BuildPoints()
    {
        if (from == null || to == null)
            return null;

        int extra = waypoints != null ? waypoints.Length : 0;
        var points = new Vector3[2 + extra];
        int index = 0;
        points[index++] = from.position;
        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                    points[index++] = waypoints[i].position;
            }
        }

        points[index] = to.position;
        if (index == points.Length - 1)
            return points;

        var trimmed = new Vector3[index + 1];
        for (int i = 0; i <= index; i++)
            trimmed[i] = points[i];
        return trimmed;
    }

    private bool EnsureSegmentCount(int count)
    {
        if (transform == null)
            return false;

        while (transform.childCount > count)
        {
            var extra = transform.GetChild(transform.childCount - 1);
            if (extra == null)
                break;

            if (Application.isPlaying)
                Destroy(extra.gameObject);
            else
                extra.gameObject.SetActive(false);
            break;
        }

        while (transform.childCount < count)
        {
            var segment = new GameObject($"Segment_{transform.childCount}", typeof(RectTransform));
            segment.transform.SetParent(transform, false);

            EnsureSegmentVisuals(segment.transform);
        }

        return transform.childCount >= count;
    }

    private static void EnsureSegmentVisuals(Transform segment)
    {
        if (segment == null)
            return;

        var rootImage = segment.GetComponent<Image>();
        if (rootImage != null)
            rootImage.enabled = false;

        if (segment.Find("Glow") == null)
            CreateLineImage(segment, "Glow");
        if (segment.Find("Core") == null)
            CreateLineImage(segment, "Core");
    }

    private static Image CreateLineImage(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        return image;
    }

    private void PlaceSegment(int index, Vector3 worldA, Vector3 worldB)
    {
        if (index < 0 || index >= transform.childCount)
            return;

        var segment = transform.GetChild(index) as RectTransform;
        if (segment == null)
            return;

        var parent = transform as RectTransform;
        Vector3 localA = parent != null ? parent.InverseTransformPoint(worldA) : worldA;
        Vector3 localB = parent != null ? parent.InverseTransformPoint(worldB) : worldB;
        Vector3 delta = localB - localA;
        float distance = Mathf.Max(0.01f, delta.magnitude);
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        segment.anchorMin = new Vector2(0.5f, 0.5f);
        segment.anchorMax = new Vector2(0.5f, 0.5f);
        segment.pivot = new Vector2(0.5f, 0.5f);
        segment.anchoredPosition = (localA + localB) * 0.5f;
        segment.sizeDelta = Vector2.zero;
        segment.localRotation = Quaternion.identity;
        segment.localScale = Vector3.one;

        EnsureSegmentVisuals(segment);
        var glow = segment.Find("Glow") as RectTransform;
        var core = segment.Find("Core") as RectTransform;
        bool lit = _activated || index < _revealedSegments;
        LayoutLine(glow, distance, pathWidth * (lit ? 2.4f : 1.4f), angle, lit ? glowColor : Color.clear);
        LayoutLine(core, distance, pathWidth * (lit ? 1.2f : 1f), angle, lit ? activeColor : inactiveColor);
    }

    private static void LayoutLine(RectTransform line, float distance, float width, float angle, Color color)
    {
        if (line == null)
            return;

        line.anchorMin = new Vector2(0.5f, 0.5f);
        line.anchorMax = new Vector2(0.5f, 0.5f);
        line.pivot = new Vector2(0.5f, 0.5f);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = new Vector2(distance, width);
        line.localRotation = Quaternion.Euler(0f, 0f, angle);
        line.localScale = Vector3.one;

        var image = line.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
            image.raycastTarget = false;
        }
    }
}
