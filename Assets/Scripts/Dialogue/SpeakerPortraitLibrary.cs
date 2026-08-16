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
    [Tooltip("애쉬 / 화동 초상화")]
    public Sprite ash;
    [Tooltip("발명가 / 레온 초상화 (레온-예식장준비자)")]
    public Sprite inventor;
    [Tooltip("아스벨 초상화 (아스벨-스타일리스트)")]
    public Sprite asbel;
    [Tooltip("요리사 초상화 (아인-요리사)")]
    public Sprite chef;
    [Tooltip("사진사 초상화 (룩스-사진사)")]
    public Sprite photographer;
    [Tooltip("사회자 초상화 (피에르_사회자)")]
    public Sprite host;

    public Sprite Resolve(string speakerName)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
            return null;

        switch (speakerName.Trim())
        {
            case "루시아":
                return lucia;
            case "키보드":
            case "붐":
                return keyboard != null ? keyboard : rockDefault;
            case "드럼":
            case "쾅":
                return drum != null ? drum : rockDefault;
            case "벨리안":
            case "???":
                return belian != null ? belian : rockDefault;
            case "일렉":
            case "일렉&주인공":
            case "리더":
            case "락":
                return rockDefault;
            case "애쉬":
                return ash;
            case "발명가":
            case "레온":
                return inventor;
            case "아스벨":
                return asbel;
            case "요리사":
            case "아인":
                return chef;
            case "사진사":
            case "사진사는":
            case "룩스":
                return photographer;
            case "사회자":
            case "피에르":
                return host;
            case "마수":
            case "고양이마수":
            case "링베어러":
            case "네로":
                return ringBearer != null ? ringBearer : rockDefault;
            default:
                return null;
        }
    }
}
