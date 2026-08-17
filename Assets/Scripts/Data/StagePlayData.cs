using UnityEngine;

[CreateAssetMenu(fileName = "StagePlayData", menuName = "DevilMarriage/Stage Play Data")]
public class StagePlayData : ScriptableObject
{
    [Header("HUD")]
    public string stageCode = "1-1";
    public string stageTitle = "임시 스테이지";

    [Header("Resources")]
    public int maxSanity = 100;
    public int maxRatBlood = 15;

    [Header("Puzzle")]
    [Tooltip("이동 1회당 쥐의 피 소모량")]
    public int bloodCostPerMove = 1;

    [Tooltip("이 스테이지에서 생성할 퍼즐 Prefab (Stage1Puzzle, Stage2Puzzle 등). 비우면 StagePlayScene에 있는 기본 퍼즐을 사용합니다.")]
    public GameObject puzzlePrefab;

    [Header("Demon Summon Skills")]
    [Tooltip("비워두면 해금된 악마를 모두 표시합니다. 값을 넣으면 이 스테이지에서 쓸 악마만 남깁니다.")]
    public DemonSkillId[] availableDemonSkills;
}
