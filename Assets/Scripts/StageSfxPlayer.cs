using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 스테이지 플레이 중 자원 변화에 맞춰 효과음을 재생한다.
/// </summary>
public class StageSfxPlayer : MonoBehaviour
{
    [SerializeField] private StageResourceManager resourceManager;
    [SerializeField] private AudioClip bloodSplatterClip;
    [SerializeField] private AudioClip sanityDownClip;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] [Range(0f, 1f)] private float bloodSplatterVolume = 0.35f;

    private AudioSource _audioSource;
    private int _previousBlood = -1;
    private int _previousSanity = -1;

    private void Awake()
    {
        if (resourceManager == null)
            resourceManager = GetComponent<StageResourceManager>()
                ?? StageResourceManager.Instance
                ?? FindFirstObjectByType<StageResourceManager>();

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;

        if (sfxMixerGroup != null)
            _audioSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void OnEnable()
    {
        if (resourceManager == null)
            return;

        resourceManager.OnRatBloodChanged += HandleBloodChanged;
        resourceManager.OnSanityChanged += HandleSanityChanged;
    }

    private void OnDisable()
    {
        if (resourceManager == null)
            return;

        resourceManager.OnRatBloodChanged -= HandleBloodChanged;
        resourceManager.OnSanityChanged -= HandleSanityChanged;
    }

    private void Start()
    {
        if (resourceManager == null)
            return;

        _previousBlood = resourceManager.CurrentRatBlood;
        _previousSanity = resourceManager.CurrentSanity;
    }

    private void HandleBloodChanged(int current, int max)
    {
        if (_previousBlood >= 0 && current < _previousBlood)
            Play(bloodSplatterClip, bloodSplatterVolume);

        _previousBlood = current;
    }

    private void HandleSanityChanged(int current, int max)
    {
        if (_previousSanity >= 0 && current < _previousSanity)
            Play(sanityDownClip, 1f);

        _previousSanity = current;
    }

    private void Play(AudioClip clip, float volumeScale)
    {
        if (clip == null || _audioSource == null)
            return;

        _audioSource.PlayOneShot(clip, volumeScale);
    }
}
