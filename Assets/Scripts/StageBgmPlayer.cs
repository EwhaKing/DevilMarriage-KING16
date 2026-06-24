using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 스테이지 씬에서 BGM을 재생한다. TitleScene과 동일한 BGM 믹서 그룹을 사용한다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class StageBgmPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.playOnAwake = true;

        if (bgmClip != null)
            _audioSource.clip = bgmClip;

        if (bgmMixerGroup != null)
            _audioSource.outputAudioMixerGroup = bgmMixerGroup;
    }
}
