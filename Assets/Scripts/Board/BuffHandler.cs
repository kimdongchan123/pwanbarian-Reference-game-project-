using System.Collections.Generic;
using UnityEngine;

public class BuffHandler : MonoBehaviour
{
    [SerializeField] private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    private BattleUnit battleUnit;

    private void Awake()
    {
        battleUnit = GetComponent<BattleUnit>();
    }

    public void AddBuff(BuffType type, float value, int duration)
    {
        if (battleUnit != null)
        {
            battleUnit.AddStatus(ToStatusEffectType(type), Mathf.RoundToInt(value));
            Debug.Log($"{gameObject.name}: {type} status applied through BattleUnit.");
            return;
        }

        activeBuffs.Add(new ActiveBuff(type, value, duration));
        Debug.Log($"{gameObject.name}: {type} buff applied.");
    }

    public float GetTotalModifier(BuffType type)
    {
        if (battleUnit != null)
        {
            return battleUnit.GetStatusAmount(ToStatusEffectType(type));
        }

        float total = 0;
        foreach (ActiveBuff buff in activeBuffs)
        {
            if (buff.type == type)
            {
                total += buff.value;
            }
        }

        return total;
    }

    public void TickBuffs()
    {
        if (battleUnit != null)
        {
            return;
        }

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].duration--;
            if (activeBuffs[i].duration <= 0)
            {
                activeBuffs.RemoveAt(i);
            }
        }
    }

    private StatusEffectType ToStatusEffectType(BuffType type)
    {
        switch (type)
        {
            case BuffType.AtkUp:
                return StatusEffectType.AtkUp;
            case BuffType.DefenseUp:
                return StatusEffectType.DefUp;
            case BuffType.SpeedUp:
                return StatusEffectType.Quick;
            case BuffType.DamageUp:
                return StatusEffectType.DamageUp;
            case BuffType.Protection:
                return StatusEffectType.Protection;
            case BuffType.AtkDown:
                return StatusEffectType.AtkDown;
            case BuffType.DefenseDown:
                return StatusEffectType.DefDown;
            case BuffType.SpeedDown:
                return StatusEffectType.Slow;
            case BuffType.DamageDown:
                return StatusEffectType.DamageDown;
            case BuffType.Weakness:
                return StatusEffectType.Weakness;
            default:
                return StatusEffectType.None;
        }
    }
}
