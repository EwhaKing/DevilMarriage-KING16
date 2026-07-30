using UnityEngine;

[RequireComponent(typeof(Animator))]
public class StagePlayerAnimationController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int DamagedHash =
        Animator.StringToHash("Damaged");

    private static readonly int DeathHash =
        Animator.StringToHash("Death");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetMoving(bool isMoving)
    {
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
