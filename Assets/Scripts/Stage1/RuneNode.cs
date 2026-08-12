using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RuneNode : MonoBehaviour
{
    [SerializeField] private int runeIndex;
    [SerializeField] private bool isStartRune;
    [SerializeField] private bool isMandatory = true;
    [SerializeField] private bool isForbidden;
    [SerializeField] private bool isSanityHazard;

    private Stage1PuzzleController _controller;
    private SpriteRenderer _spriteRenderer;
    private Color _defaultColor = Color.white;
    private Vector3 _defaultScale = Vector3.one;
    private bool _highlighted;
    private float _pulseTime;

    public int RuneIndex => runeIndex;
    public bool IsStartRune => isStartRune;
    public bool IsMandatory => isMandatory;
    public bool IsForbidden => isForbidden;
    public bool IsSanityHazard => isSanityHazard;
    public Vector3 WorldPosition => transform.position;

    public void Initialize(Stage1PuzzleController controller)
    {
        _controller = controller;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _defaultColor = _spriteRenderer.color;
        _defaultScale = transform.localScale;
        ApplyHazardVisual();
    }

    public void Configure(int index, bool start, bool mandatory, bool forbidden)
    {
        runeIndex = index;
        isStartRune = start;
        isMandatory = mandatory;
        isForbidden = forbidden;
    }

    public void SetSanityHazard(bool enabled)
    {
        isSanityHazard = enabled;
        ApplyHazardVisual();
    }

    private void ApplyHazardVisual()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            return;

        _spriteRenderer.color = isSanityHazard
            ? new Color(0.75f, 0.35f, 0.95f, 1f)
            : _defaultColor;
    }

    public void SetHighlight(bool enabled)
    {
        _highlighted = enabled;
        if (!enabled)
        {
            ApplyHazardVisual();
            transform.localScale = _defaultScale;
        }
    }

    private void Update()
    {
        if (!_highlighted)
            return;

        _pulseTime += Time.deltaTime * 3f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseTime);
        transform.localScale = _defaultScale * (1f + 0.12f * pulse);

        if (_spriteRenderer != null)
            _spriteRenderer.color = Color.Lerp(_defaultColor, Color.yellow, 0.35f + 0.35f * pulse);
    }

    private void OnMouseDown()
    {
        if (_controller == null)
            return;

        _controller.HandleRuneClicked(this);
    }
}
