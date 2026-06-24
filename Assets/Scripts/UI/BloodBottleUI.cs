using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 쥐의 피 병 UI. fillAmount로 담긴 피 양을 표시한다.
/// </summary>
public class BloodBottleUI : MonoBehaviour
{
    [SerializeField] private Image bloodFillImage;
    [SerializeField] private TextMeshProUGUI bloodCountText;

    public void SetFill(float normalizedAmount, int current, int max)
    {
        if (bloodFillImage != null)
            bloodFillImage.fillAmount = Mathf.Clamp01(normalizedAmount);

        if (bloodCountText != null)
            bloodCountText.text = $"{current}/{max}";
    }
}
