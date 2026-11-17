
using UnityEngine;

public class FinishLineController : MonoBehaviour
{
    public GameManager gameManager;

    private bool finished = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (finished) return;

        if (gameManager == null)
        {
            Debug.LogError("GameManager가 연결되지 않았어!");
            return;
        }

        // 플레이어가 먼저 도착
        if (other.CompareTag("Player"))
        {
            finished = true;
            gameManager.PlayerWin();
        }

        // 화동이 먼저 도착
        else if (other.CompareTag("FlowerGirl"))
        {
            finished = true;
            gameManager.FlowerGirlWin();
        }
    }
}
