
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using JetBrains.Annotations;

// 다시하기

public class GameManager : MonoBehaviour
{
    [Header("캐릭터 연결")]
    public PlayerController player;
    public FlowerGirlController flowerGirl;

    [Header("UI 연결")]
    public TMP_Text resultText;
    public TMP_Text countdownText;
    public TMP_Text speedText;
    public TMP_Text progressText;

    [Header("레이스 설정")]
    public float raceStartX = -100f;
    public float finishLineX = 100f;

    private bool isGameOver = false;
    private bool raceStarted = false;

    [Header("장애물 생성")]
    public ObstacleSpawner obstacleSpawner;

    [Header("다시하기 버튼")]
    public GameObject retryButton;
    void Start()
    {
        retryButton.SetActive(false);
        obstacleSpawner.GenerateObstacles(raceStartX,finishLineX);

        // 플레이어와 화동의 충돌 무시
        Collider2D playerCollider =
            player.GetComponent<Collider2D>();

        Collider2D flowerCollider =
            flowerGirl.GetComponent<Collider2D>();

        if (playerCollider != null && flowerCollider != null)
        {
            Physics2D.IgnoreCollision(
                playerCollider,
                flowerCollider
            );
        }
        resultText.text = "";

        player.StopRunning();
        flowerGirl.StopRunning();

        StartCoroutine(Countdown());

    }

    void Update()
    {
        if (isGameOver || !raceStarted)
            return;

        // 주인공 속도 표시
        speedText.text =
            "SPEED : " + player.currentSpeed.ToString("F1");

        // 두 캐릭터의 이동 거리 표시
        float playerDistance =
            Mathf.Max(0, player.transform.position.x - raceStartX);

        float flowerDistance =
            Mathf.Max(0, flowerGirl.transform.position.x - raceStartX);

        progressText.text =
            "PLAYER : " + playerDistance.ToString("F1") + "m\n" +
            "FLOWER GIRL : " + flowerDistance.ToString("F1") + "m";
    }

    IEnumerator Countdown()
    {
        string[] numbers = { "3", "2", "1" };

        foreach (string number in numbers)
        {
            countdownText.text = number;

            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";

        // 레이스 시작
        player.StartRunning();
        flowerGirl.StartRunning();

        raceStarted = true;

        yield return new WaitForSeconds(1f);

        countdownText.text = "";
    }

    // 레이스 종료
    void EndRace(string winner)
    {
        retryButton.SetActive(true);

        if (isGameOver)
            return;

        isGameOver = true;
        raceStarted = false;

        player.StopRunning();
        flowerGirl.StopRunning();

        resultText.text = winner;
        countdownText.text = "FINISH!";
    }

    // 플레이어 승리
    public void PlayerWin()
    {
        EndRace("CLEAR");
    }

    // 화동 승리
    public void FlowerGirlWin()
    {
        EndRace("GAME OVER");
    }
    public void RestartGame()
    {
        Debug.Log("다시하기 버튼 클릭!");

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
