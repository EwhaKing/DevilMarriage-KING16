using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 선택 맵 위의 플레이어. Path를 따라 걷고, idle/move 애니메이션을 재생합니다.
/// Inspector에서 이동 속도와 크기, 오프셋을 조정하세요.
/// </summary>
public class StageSelectMapPlayer : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("클수록 더 빨리 걷습니다. 맵 로컬 픽셀/초 기준입니다.")]
    [SerializeField] private float moveSpeed = 800f;
    [Tooltip("스테이지 버튼 중심에서 캐릭터를 얼마나 위로 올릴지")]
    [SerializeField] private Vector2 standOffset = new Vector2(0f, 36f);

    [Header("Appearance")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    private RectTransform _rect;
    private Image _image;
    private SpriteRenderer _spriteRenderer;
    private StagePlayerAnimationController _animation;
    private Animator _animator;
    private Coroutine _walkRoutine;
    private StageSelectCameraPan _cameraPan;

    public bool IsMoving { get; private set; }

    public static StageSelectMapPlayer FindOrCreate(
        Transform mapParent,
        StageSelectMapPlayer existing,
        RuntimeAnimatorController controller,
        StageSelectCameraPan cameraPan)
    {
        var player = existing;
        if (player == null)
        {
            var found = GameObject.Find("StageSelectPlayer");
            if (found != null)
            {
                player = found.GetComponent<StageSelectMapPlayer>();
                if (player == null)
                    player = found.AddComponent<StageSelectMapPlayer>();
            }
        }

        if (player == null)
        {
            var go = new GameObject("StageSelectPlayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (mapParent != null)
                go.transform.SetParent(mapParent, false);
            ApplyDefaultLayout(go.GetComponent<RectTransform>());
            player = go.AddComponent<StageSelectMapPlayer>();
        }

        player.BindRuntime(mapParent, controller, cameraPan);
        return player;
    }

    public void BindRuntime(
        Transform mapParent,
        RuntimeAnimatorController fallbackController,
        StageSelectCameraPan cameraPan)
    {
        _cameraPan = cameraPan;
        if (mapParent != null && transform.parent != mapParent)
            transform.SetParent(mapParent, true);

        transform.SetAsLastSibling();
        CacheComponents();
        EnsureBody(fallbackController);
        SetMoving(false);
    }

    private static void ApplyDefaultLayout(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.15f);
        rect.sizeDelta = new Vector2(180f, 180f);
    }

    private void CacheComponents()
    {
        _rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        if (_image != null)
        {
            _image.raycastTarget = false;
            _image.preserveAspect = true;
        }
    }

    private void EnsureBody(RuntimeAnimatorController fallbackController)
    {
        var body = transform.Find("Body");
        if (body == null)
        {
            var bodyObject = new GameObject("Body", typeof(RectTransform));
            bodyObject.transform.SetParent(transform, false);
            body = bodyObject.transform;
        }

        _spriteRenderer = body.GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            _spriteRenderer = body.gameObject.AddComponent<SpriteRenderer>();
        _spriteRenderer.enabled = true;
        var spriteColor = _spriteRenderer.color;
        spriteColor.a = 0f;
        _spriteRenderer.color = spriteColor;

        _animator = body.GetComponent<Animator>();
        if (_animator == null)
            _animator = body.gameObject.AddComponent<Animator>();
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var controller = animatorController != null ? animatorController : fallbackController;
#if UNITY_EDITOR
        if (controller == null)
        {
            controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Art/Player/Player.controller");
        }
#endif
        if (controller != null)
        {
            animatorController = controller;
            if (_animator.runtimeAnimatorController == null)
                _animator.runtimeAnimatorController = controller;
        }

        _animation = body.GetComponent<StagePlayerAnimationController>();
        if (_animation == null)
            _animation = body.gameObject.AddComponent<StagePlayerAnimationController>();
    }

    private void LateUpdate()
    {
        if (_image != null && _spriteRenderer != null && _spriteRenderer.sprite != null)
            _image.sprite = _spriteRenderer.sprite;
    }

    public void PlaceOn(RectTransform button)
    {
        if (button == null)
            return;

        CacheComponents();
        if (_rect == null)
            return;

        _rect.position = button.TransformPoint(standOffset);
        SetMoving(false);
    }

    public void StopWalk()
    {
        if (_walkRoutine != null)
        {
            StopCoroutine(_walkRoutine);
            _walkRoutine = null;
        }

        IsMoving = false;
        if (_cameraPan != null)
            _cameraPan.InputLocked = false;
        SetMoving(false);
    }

    public Coroutine WalkRoute(
        IList<StageSelectPathView> hops,
        IList<int> stageRoute,
        System.Func<int, RectTransform> buttonLookup,
        System.Action onArrived)
    {
        StopWalk();
        _walkRoutine = StartCoroutine(WalkRouteRoutine(hops, stageRoute, buttonLookup, onArrived));
        return _walkRoutine;
    }

    private IEnumerator WalkRouteRoutine(
        IList<StageSelectPathView> hops,
        IList<int> stageRoute,
        System.Func<int, RectTransform> buttonLookup,
        System.Action onArrived)
    {
        IsMoving = true;
        if (_cameraPan != null)
            _cameraPan.InputLocked = true;

        FaceTowardDestination(hops, stageRoute, buttonLookup);
        SetMoving(true);

        int hopCount = hops != null ? hops.Count : 0;
        for (int i = 0; i < hopCount; i++)
        {
            var hop = hops[i];
            Vector3[] points = null;
            if (hop != null)
            {
                points = hop.GetWorldPoints();
                int fromStage = stageRoute != null && i < stageRoute.Count ? stageRoute[i] : hop.StageA;
                if (fromStage == hop.StageB)
                    System.Array.Reverse(points);
            }
            else if (buttonLookup != null && stageRoute != null && i + 1 < stageRoute.Count)
            {
                var fromButton = buttonLookup(stageRoute[i]);
                var toButton = buttonLookup(stageRoute[i + 1]);
                if (fromButton != null && toButton != null)
                    points = new[] { fromButton.position, toButton.position };
            }

            if (points == null || points.Length < 2)
                continue;

            yield return MoveAlongPoints(points, hop);
            hop?.ActivatePermanently();
        }

        IsMoving = false;
        if (_cameraPan != null)
            _cameraPan.InputLocked = false;
        SetMoving(false);
        _walkRoutine = null;
        onArrived?.Invoke();
    }

    private IEnumerator MoveAlongPoints(Vector3[] points, StageSelectPathView hop)
    {
        float speed = WorldMoveSpeed();
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 destination = OffsetWorld(points[i]);
            if (i == 0)
            {
                _rect.position = destination;
                continue;
            }

            while (Vector3.Distance(_rect.position, destination) > 0.02f)
            {
                var next = Vector3.MoveTowards(_rect.position, destination, speed * Time.deltaTime);
                _rect.position = next;
                _cameraPan?.FocusOnWorldX(_rect.position.x);
                yield return null;
            }

            _rect.position = destination;
            hop?.RevealSegment(i - 1);
        }
    }

    private float WorldMoveSpeed()
    {
        if (_rect == null || _rect.parent == null)
            return moveSpeed;

        return Mathf.Max(0.01f, _rect.parent.TransformVector(new Vector3(moveSpeed, 0f, 0f)).magnitude);
    }

    private Vector3 OffsetWorld(Vector3 point)
    {
        if (_rect == null)
            return point;

        var parent = _rect.parent as RectTransform;
        if (parent == null)
            return point + (Vector3)standOffset;

        var worldOffset = parent.TransformVector(standOffset);
        return point + worldOffset;
    }

    private void FaceTowardDestination(
        IList<StageSelectPathView> hops,
        IList<int> stageRoute,
        System.Func<int, RectTransform> buttonLookup)
    {
        if (_rect == null)
            return;

        float destinationX = _rect.position.x;
        if (stageRoute != null && stageRoute.Count > 0 && buttonLookup != null)
        {
            var destination = buttonLookup(stageRoute[stageRoute.Count - 1]);
            if (destination != null)
                destinationX = destination.position.x;
        }
        else if (hops != null && hops.Count > 0)
        {
            var points = hops[hops.Count - 1] != null ? hops[hops.Count - 1].GetWorldPoints() : null;
            if (points != null && points.Length > 0)
                destinationX = points[points.Length - 1].x;
        }

        Face(destinationX - _rect.position.x);
    }

    private void Face(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.001f || _rect == null)
            return;

        var scale = _rect.localScale;
        // 기본 스프라이트가 왼쪽을 보므로, 오른쪽 이동만 좌우 반전합니다.
        scale.x = Mathf.Abs(scale.x) * (directionX > 0f ? -1f : 1f);
        _rect.localScale = scale;
    }

    private void SetMoving(bool moving)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return;

        if (_animation != null)
            _animation.SetMoving(moving);
        else
            _animator.SetBool("IsMoving", moving);

        _animator.Play(moving ? "Player_Move" : "Player_Idle", 0, 0f);
    }
}
