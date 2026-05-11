using UnityEngine;

public enum SkillEffect
{
    none,
    damagebuff,
    atkbuff,
    deepbreath,
    hpbuff
}

public enum SkillTargetType
{
    Self,
    Enemy,
    Ally,
}

public enum SkillCostType
{
    None,
    HP,
    ST,
}

[CreateAssetMenu(menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;

    [TextArea(2, 5)]
    public string description;

    public int coolTime = 0;

    [Header("Use Condition")]
    public SkillTargetType targetType = SkillTargetType.Enemy;
    public SkillCostType costType = SkillCostType.None;
    public int costAmount = 0;
    public bool consumeAction = true;

    [Header("Damage")]
    public bool useDamage = false;
    public DamageType damageType = DamageType.Physical;
    public int power = 0;
    public bool includeAtk = true;

    [Header("Status Effect")]
    public bool useStatusEffect = false;
    public StatusEffectType statusEffectType = StatusEffectType.None;
    public int statusAmount = 0;
    public int statusCount = 0;
    public bool statusToSelf = false;

    [Header("Recover")]
    public int hpRecover = 0;
    public int stRecover = 0;

    [Header("Legacy Compatibility")]
    public SkillEffect skillEffect;
    public int effectValue;
    public int duration;
}
