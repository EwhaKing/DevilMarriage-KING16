using UnityEngine;

[RequireComponent(typeof(Animator))]
public class StagePlayerAnimationController : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int DamagedHash =
        Animator.StringToHash("Damaged");

    private static readonly int DeathHash =
        Animator.StringToHash("Death");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // The shared character artwork faces left by default. Flip only the visual,
    // preserving the player transform, colliders and movement coordinates.
    public void FaceTowardWorldPosition(Vector3 target)
    {
        FaceHorizontal(target.x - transform.position.x);
    }

    public void FaceTowardScreenPosition(Vector2 pointer, Camera viewCamera)
    {
        if (viewCamera == null || !viewCamera.pixelRect.Contains(pointer))
            return;
        Vector3 screenPosition = viewCamera.WorldToScreenPoint(transform.position);
        if (screenPosition.z <= 0f)
            return;
        FaceHorizontal(pointer.x - screenPosition.x);
    }

    private void FaceHorizontal(float horizontalOffset)
    {
        if (Mathf.Abs(horizontalOffset) < 0.001f)
            return;
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = horizontalOffset > 0f;
    }

    public void SetMoving(bool isMoving)
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();
        if (_animator != null)
            _animator.SetBool(IsMovingHash, isMoving);
    }

    public void PlayDamaged()
    {
        _animator.SetTrigger(DamagedHash);
    }

    public void PlayDeath()
    {
        _animator.SetBool(IsMovingHash, false);
        _animator.SetTrigger(DeathHash);
    }

    public void ResetToIdle()
    {
        _animator.ResetTrigger(DamagedHash);
        _animator.ResetTrigger(DeathHash);
        _animator.SetBool(IsMovingHash, false);
        _animator.Play("Idle", 0, 0f);
    }
}
