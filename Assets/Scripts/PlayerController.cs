
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
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

    [Header("게임 진행")]
    public bool canControl = false;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private HashSet<GameObject> hitObstacles =
        new HashSet<GameObject>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        canControl = false;
        currentSpeed = 0f;
    }

    public void StartRunning()
    {
        canControl = true;
    }

    public void StopRunning()
    {
        canControl = false;
        currentSpeed = 0f;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );
        }
    }

    void Update()
    {
        if (!canControl) return;

        // 오른쪽 방향키를 한 번 누를 때마다 가속
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentSpeed += acceleration;
        }

        // 방향키를 누르지 않으면 서서히 감속
        if (!Input.GetKey(KeyCode.RightArrow))
        {
            currentSpeed -= deceleration * Time.deltaTime;
        }

        currentSpeed = Mathf.Clamp(
            currentSpeed,
            minSpeed,
            maxSpeed
        );

        // 스페이스바 점프
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

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

    // 바닥에 닿았을 때
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // 바닥에 계속 닿아 있는 동안
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // 바닥에서 떨어졌을 때
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    // 장애물 충돌
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canControl) return;

        if (other.CompareTag("Obstacle"))
        {
            if (hitObstacles.Contains(other.gameObject))
                return;

            hitObstacles.Add(other.gameObject);

            // 현재 속도의 50%로 감소
            currentSpeed *= collisionSlowRate;

            currentSpeed = Mathf.Clamp(
                currentSpeed,
                minSpeed,
                maxSpeed
            );

            // 빨간색 충돌 효과
            if (spriteRenderer != null)
            {
                if (flashCoroutine != null)
                    StopCoroutine(flashCoroutine);

                flashCoroutine = StartCoroutine(FlashColor());
            }
        }
    }

    IEnumerator FlashColor()
    {
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
}
