
using UnityEngine;

public class FlowerGirlController : MonoBehaviour
{
    private Animator animator;

    [Header("화동 이동 설정")]
    public float runSpeed = 4f;

    public bool isRunning = false;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        isRunning = false;
        animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    public void StartRunning()
    {
        isRunning = true;

        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    public void StopRunning()
    {
        if (animator != null)
        {
            animator.speed = 0f;
        }

        isRunning = false;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (!isRunning)
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );

            return;
        }

        rb.linearVelocity = new Vector2(
            runSpeed,
            rb.linearVelocity.y
        );
    }
}
