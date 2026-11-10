using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("캐릭터 연결")]
    public PlayerController player;
    public FlowerGirlController flowerGirl;

    [Header("결과 UI")]
    public TMP_Text resultText;

    private bool isGameOver = false;

    void Start()
    {
        resultText.text = "";
    }

    // 주인공이 먼저 도착한 경우
    public void PlayerWin()
    {
        if (isGameOver) return;

        isGameOver = true;

        resultText.text = "CLEAR!";
        resultText.color = Color.blue;

        StopCharacters();
    }

    // 화동이 먼저 도착한 경우
    public void FlowerGirlWin()
    {
        if (isGameOver) return;

        isGameOver = true;

        resultText.text = "GAME OVER";
        resultText.color = Color.red;

        StopCharacters();
    }

    // 캐릭터 정지
    void StopCharacters()
    {
        player.StopRunning();
        flowerGirl.StopRunning();
    }
}
