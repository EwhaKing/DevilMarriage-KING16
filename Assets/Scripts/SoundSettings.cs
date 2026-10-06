using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SoundSettings : MonoBehaviour
{
    [Header("오디오 믹서 연결")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("UI 슬라이더 연결")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        ApplySavedVolumes();
    }

    /// <summary>
    /// 저장된 BGM/효과음 볼륨을 슬라이더와 믹서에 적용합니다.
    /// 설정 팝업이 비활성으로 시작할 때도 씬 진입 직후 호출할 수 있습니다.
    /// </summary>
    public void ApplySavedVolumes()
    {
        float savedBGM = PlayerPrefs.GetFloat("BGM_Volume", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Volume", 0.75f);

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(savedBGM);
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(savedSFX);

        SetBGMVolume(savedBGM);
        SetSFXVolume(savedSFX);
    }

    // 슬라이더 값이 바뀔 때 실시간으로 호출할 함수들
    public void SetBGMVolume(float volume)
    {
        // 오디오 믹서는 데시벨(dB)을 쓰므로 로그 계산이 들어갑니다. (0일 때 -80dB 무음 처리)
        if (audioMixer == null)
            return;

        float db = volume <= 0 ? -80f : Mathf.Log10(volume) * 20f;
        audioMixer.SetFloat("BGM_Vol", db);
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer == null)
            return;

        float db = volume <= 0 ? -80f : Mathf.Log10(volume) * 20f;
        audioMixer.SetFloat("SFX_Vol", db);
    }

    /// <summary>
    /// X자 버튼(닫기)을 누를 때 호출할 함수! 현재 설정을 저장합니다.
    /// </summary>
    public void SaveSoundSettings()
    {
        if (bgmSlider != null && sfxSlider != null)
        {
            // PlayerPrefs를 이용해 로컬 기기에 볼륨 값을 안전하게 저장합니다.
            PlayerPrefs.SetFloat("BGM_Volume", bgmSlider.value);
            PlayerPrefs.SetFloat("SFX_Volume", sfxSlider.value);
            PlayerPrefs.Save();

            Debug.Log("소리 설정 저장 완료! BGM: " + bgmSlider.value + ", SFX: " + sfxSlider.value);
        }
    }
}