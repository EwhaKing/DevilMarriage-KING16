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
    public int bloodCostPerMove = 1;
}
