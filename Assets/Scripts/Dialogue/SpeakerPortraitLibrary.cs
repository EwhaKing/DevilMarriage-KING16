using UnityEngine;

/// <summary>
/// 화자별 기본 초상화. Resources/SpeakerPortraits 에서 로드합니다.
/// </summary>
[CreateAssetMenu(fileName = "SpeakerPortraits", menuName = "DevilMarriage/Speaker Portraits")]
public class SpeakerPortraitLibrary : ScriptableObject
{
    public Sprite lucia;
    public Sprite rockDefault;
    [Tooltip("마수 / 링베어러 초상화")]
    public Sprite ringBearer;
    [Tooltip("키보드 초상화 (붐_기본)")]
    public Sprite keyboard;
    [Tooltip("드럼 초상화 (쾅_기본)")]
    public Sprite drum;
    [Tooltip("벨리안 초상화 (벨리안_기본)")]
    public Sprite belian;

    public Sprite Resolve(string speakerName)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
            return null;

        switch (speakerName.Trim())
        {
            case "루시아":
                return lucia;
            case "키보드":
                return keyboard != null ? keyboard : rockDefault;
            case "드럼":
                return drum != null ? drum : rockDefault;
            case "벨리안":
            case "???":
                return belian != null ? belian : rockDefault;
            case "일렉":
            case "일렉&주인공":
                return rockDefault;
            case "마수":
            case "고양이마수":
            case "링베어러":
                return ringBearer != null ? ringBearer : rockDefault;
            default:
                return null;
        }
    }
}
