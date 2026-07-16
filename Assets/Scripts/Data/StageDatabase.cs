using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDatabase", menuName = "DevilMarriage/Stage Database")]
public class StageDatabase : ScriptableObject
{
    [SerializeField] private List<StageData> stages = new();

    public IReadOnlyList<StageData> Stages => stages;

    public StageData GetStage(int stageNumber)
    {
        foreach (var stage in stages)
        {
            if (stage != null && stage.stageNumber == stageNumber)
                return stage;
        }

        return null;
    }

    public int StageCount => stages.Count;
}
