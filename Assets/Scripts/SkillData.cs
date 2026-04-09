using UnityEngine;

public enum SkillEffect
{
    none,
    damagebuff,
    atkbuff,
    deepbreath
}


[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("스킬 이름")]
    public string Skillname;

    [Header("스킬 설명")]
    [TextArea(3, 5)]
    public string Description;

    [Header("쿨타임 (CT)")]
    public int CT;

    [Header("스킬 효과")]
    public SkillEffect skillEffect;

    [Header("효과 수치 (예: 피해량 +5)")]
    public int effectValue;

    [Header("지속 턴 수")]
    public int Duration;
}
