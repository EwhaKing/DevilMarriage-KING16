/// <summary>
/// 악마 소환 스킬 식별자. 새 악마를 추가할 때 여기에 항목을 더하면 됩니다.
/// </summary>
public enum DemonSkillId
{
    None = 0,
    BackingTrio = 1,
    WeddingPlanner = 2,
}

/// <summary>
/// 악마 소환 스킬이 퍼즐에 적용하는 효과 종류.
/// </summary>
public enum DemonSkillEffectType
{
    None = 0,
    RestoreSanity = 1,
    TeleportToRune = 2,
}
