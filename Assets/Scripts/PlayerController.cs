
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public void StopRunning()
    {
        canControl = false;
        currentSpeed = 0f;
    }

    public bool canControl = true;

    [Header("달리기 설정")]
    public float currentSpeed = 0f;
    public float acceleration = 0.5f;
    public float deceleration = 0.3f;
    public float maxSpeed = 8f;
    public float minSpeed = 0f;

    [Header("점프 설정")]
    public float jumpPower = 10f;
    private bool isGrounded = false;

    [Header("충돌 설정")]
    public float collisionSlowRate = 0.5f;
    public float flashDuration = 0.3f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // 이미 충돌한 장애물을 기록
    private HashSet<GameObject> hitObstacles =
        new HashSet<GameObject>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    void Update()
    {
        if (!canControl) return;

        // 달리기
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentSpeed += acceleration;
        }

        currentSpeed = Mathf.Clamp(
            currentSpeed, minSpeed, maxSpeed
        );

        if (!Input.GetKey(KeyCode.RightArrow))
        {
            currentSpeed -= deceleration * Time.deltaTime;
        }

        currentSpeed = Mathf.Clamp(
            currentSpeed, minSpeed, maxSpeed
        );

        // 점프
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        if (!canControl)
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );

            return;
        }

        rb.linearVelocity = new Vector2(
            currentSpeed,
            rb.linearVelocity.y
        );
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpPower
        );

        isGrounded = false;
    }

    // 바닥 착지
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // 장애물 충돌 감지
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
        {
            // 이미 충돌한 장애물이라면 무시
            if (hitObstacles.Contains(other.gameObject))
                return;

            hitObstacles.Add(other.gameObject);

            // 속도를 50%로 감소
            currentSpeed *= collisionSlowRate;

            // 속도가 최저 속도보다 낮아지지 않도록 제한
            currentSpeed = Mathf.Clamp(
                currentSpeed, minSpeed, maxSpeed
            );

            // 충돌 색상 효과
            StopCoroutine("FlashColor");
            StartCoroutine("FlashColor");
        }
    }

    // 충돌 시 색상 변경
    IEnumerator FlashColor()
    {
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.color = originalColor;
    }
}
