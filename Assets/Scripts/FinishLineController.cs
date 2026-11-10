
using UnityEngine;

public class FinishLineController : MonoBehaviour
{
    public GameManager gameManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 주인공이 먼저 도착
        if (other.CompareTag("Player"))
        {
            gameManager.PlayerWin();
        }

        // 화동이 먼저 도착
        else if (other.CompareTag("FlowerGirl"))
        {
            gameManager.FlowerGirlWin();
        }
    }
}
