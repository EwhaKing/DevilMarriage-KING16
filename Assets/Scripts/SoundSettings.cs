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
        // 1. 게임이 켜지면 기존에 저장된 볼륨 값을 불러옵니다. (없으면 기본값 0.75f)
        float savedBGM = PlayerPrefs.GetFloat("BGM_Volume", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Volume", 0.75f);

        // 2. 슬라이더의 위치를 저장되었던 값으로 세팅합니다.
        if (bgmSlider != null) bgmSlider.value = savedBGM;
        if (sfxSlider != null) sfxSlider.value = savedSFX;

        // 3. 실제 오디오 믹서에 볼륨을 적용합니다.
        SetBGMVolume(savedBGM);
        SetSFXVolume(savedSFX);
    }

    // 슬라이더 값이 바뀔 때 실시간으로 호출할 함수들
    public void SetBGMVolume(float volume)
    {
        // 오디오 믹서는 데시벨(dB)을 쓰므로 로그 계산이 들어갑니다. (0일 때 -80dB 무음 처리)
        float db = volume <= 0 ? -80f : Mathf.Log10(volume) * 20f;
        audioMixer.SetFloat("BGM_Vol", db);
    }

    public void SetSFXVolume(float volume)
    {
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