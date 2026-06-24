using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 씬 진입 시 한 번 재생되는 효과음/징글 (스테이지 클리어, 게임 오버 등).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SceneStingerPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip stingerClip;
    [SerializeField] private AudioMixerGroup mixerGroup;

    private void Awake()
    {
        var source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;

        if (mixerGroup != null)
            source.outputAudioMixerGroup = mixerGroup;

        if (stingerClip == null)
            return;

        source.clip = stingerClip;
        source.Play();
    }
}
