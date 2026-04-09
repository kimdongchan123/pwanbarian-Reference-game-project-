using System.Collections.Generic;
using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool hasMoved = false;
    public CardData[] cards;

    [Header("스킬")]
    public SkillData[] skillData;
    public SkillRuntimeSlot[] skillSlots;

    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    private void Start()
    {
        GridManager.Instance?.RegisterEnemy(this);
        InitSkillCT();
    }

    private void OnDestroy()
    {
        GridManager.Instance?.UnregisterEnemy(this);
    }

    private void InitSkillCT()
    {
        skillSlots = new SkillRuntimeSlot[skillData?.Length ?? 0];
        if (skillData == null) return;
        for (int i = 0; i < skillData.Length; i++)
            skillSlots[i] = new SkillRuntimeSlot { skillName = skillData[i].Skillname, currentCT = 0 };
    }

    public void SetGridPosition(Vector2Int newPos)
    {
        gridPosition = newPos;
        transform.position = new Vector3(newPos.x, newPos.y, 0f);
        hasMoved = true;
    }

    // ============================
    // 스킬 CT 관리
    // ============================
    public void TickSkillCT()
    {
        if (skillSlots == null) return;
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i].currentCT > 0)
                skillSlots[i].currentCT--;
        }
    }

    public bool IsSkillReady(int index)
    {
        if (skillSlots == null || index >= skillSlots.Length) return false;
        return skillSlots[index].currentCT <= 0;
    }

    public SkillData GetReadySkill(out int index)
    {
        index = -1;
        if (skillData == null || skillSlots == null) return null;
        for (int i = 0; i < skillData.Length; i++)
        {
            if (skillSlots[i].currentCT <= 0)
            {
                index = i;
                return skillData[i];
            }
        }
        return null;
    }

    public void UseSkill(int index)
    {
        if (!IsSkillReady(index)) return;
        SkillData skill = skillData[index];

        if (skill.skillEffect == SkillEffect.damagebuff)
            ApplyBuff(new ActiveBuff(skill.effectValue, skill.Duration));

        skillSlots[index].currentCT = skill.CT;
        Debug.Log($"{gameObject.name} 스킬 사용: {skill.Skillname}");
    }

    // ============================
    // 버프 관리
    // ============================
    public int GetDamageBonus()
    {
        int bonus = 0;
        foreach (var buff in activeBuffs)
            bonus += buff.damageBonus;
        return bonus;
    }

    public void ApplyBuff(ActiveBuff buff)
    {
        activeBuffs.Add(buff);
        Debug.Log($"{gameObject.name} 버프 적용: 피해량 +{buff.damageBonus} ({buff.turnsRemaining}턴)");
    }

    public void TickBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].turnsRemaining--;
            if (activeBuffs[i].turnsRemaining <= 0)
                activeBuffs.RemoveAt(i);
        }
    }
}
