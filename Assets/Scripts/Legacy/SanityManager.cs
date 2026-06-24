using UnityEngine;
using UnityEngine.Events;

public class SanityManager : MonoBehaviour
{
    public int maxSanity = 100;
    public int penaltyPerUndo = 10;
    private int currentSanity;

    public UnityEvent<int, int> OnSanityChanged;
    public UnityEvent OnGameOver;

    public void Initialize()
    {
        currentSanity = maxSanity;
        OnSanityChanged?.Invoke(currentSanity, maxSanity);
    }

    public bool IsSane()
    {
        return currentSanity > 0;
    }

    // 되돌아가기 시 정신력 소모
    public void ConsumeSanity()
    {
        currentSanity -= penaltyPerUndo;
        if (currentSanity < 0) currentSanity = 0;
        
        OnSanityChanged?.Invoke(currentSanity, maxSanity);
        Debug.Log($"[정신력] 남은 수치: {currentSanity}");

        if (currentSanity == 0)
        {
            OnGameOver?.Invoke();
        }
    }
}
