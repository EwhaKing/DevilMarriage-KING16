
using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("장애물 설정")]
    public GameObject obstaclePrefab;
    public int minObstacleCount = 15;
    public int maxObstacleCount = 20;

    [Header("장애물 간격")]
    public float minSpacing = 7f;
    public float maxSpacing = 10f;
    public float startSafeDistance = 5f;
    public float finishSafeDistance = 10f;

    [Header("점프 간격 설정")]
    public PlayerController player;
    public float landingBuffer = 1f;
    public float extraObstacleSpacing = 0f;

    [Header("바닥 연결")]
    public Collider2D groundCollider;

    private List<GameObject> spawnedObstacles =
    new List<GameObject>();



    public void GenerateObstacles(float startX, float finishX)
    {
        ClearObstacles();

        if (obstaclePrefab == null || groundCollider == null)
        {
            Debug.LogError("ObstacleSpawner 연결을 확인해 줘!");
            return;
        }

        // 장애물 생성 가능 구간
        float minX = startX + startSafeDistance;
        float maxX = finishX - finishSafeDistance;

        float availableDistance = maxX - minX;

        Debug.Log("전달된 시작 좌표: " + startX);
        Debug.Log("전달된 결승 좌표: " + finishX);
        Debug.Log("최소 생성 좌표: " + minX);
        Debug.Log("최대 생성 좌표: " + maxX);
        Debug.Log("실제 생성 가능 거리: " + availableDistance);

        Debug.Log("시작 좌표: " + startX);
        Debug.Log("결승 좌표: " + finishX);
        Debug.Log("장애물 생성 구간: " + availableDistance);

        if (availableDistance <= 0)
        {
            Debug.LogError("장애물 생성 구간이 잘못되었어!");
            return;
        }

        // 생성할 장애물 개수
        int obstacleCount = 15;

        // 장애물 사이 기본 간격
        float spacing = availableDistance / (obstacleCount + 1);

        // 랜덤 위치를 만들되 간격이 너무 좁아지지 않게 제한
        float randomRange = spacing * 0.2f;

        List<float> positions = new List<float>();

        for (int i = 0; i < obstacleCount; i++)
        {
            float baseX = minX + spacing * (i + 1);

            float randomOffset = Random.Range(
                -randomRange,
                randomRange
            );

            float randomX = baseX + randomOffset;

            positions.Add(randomX);
        }

        // 장애물 생성
        foreach (float x in positions)
        {
            GameObject obstacle = Instantiate(
                obstaclePrefab,
                Vector3.zero,
                Quaternion.identity
            );

            Collider2D obstacleCollider =
                obstacle.GetComponentInChildren<Collider2D>();

            float obstacleHeight = 0f;

            if (obstacleCollider != null)
            {
                obstacleHeight = obstacleCollider.bounds.extents.y;
            }

            Vector3 spawnPosition = new Vector3(
                x,
                groundCollider.bounds.max.y + obstacleHeight,
                0f
            );

            obstacle.transform.position = spawnPosition;

            spawnedObstacles.Add(obstacle);
        }

        Debug.Log(
            "장애물 생성 완료: " + spawnedObstacles.Count +
            "개 / 기본 간격: " + spacing.ToString("F1") + "m"
        );
    }

    void ClearObstacles()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
                Destroy(obstacle);
        }

        spawnedObstacles.Clear();
    }
}
