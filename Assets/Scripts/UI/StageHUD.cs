using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 스테이지 HUD: 좌상단 스테이지명·정신력, 우상단 쥐의 피.
/// </summary>
public class StageHUD : MonoBehaviour
{
    [Header("Stage Info")]
    [SerializeField] private string stageCode = "1-1";
    [SerializeField] private string stageTitle = "의식의 시작";

    [Header("Left Panel")]
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI sanityLabelText;
    [SerializeField] private TextMeshProUGUI sanityValueText;
    [SerializeField] private Slider sanitySlider;

    [Header("Right Panel")]
    [SerializeField] private BloodBottleUI bloodBottleUI;

    [Header("Labels")]
    [SerializeField] private string sanityLabel = "정신력";

    private StageResourceManager _resources;

    private void Start()
    {
        _resources = StageResourceManager.Instance;
        if (_resources == null)
            _resources = FindFirstObjectByType<StageResourceManager>();

        UpdateStageName();
        UpdateSanityDisplay(_resources != null ? _resources.CurrentSanity : 0,
            _resources != null ? _resources.MaxSanity : 1);
        UpdateBloodDisplay(_resources != null ? _resources.CurrentRatBlood : 0,
            _resources != null ? _resources.MaxRatBlood : 1);

        if (_resources != null)
        {
            _resources.OnSanityChanged += UpdateSanityDisplay;
            _resources.OnRatBloodChanged += UpdateBloodDisplay;
        }
    }

    private void OnDestroy()
    {
        if (_resources != null)
        {
            _resources.OnSanityChanged -= UpdateSanityDisplay;
            _resources.OnRatBloodChanged -= UpdateBloodDisplay;
        }
    }

    private void UpdateStageName()
    {
        if (stageNameText != null)
            stageNameText.text = $"{stageCode} {stageTitle}";
    }

    private void UpdateSanityDisplay(int current, int max)
    {
        if (sanityLabelText != null)
            sanityLabelText.text = sanityLabel;

        if (sanityValueText != null)
            sanityValueText.text = $"{current}/{max}";

        if (sanitySlider != null)
        {
            sanitySlider.minValue = 0f;
            sanitySlider.maxValue = 1f;
            sanitySlider.value = max > 0 ? (float)current / max : 0f;
        }
    }

    private void UpdateBloodDisplay(int current, int max)
    {
        if (bloodBottleUI != null)
            bloodBottleUI.SetFill(max > 0 ? (float)current / max : 0f, current, max);
    }
}
