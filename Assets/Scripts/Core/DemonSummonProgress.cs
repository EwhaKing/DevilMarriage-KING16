using UnityEngine;

/// <summary>
/// 악마 소환 스킬 해금/표시 조건. 동료가 된 악마만 패널에 올립니다.
/// </summary>
public static class DemonSummonProgress
{
    public const int DefaultPanelUnlockStage = 11;

    public static int GetCurrentPlayStage()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        return stage != null ? stage.stageNumber : 0;
    }

    public static bool IsSkillPanelVisible(int currentStage, DemonSkillCatalog catalog)
    {
        int unlockStage = catalog != null ? catalog.panelUnlockStage : DefaultPanelUnlockStage;
        return currentStage >= unlockStage;
    }

    public static bool TryUseSkill(DemonSkillDefinition skill)
    {
        if (skill == null || skill.id == DemonSkillId.None)
            return false;

        var resources = StageResourceManager.Instance;
        if (resources == null || resources.IsGameOver)
            return false;

        switch (skill.effectType)
        {
            case DemonSkillEffectType.RestoreSanity:
                resources.RestoreSanity(skill.effectValue);
                return true;
            case DemonSkillEffectType.TeleportToRune:
                int cost = skill.effectValue;
                if (cost <= 0 && skill.id == DemonSkillId.WeddingPlanner)
                    cost = 30;
                if (cost > 0)
                    resources.ReduceSanity(cost);
                return true;
            default:
                Debug.LogWarning($"[DemonSummon] 아직 구현되지 않은 스킬 효과: {skill.effectType}");
                return false;
        }
    }
}
