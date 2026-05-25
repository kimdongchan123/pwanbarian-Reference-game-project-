// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Board/ 신규 추가
using System.Collections.Generic;
using UnityEngine;

// 🌟 추가: 팀원분의 기존 버프 시스템이 꼬이지 않도록 이름을 'LegacyEnemyBuff'로 분리했습니다!
[System.Serializable]
public class LegacyEnemyBuff
{
    public int damageBonus;
    public int turnsRemaining;

    public LegacyEnemyBuff(int bonus, int turns)
    {
        damageBonus = bonus;
        turnsRemaining = turns;
    }
}

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
    private int lastFiredSkillIndex = -1;

    [Header("스프라이트 교체 (선택)")]
    public Sprite buffedSprite;
    private SpriteRenderer sr;
    private Sprite originalSprite;

    // 🌟 수정: ActiveBuff 대신 LegacyEnemyBuff 사용
    private List<LegacyEnemyBuff> activeBuffs = new List<LegacyEnemyBuff>();
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalSprite = sr.sprite;
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

        // 쿨타임 중이면 건너뜀
        if (!IsSkillReady(currentIndex)) return;

        SkillData skill = skillData[currentIndex];
        skillSequenceIndex = (skillSequenceIndex + 1) % skillData.Length;

        // CT는 턴 종료 시점에 설정되므로 여기서는 발동 인덱스만 기록
        lastFiredSkillIndex = currentIndex;

        ApplySkillEffect(skill);
    }

    // 턴 종료 시 호출: 버프 만료 + 이번 턴에 발동한 스킬의 CT를 설정
    public void OnEnemyTurnEnd()
    {
        TickBuffs();

        if (lastFiredSkillIndex >= 0
            && skillData != null && skillSlots != null
            && lastFiredSkillIndex < skillData.Length
            && lastFiredSkillIndex < skillSlots.Length
            && skillData[lastFiredSkillIndex] != null)
        {
            skillSlots[lastFiredSkillIndex].currentCT = skillData[lastFiredSkillIndex].coolTime;
        }
        lastFiredSkillIndex = -1;

        // 스프라이트 복원
        if (sr != null && originalSprite != null)
            sr.sprite = originalSprite;
    }

    private void ApplySkillEffect(SkillData skill)
    {
        switch (skill.skillEffect)
        {
            case SkillEffect.damagebuff:
                // 🌟 수정: LegacyEnemyBuff 로 생성
                activeBuffs.RemoveAll(b => b.damageBonus > 0);
                ApplyBuff(new LegacyEnemyBuff(skill.effectValue, skill.duration));
                if (enemy != null)
                    enemy.damage = (enemy.EnemyData != null ? enemy.EnemyData.maxatk : 0) + GetDamageBonus();
                if (sr != null && buffedSprite != null)
                    sr.sprite = buffedSprite;
                Debug.Log($"💢 [기합 발동!] {gameObject.name} ▶ {skill.skillName} | ATK +{skill.effectValue} ({skill.duration}턴) | 현재 ATK = {(enemy != null ? enemy.damage : 0)}");
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

            // ── 신체재생: HP·St 회복 + 자바무너의 다리 1~2개 부활 ───────────
            case SkillEffect.bodyRegen:
                if (enemy != null && enemy.EnemyData != null)
                {
                    int hpRec = Mathf.Max(1, Mathf.RoundToInt(enemy.EnemyData.maxHp * 0.1f));
                    enemy.CurrentHp = Mathf.Min(enemy.CurrentHp + hpRec, enemy.EnemyData.maxHp);

                    int stRec = Mathf.Max(1, Mathf.RoundToInt(enemy.EnemyData.maxSt * 0.1f));
                    enemy.RecoverSt(stRec);

                    int reviveCount = Random.Range(1, 3);
                    int revived = 0;
                    for (int i = 0; i < reviveCount; i++)
                    {
                        if (EnemySpawnManager.Instance != null &&
                            EnemySpawnManager.Instance.SpawnEnemy("자바문어의 다리") != null)
                            revived++;
                    }
                    Debug.Log($"[신체재생] {gameObject.name} HP+{hpRec}({enemy.CurrentHp}/{enemy.EnemyData.maxHp}) St+{stRec} 다리 {revived}개 부활");
                }
                break;

            // ── 용언세뇌: 자신보다 maxSt 낮은 적 1명에게 세뇌(8) 부여 ──────
            case SkillEffect.brainwash:
            {
                int selfMaxSt = enemy?.EnemyData?.maxSt ?? 0;
                BattleUnit brainTarget = null;
                foreach (var unit in FindObjectsByType<BattleUnit>(FindObjectsSortMode.None))
                {
                    if (unit.data == null) continue;
                    if (unit.data.maxSt < selfMaxSt &&
                        (brainTarget == null || unit.data.maxSt < brainTarget.data.maxSt))
                        brainTarget = unit;
                }
                if (brainTarget != null && enemy != null)
                {
                    int drain = brainTarget.currentSt;
                    enemy.CurrentSt = Mathf.Max(0, enemy.CurrentSt - drain);
                    brainTarget.AddStatus(StatusEffectType.Charm, 1, 8);
                    Debug.Log($"[용언세뇌] {gameObject.name} → {brainTarget.name} | 자신 St -{drain}, 세뇌(Charm×8) 부여");
                }
                else
                {
                    Debug.Log($"[용언세뇌] {gameObject.name} — 대상 없음(자신보다 maxSt 낮은 아군 기물 없음)");
                }
                break;
            }

            // ── 지원 요청: 우앙개미 0~2마리 소환 ────────────────────────────
            case SkillEffect.callForAid:
            {
                int spawnCount = Random.Range(0, 3);
                int spawned = 0;
                for (int i = 0; i < spawnCount; i++)
                {
                    if (EnemySpawnManager.Instance != null &&
                        EnemySpawnManager.Instance.SpawnEnemy("우앙개미") != null)
                        spawned++;
                }
                Debug.Log($"[지원 요청] {gameObject.name} — 우앙개미 {spawned}마리 소환");
                break;
            }

            // ── 용의 마력: 마나 1 축적 (거센 불길 트리거 기준: 10) ───────────
            case SkillEffect.dragonMana:
                if (enemy != null)
                {
                    enemy.manaStacks++;
                    Debug.Log($"[용의 마력] {gameObject.name} — 마나 {enemy.manaStacks}스택 (거센 불길 발동 조건: 10스택)");
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

    // 🌟 수정: 매개변수 타입 변경
    public void ApplyBuff(LegacyEnemyBuff buff)
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
            enemy.damage = enemy.EnemyData.maxatk + GetDamageBonus();
    }
}