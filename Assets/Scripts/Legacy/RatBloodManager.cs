using UnityEngine;
using UnityEngine.Events;

public class RatBloodManager : MonoBehaviour
{
    public float costPerStroke = 1.0f;
    private float currentRatBlood;
    private float maxRatBlood;

    public UnityEvent<float, float> OnBloodChanged;

    public void Initialize(float stageCapacity)
    {
        maxRatBlood = stageCapacity;
        currentRatBlood = maxRatBlood;
        OnBloodChanged?.Invoke(currentRatBlood, maxRatBlood);
    }

    // 선을 그을 때 피가 충분한지 확인
    public bool CanDraw()
    {
        return currentRatBlood >= costPerStroke;
    }

    // 선을 그어서 피 소모
    public void ConsumeBlood()
    {
        currentRatBlood -= costPerStroke;
        OnBloodChanged?.Invoke(currentRatBlood, maxRatBlood);
        Debug.Log($"[쥐의 피] 남은 양: {currentRatBlood}L");
    }

    // 되돌아가서 피 회복
    public void RefundBlood()
    {
        currentRatBlood += costPerStroke;
        OnBloodChanged?.Invoke(currentRatBlood, maxRatBlood);
    }
}
