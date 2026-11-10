
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    public Transform target;

    [Header("카메라 설정")]
    public float smoothSpeed = 5f;
    public float offsetX = 2f;
    public float offsetY = 0f;

    void LateUpdate()
    {
        if (target == null)
            return;

        // 카메라가 이동할 위치
        Vector3 targetPosition = new Vector3(
            target.position.x + offsetX,
            target.position.y + offsetY,
            -10f
        );

        // 부드럽게 따라가기
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
