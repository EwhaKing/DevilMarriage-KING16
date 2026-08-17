using System;
using UnityEngine;

[Serializable]
public class DemonSkillDefinition
{
    public DemonSkillId id = DemonSkillId.None;
    public string displayName = "미지정";
    public Sprite icon;
    [Tooltip("이 스테이지 번호부터 소환 동료로 해금되어 패널에 표시됩니다.")]
    public int unlockStage = 11;
    [Tooltip("한 스테이지 시도당 사용 가능 횟수.")]
    public int usesPerStage = 1;
    public DemonSkillEffectType effectType = DemonSkillEffectType.None;
    public int effectValue = 0;
}
