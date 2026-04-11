// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Board/ 신규 추가
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyUnit : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool hasMoved = false;
    public CardData[] cards;

    [Header("스킬")]
    public SkillData[] skillData;
    public SkillRuntimeSlot[] skillSlots;
    [SerializeField] private int skillSequenceIndex = 0;

    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        InitSkillCT();
    }

    private void OnDestroy()
    {
        // 사망 시 타일 점유 해제
        if (MapManager.Instance != null &&
            MapManager.Instance.tiles.TryGetValue(gridPosition, out Tile tile))
        {
            tile.isOccupied = false;
            tile.currentUnit = null;
        }
    }

    private void InitSkillCT()
    {
        skillSlots = new SkillRuntimeSlot[skillData?.Length ?? 0];
        if (skillData == null) return;
        for (int i = 0; i < skillData.Length; i++)
            skillSlots[i] = new SkillRuntimeSlot { skillName = skillData[i].skillName, currentCT = 0 };
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

    public void UseSkill(int index)
    {
        if (!IsSkillReady(index)) return;
        SkillData skill = skillData[index];
        ApplySkillEffect(skill);
        skillSlots[index].currentCT = skill.coolTime;
    }

    // 행마할 때마다 호출 — 순서대로 스킬 자동 발동
    public void UseNextSkillInSequence()
    {
        if (skillData == null || skillData.Length == 0) return;

        int currentIndex = skillSequenceIndex;
        SkillData skill = skillData[currentIndex];
        skillSequenceIndex = (skillSequenceIndex + 1) % skillData.Length;

        if (skillSlots != null && currentIndex < skillSlots.Length)
            skillSlots[currentIndex].currentCT = skill.coolTime;

        ApplySkillEffect(skill);
    }

    private void ApplySkillEffect(SkillData skill)
    {
        switch (skill.skillEffect)
        {
            case SkillEffect.damagebuff:
                ApplyBuff(new ActiveBuff(skill.effectValue, skill.duration));
                if (enemy != null)
                    enemy.damage = (enemy.EnemyData != null ? enemy.EnemyData.atk : 0) + GetDamageBonus();
                Debug.Log($"[스킬] {gameObject.name} ▶ {skill.skillName} | 피해량 +{skill.effectValue} ({skill.duration}턴) | 현재 ATK = {(enemy != null ? enemy.damage : 0)}");
                break;

            case SkillEffect.deepbreath:
                if (enemy != null)
                {
                    enemy.RecoverSt(skill.effectValue);
                    int maxSt = enemy.EnemyData != null ? enemy.EnemyData.maxSt : 0;
                    Debug.Log($"[스킬] {gameObject.name} ▶ {skill.skillName} | ST +{skill.effectValue} | 현재 ST = {enemy.CurrentSt}/{maxSt}");
                }
                break;

            case SkillEffect.atkbuff:
                if (enemy != null)
                {
                    enemy.damage += skill.effectValue;
                    Debug.Log($"[스킬] {gameObject.name} ▶ {skill.skillName} | ATK +{skill.effectValue} | 현재 ATK = {enemy.damage}");
                }
                break;

            default:
                Debug.Log($"[스킬] {gameObject.name} ▶ {skill.skillName} | 발동 (별도 효과 없음)");
                break;
        }
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

        if (enemy != null && enemy.EnemyData != null)
            enemy.damage = enemy.EnemyData.atk + GetDamageBonus();
    }
}
