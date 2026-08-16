using UnityEngine;

/// <summary>
/// 한붓그리기 퍼즐의 룬(노드)입니다.
/// Inspector에서 인덱스·시작/종료·특수 속성을 설정하세요.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RuneNode : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("룬 고유 번호. Path 연결·이동 판정에 사용됩니다. 같은 퍼즐 안에서 겹치지 않게 하세요.")]
    [SerializeField] private int runeIndex;

    [Header("Roles")]
    [Tooltip("시작 룬 표시용. 실제 시작점은 플레이 시작 전 플레이어가 직접 고릅니다.")]
    [SerializeField] private bool isStartRune;

    [Tooltip("종료 룬. 하나라도 켜져 있으면 클리어 시 이 룬 위에 있어야 합니다. 모두 끄면 시작 룬으로 돌아와야 클리어됩니다.")]
    [SerializeField] private bool isEndRune;

    [Tooltip("반드시 방문해야 하는 룬. 끄면 방문하지 않아도 클리어 가능합니다.")]
    [SerializeField] private bool isMandatory = true;

    [Header("Special")]
    [Tooltip("밟으면 이동이 거부되고 정신력 패널티가 적용됩니다.")]
    [SerializeField] private bool isForbidden;

    [Tooltip("밟으면(전진 이동 시) 정신력이 감소합니다.")]
    [SerializeField] private bool isSanityHazard;

    private Stage1PuzzleController _controller;
    private SpriteRenderer _spriteRenderer;
    private Color _defaultColor = Color.white;
    private Vector3 _defaultScale = Vector3.one;
    private bool _highlighted;
    private float _pulseTime;

    public int RuneIndex => runeIndex;
    public bool IsStartRune => isStartRune;
    public bool IsEndRune => isEndRune;
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

    public void SetEndRune(bool enabled)
    {
        isEndRune = enabled;
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isStartRune ? Color.green : isEndRune ? Color.cyan : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
    }
#endif
}
