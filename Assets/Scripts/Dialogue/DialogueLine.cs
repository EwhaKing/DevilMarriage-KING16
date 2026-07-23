using System;
using UnityEngine;

/// <summary>
/// 대사 "한 줄"에 필요한 정보를 담는 데이터입니다.
/// ScriptableObject 안의 리스트에 여러 개를 넣어 대화를 구성합니다.
/// </summary>
[Serializable]
public class DialogueLine
{
    [Tooltip("화면에 표시할 화자 이름 (예: 주인공, 나레이션, 엄마)")]
    public string speakerName = "";

    [Tooltip("실제로 출력할 대사 문장입니다. 여러 줄로 적어도 됩니다.")]
    [TextArea(2, 6)]
    public string dialogueText = "";

    [Tooltip("이 줄에서 보여줄 캐릭터 스프라이트 (비워두면 이전 이미지를 유지)")]
    public Sprite characterSprite;

    [Tooltip("표정 ID. 비우면 이전 표정 유지.\n예: default, wake, nervous, dark, angry, cry, sparkle, scheming, happy")]
    public string expressionId = "";

    [Tooltip("캐릭터를 왼쪽/가운데/오른쪽 중 어디에 둘지")]
    public CharacterPosition characterPosition = CharacterPosition.Center;

    [Tooltip("이 줄에서 바꿀 배경 이미지 (비워두면 이전 배경 유지)")]
    public Sprite backgroundImage;

    [Tooltip("이 줄을 시작할 때 실행할 이벤트 ID (없으면 비워두세요).\n예: RequestPlayerName, FadeToBlack, GoToStageSelect")]
    public string eventId = "";
}
