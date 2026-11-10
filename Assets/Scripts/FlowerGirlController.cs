
using UnityEngine;

public class FlowerGirlController : MonoBehaviour
{
    [Header("화동 이동 설정")]
    public float runSpeed = 4f;

    // 게임 진행 여부
    public bool isRunning = true;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // 게임이 끝났다면 이동 중지
        if (!isRunning)
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );

            return;
        }

        // 일정한 속도로 오른쪽 이동
        rb.linearVelocity = new Vector2(
            runSpeed,
            rb.linearVelocity.y
        );
    }

    // 화동 이동 정지
    public void StopRunning()
    {
        isRunning = false;

        rb.linearVelocity = new Vector2(
            0f,
            rb.linearVelocity.y
        );
    }
}
