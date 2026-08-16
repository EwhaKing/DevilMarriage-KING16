using UnityEngine;

// 능력별 분류 탭을 위한 Enum
public enum DevilCategory
{
    All,            // 전체
    PuzzleAssist,   // 퍼즐 보조
    SpecialAbility  // 특수 능력
}

[CreateAssetMenu(fileName = "NewDevilData", menuName = "Game Data/Devil Data")]
public class DevilData : ScriptableObject
{
    [Header("기본 정보")]
    public int devilNumber;          // 도감 번호
    public string devilName;         // 이름
    public Sprite portrait;          // 초상화 (큰 이미지 및 버튼 이미지)
    public DevilCategory category;   // 능력 분류
    public bool isUnlocked;          // 해금 여부

    [Header("상세 설명")]
    [TextArea] public string description;  // 캐릭터 설명
    public string quote;                   // 한마디

    [Header("스킬 정보")]
    [TextArea] public string skillInfo;    // 스킬 정보 (스킬명 + 스킬 설명 통째로 작성)
}