using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 스테이지/프롤로그 씬에서 BGM을 재생한다. TitleScene과 동일한 BGM 믹서 그룹을 사용한다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class StageBgmPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    private AudioSource _audioSource;
    private Coroutine _fadeCoroutine;

    public AudioSource Source => _audioSource;
    public float Volume
    {
        get => _audioSource != null ? _audioSource.volume : 0f;
        set
        {
            if (_audioSource != null)
                _audioSource.volume = Mathf.Clamp01(value);
        }
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;

        if (bgmMixerGroup != null)
            _audioSource.outputAudioMixerGroup = bgmMixerGroup;

        if (bgmClip != null)
        {
            _audioSource.clip = bgmClip;
            _audioSource.Play();
        }
    }

    public void SetBgmClip(AudioClip clip)
    {
        Play(clip, Volume <= 0f ? 1f : Volume);
    }

    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _audioSource == null)
            return;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        bgmClip = clip;
        _audioSource.clip = clip;
        _audioSource.volume = Mathf.Clamp01(volume);
        _audioSource.Play();
    }

    public void FadeToVolume(float targetVolume, float duration)
    {
        if (_audioSource == null)
            return;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(targetVolume, duration));
    }

    private System.Collections.IEnumerator FadeVolumeCoroutine(float targetVolume, float duration)
    {
        float start = _audioSource.volume;
        float elapsed = 0f;
        targetVolume = Mathf.Clamp01(targetVolume);

        if (duration <= 0f)
        {
            _audioSource.volume = targetVolume;
            _fadeCoroutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _audioSource.volume = Mathf.Lerp(start, targetVolume, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        _audioSource.volume = targetVolume;
        _fadeCoroutine = null;
    }
}
