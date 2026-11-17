
using UnityEngine;

public class FlowerGirlController : MonoBehaviour
{
    [Header("화동 이동 설정")]
    public float runSpeed = 4f;

    public bool isRunning = false;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        isRunning = false;
    }

    public void StartRunning()
    {
        isRunning = true;
    }

    public void StopRunning()
    {
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
