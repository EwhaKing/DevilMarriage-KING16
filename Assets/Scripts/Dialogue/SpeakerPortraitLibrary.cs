using UnityEngine;

/// <summary>
/// 화자별 기본 초상화. Resources/SpeakerPortraits 에서 로드합니다.
/// 일렉·키보드·드럼은 모두 rockDefault(락_기본)를 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "SpeakerPortraits", menuName = "DevilMarriage/Speaker Portraits")]
public class SpeakerPortraitLibrary : ScriptableObject
{
    public Sprite lucia;
    public Sprite rockDefault;

    public Sprite Resolve(string speakerName)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
            return null;

        switch (speakerName.Trim())
        {
            case "루시아":
                return lucia;
            case "일렉":
            case "키보드":
            case "드럼":
            case "마수":
            case "고양이마수":
                return rockDefault;
            default:
                return null;
        }
    }
}
