using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DemonSkillCatalog", menuName = "DevilMarriage/Demon Skill Catalog")]
public class DemonSkillCatalog : ScriptableObject
{
    public const string ResourcesPath = "DemonSkillCatalog";

    [Tooltip("이 번호부터 StagePlay 하단에 악마 소환 스킬 패널을 표시합니다.")]
    public int panelUnlockStage = 11;

    public DemonSkillDefinition[] skills = System.Array.Empty<DemonSkillDefinition>();

    public static DemonSkillCatalog Load()
    {
        return Resources.Load<DemonSkillCatalog>(ResourcesPath);
    }

    public IReadOnlyList<DemonSkillDefinition> GetAvailableSkills(int currentStage, DemonSkillId[] stageFilter = null)
    {
        var result = new List<DemonSkillDefinition>();
        if (skills == null)
            return result;

        foreach (var skill in skills)
        {
            if (skill == null || skill.id == DemonSkillId.None)
                continue;
            if (currentStage < skill.unlockStage)
                continue;
            if (stageFilter != null && stageFilter.Length > 0 && !Contains(stageFilter, skill.id))
                continue;
            result.Add(skill);
        }

        return result;
    }

    private static bool Contains(DemonSkillId[] ids, DemonSkillId id)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == id)
                return true;
        }

        return false;
    }
}
