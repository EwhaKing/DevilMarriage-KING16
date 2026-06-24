using UnityEngine;
using UnityEngine.UI;

public class ResourceUIController : MonoBehaviour
{
    [Header("UI 이미지 연결")]
    [Tooltip("쥐의 피를 표시할 UI Image (Image Type이 반드시 'Filled'여야 합니다)")]
    public Image bloodGauge;
    
    [Tooltip("정신력을 표시할 UI Image")]
    public Image sanityGauge;

    public void UpdateBloodGauge(float currentBlood, float maxBlood)
    {
        if (bloodGauge != null && maxBlood > 0)
            bloodGauge.fillAmount = currentBlood / maxBlood;
    }

    public void UpdateSanityGauge(int currentSanity, int maxSanity)
    {
        if (sanityGauge != null && maxSanity > 0)
            sanityGauge.fillAmount = (float)currentSanity / maxSanity;
    }
}
