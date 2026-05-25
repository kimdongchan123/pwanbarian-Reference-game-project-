using UnityEngine;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    [Header("기본 정보")]
    public string unitName = "테스트 유닛";
    public bool isAlly = true;
    public int formationIndex = 0;
    public UnitData data;

    [Header("전투 스탯")]
    public int maxHp = 20;
    [HideInInspector] public int currentHp;
    [HideInInspector] public int maxSt;
    [HideInInspector] public int currentSt;

    [Header("핵심 시스템 모듈 (자동 연결됨)")]
    public UnitMovement movement;
    public UnitStats stats;
    private BuffHandler buffHandler;
    private readonly List<StatusEffect> statusEffects = new List<StatusEffect>();
    private readonly Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();
    private readonly Dictionary<string, int> skillUseCounts = new Dictionary<string, int>();
    private readonly List<StatusEffectType> temporaryTurnStatuses = new List<StatusEffectType>();
    private bool pendingBurnOnAttack;
    private bool pendingFocusGapDamageOnAttack;
    private bool pendingGiantSlayer;
    private bool pendingSupportShot;
    private bool isRetreated;
    private bool returnFromRetreatNextTurn;
    private bool extraMoveAvailable;
    private bool unyieldingUsed;
    private bool unyieldingProtectionActive;

    public UnitData UnitData => data;

    void Awake()
    {
        movement = GetComponent<UnitMovement>();
        stats = GetComponent<UnitStats>();
        buffHandler = GetComponent<BuffHandler>();

        if (movement == null) Debug.LogWarning($" {unitName} : UnitMovement 없음");
        if (stats == null) Debug.LogWarning($" {unitName} : UnitStats 없음");

        // 버프 핸들러가 없다면 자동으로 추가합니다.
        if (buffHandler == null) buffHandler = gameObject.AddComponent<BuffHandler>();
    }

    void Start()
    {
        if (currentHp <= 0) currentHp = maxHp;
    }

    public void Initialize(UnitData unitData)
    {
        data = unitData;
        if (data == null) return;

        unitName = data.unitName;
        name = data.unitName;
        maxHp = data.maxHp;
        maxSt = data.maxSt;
        currentHp = maxHp;
        currentSt = maxSt;
        unyieldingUsed = false;
        unyieldingProtectionActive = false;

        if (stats != null)
        {
            stats.baseAttack = Mathf.Max(1, data.minAtk);
            stats.minSpeed = data.minSp;
            stats.maxSpeed = data.maxSp;
        }

        Sprite battleSprite = data.battleSprite != null ? data.battleSprite : data.portraitSprite;
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && battleSprite != null)
        {
            spriteRenderer.sprite = battleSprite;
        }

        skillCooldowns.Clear();
        skillUseCounts.Clear();
        if (data.skills == null) return;

        foreach (SkillData skill in data.skills)
        {
            if (skill != null && !skillCooldowns.ContainsKey(skill.skillName))
            {
                skillCooldowns.Add(skill.skillName, 0);
            }
        }
    }

    public SkillData[] GetSkills()
    {
        return data != null && data.skills != null ? data.skills : System.Array.Empty<SkillData>();
    }

    public bool CanUseSkill(SkillData skill)
    {
        if (skill == null) return false;
        if (skillCooldowns.TryGetValue(skill.skillName, out int cooldown) && cooldown > 0) return false;
        if (GetSkillUseLimit(skill.skillName) > 0 && GetSkillUseCount(skill.skillName) >= GetSkillUseLimit(skill.skillName)) return false;
        if (skill.costType == SkillCostType.HP && currentHp <= skill.costAmount) return false;
        if (skill.costType == SkillCostType.ST && currentSt < skill.costAmount) return false;
        if (skill.skillName == "Gda.Over 10(k)" && GetStatusAmount(StatusEffectType.Charge) < 3) return false;
        if (skill.skillName == "황금저축" && GetStatusAmount(StatusEffectType.GoldenTime) <= 0) return false;
        return true;
    }

    public bool UseSkill(SkillData skill)
    {
        if (!CanUseSkill(skill))
        {
            Debug.LogWarning($"{unitName}: {skill?.skillName} 사용 불가");
            return false;
        }

        PaySkillCost(skill);
        ApplySkillEffect(skill);
        skillCooldowns[skill.skillName] = skill.coolTime;
        skillUseCounts[skill.skillName] = GetSkillUseCount(skill.skillName) + 1;
        Debug.Log($"{unitName} 스킬 사용: {skill.skillName}");
        return true;
    }

    public int GetSkillCooldown(SkillData skill)
    {
        if (skill == null) return 0;
        return skillCooldowns.TryGetValue(skill.skillName, out int cooldown) ? cooldown : 0;
    }

    public int GetSkillUseCount(string skillName)
    {
        return skillUseCounts.TryGetValue(skillName, out int count) ? count : 0;
    }

    public int GetSkillUseLimit(string skillName)
    {
        return skillName == "퀵드로우" ? 3 : 0;
    }

    // 🌟 카드를 써서 공격할 때 호출되는 진짜 공격력 (버프 포함)
    public int GetAttackPower()
    {
        int baseAttack = stats != null ? stats.CurrentAttack : 5;
        if (data != null)
        {
            baseAttack = Random.Range(data.minAtk, data.maxAtk + 1);
        }

        baseAttack += GetStatusAmount(StatusEffectType.AtkUp);
        baseAttack -= GetStatusAmount(StatusEffectType.AtkDown);
        return Mathf.Max(1, baseAttack);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(int damage, Enemy attacker)
    {
        if (isRetreated)
        {
            Debug.Log($"{unitName}은(는) 퇴각 중이라 피해를 받지 않습니다.");
            return;
        }

        int finalDamage = CalculateIncomingDamage(damage);
        if (finalDamage <= 0)
        {
            Debug.Log($"{unitName}: 방어 특성으로 피해를 받지 않음");
            return;
        }

        if (currentHp - finalDamage <= 0 && HasTrait("불굴") && !unyieldingUsed)
        {
            unyieldingUsed = true;
            unyieldingProtectionActive = true;
            currentHp = 1;
            Debug.Log($"{unitName} 불굴 발동: 사망 피해를 버팀");
            return;
        }

        currentHp = Mathf.Max(0, currentHp - finalDamage);
        Debug.Log($"{name}이(가) {finalDamage} 피해를 받음. 남은 체력: {currentHp}");

        ApplyTraitsOnHit(attacker);

        if (currentHp <= 0)
        {
            Debug.Log($"{name} 사망");
            // 사망 처리 로직 필요 시 추가
        }
    }

    // 🌟 카드 시스템에서 넘겨준 "AtkUp" 데이터를 버프로 바꿔줍니다!
    public void AddStatus(StatusEffectType type, int amount)
    {
        AddStatus(type, amount, 0);
    }

    public void AddStatus(StatusEffectType type, int amount, int count)
    {
        if (type == StatusEffectType.None) return;

        StatusEffect effect = statusEffects.Find(status => status.type == type);
        if (effect == null)
        {
            statusEffects.Add(new StatusEffect(type, amount, count));
        }
        else
        {
            effect.amount += amount;
            effect.count = Mathf.Max(effect.count, count);
        }

        Debug.Log($"🃏 {type} 효과가 {amount}만큼 {name}에게 적용되었습니다.");
    }

    public int GetStatusAmount(StatusEffectType type)
    {
        StatusEffect effect = statusEffects.Find(status => status.type == type);
        return effect != null ? effect.amount : 0;
    }

    public void RemoveStatus(StatusEffectType type, int amount)
    {
        StatusEffect effect = statusEffects.Find(status => status.type == type);
        if (effect == null) return;

        effect.amount -= amount;
        if (effect.amount <= 0) statusEffects.Remove(effect);
    }

    public bool HasStatus(StatusEffectType type, int minAmount = 1)
    {
        return GetStatusAmount(type) >= minAmount;
    }

    public bool HasTrait(string traitName)
    {
        if (data == null || data.traits == null) return false;

        foreach (TraitData trait in data.traits)
        {
            if (trait == null) continue;
            if (trait.traitName == traitName || trait.name == traitName) return true;
        }

        return false;
    }

    public bool ShouldIgnoreIncomingHit(int damage)
    {
        return HasTrait("회피") && damage <= GetDefenseValue();
    }

    public void OnTurnStart()
    {
        ReduceSkillCooldowns();
        ApplyTraitsOnTurnStart();

        if (returnFromRetreatNextTurn)
        {
            SetRetreated(false);
            returnFromRetreatNextTurn = false;
            AddStatus(StatusEffectType.GoldenGuarantee, 5);
            Debug.Log($"{unitName} 출격: 황금시간-이자 +5");
        }
    }

    private void PaySkillCost(SkillData skill)
    {
        if (skill.costType == SkillCostType.HP)
        {
            currentHp = Mathf.Max(1, currentHp - skill.costAmount);
        }
        else if (skill.costType == SkillCostType.ST)
        {
            currentSt = Mathf.Max(0, currentSt - skill.costAmount);
        }
    }

    private void ApplySkillEffect(SkillData skill)
    {
        if (skill.useStatusEffect)
        {
            AddStatus(skill.statusEffectType, skill.statusAmount, skill.statusCount);
        }

        if (skill.hpRecover > 0) currentHp = Mathf.Min(maxHp, currentHp + skill.hpRecover);
        if (skill.stRecover > 0) currentSt = Mathf.Min(maxSt, currentSt + skill.stRecover);

        switch (skill.skillName)
        {
            case "꼬르르르르르크르르륵!!!":
                currentSt = Mathf.Max(0, currentSt - 1);
                pendingFocusGapDamageOnAttack = true;
                Debug.Log("다음 공격 적중 시 잃은 ST만큼 추가 피해가 적용됩니다.");
                break;
            case "퀵드로우":
                AddStatus(StatusEffectType.Charge, 1);
                if (HandUIManager.Instance != null) HandUIManager.Instance.DrawCards(2);
                break;
            case "케이론류 창술 556식 현하희풍":
                AddTemporaryStatus(StatusEffectType.Protection, 1);
                AddTemporaryStatus(StatusEffectType.DefUp, 1);
                break;
            case "케이론류 창술 423식 파도뚫기":
                AddTemporaryStatus(StatusEffectType.AtkUp, 1);
                AddTemporaryStatus(StatusEffectType.DamageUp, 1);
                break;
            case "아류 제이슨 오의 조각해류":
                int breathTotal = GetStatusAmount(StatusEffectType.Breath);
                foreach (Unit ally in FindObjectsByType<Unit>(FindObjectsSortMode.None))
                {
                    if (ally != null && ally.isAlly)
                    {
                        ally.AddTemporaryStatus(StatusEffectType.Frenzy, Mathf.Max(1, breathTotal));
                    }
                }
                break;
            case "용암뚫기":
                pendingBurnOnAttack = true;
                Debug.Log("이번 턴 공격 적중 시 대상에게 화상 1을 부여합니다.");
                break;
            case "깨어진 환상":
                statusEffects.Clear();
                AddStatus(StatusEffectType.Agitation, 10);
                break;
            case "무모한 질주":
                pendingGiantSlayer = true;
                Debug.Log("이번 턴 거인 죽이기 효과가 발동됩니다.");
                break;
            case "불가침 기합넣기":
                int recover = Mathf.Max(5, GetStatusAmount(StatusEffectType.Focus));
                currentHp = Mathf.Min(maxHp, currentHp + recover);
                currentSt = Mathf.Min(maxSt, currentSt + recover);
                RemoveStatus(StatusEffectType.Focus, recover);
                break;
            case "Gda.Over 10(k)":
                if (GetStatusAmount(StatusEffectType.Charge) >= 3)
                {
                    RemoveStatus(StatusEffectType.Charge, 3);
                    AddTemporaryStatus(StatusEffectType.AtkUp, 1);
                    Debug.Log("무장 키워드 선택 처리: 임시 ATK 증가 +1");
                }
                break;
            case "후방 저격":
                pendingSupportShot = true;
                AddTemporaryStatus(StatusEffectType.DamageUp, 3);
                Debug.Log("지원 사격 준비: 다음 공격 피해량 증가");
                break;
            case "황금대출-위프":
                currentHp = Mathf.Max(1, currentHp - 1);
                SetRetreated(true);
                returnFromRetreatNextTurn = true;
                break;
            case "황금저축":
                if (GetStatusAmount(StatusEffectType.GoldenTime) > 0)
                {
                    RemoveStatus(StatusEffectType.GoldenTime, 1);
                    AddStatus(StatusEffectType.GoldenGuarantee, Random.Range(10, 16));
                }
                break;
        }
    }

    public int GetAttackDamageAgainst(Enemy enemy)
    {
        int damage = GetAttackPower() + GetStatusAmount(StatusEffectType.DamageUp) - GetStatusAmount(StatusEffectType.DamageDown);
        damage = ApplyTraitsToAttackDamage(damage, enemy);

        if (pendingFocusGapDamageOnAttack)
        {
            damage += Mathf.Max(0, maxSt - currentSt);
        }

        if (pendingGiantSlayer)
        {
            damage += Mathf.Max(5, Mathf.RoundToInt(damage * 0.5f));
        }

        if (pendingSupportShot)
        {
            damage += 3;
        }

        return Mathf.Max(1, damage);
    }

    public void OnAttackHit(Enemy target)
    {
        ApplyTraitsOnAttackHit(target);

        if (target != null && pendingBurnOnAttack)
        {
            target.AddBurn(1);
            Debug.Log($"{unitName}: 용암뚫기 화상 부여");
        }

        pendingBurnOnAttack = false;
        pendingFocusGapDamageOnAttack = false;
        pendingGiantSlayer = false;
        pendingSupportShot = false;
    }

    // 🌟 턴이 끝날 때 버프 지속 시간을 1턴씩 깎습니다.
    public void OnTurnEnd()
    {
        if (buffHandler != null)
        {
            buffHandler.TickBuffs();
        }

        ApplyTraitsOnTurnEnd();

        foreach (StatusEffectType type in temporaryTurnStatuses)
        {
            RemoveStatus(type, 9999);
        }

        temporaryTurnStatuses.Clear();
        pendingBurnOnAttack = false;
        pendingFocusGapDamageOnAttack = false;
        pendingGiantSlayer = false;
        pendingSupportShot = false;
        unyieldingProtectionActive = false;
        BattleSkillButtonUI.Instance?.Refresh();
    }

    public bool ConsumeExtraMove()
    {
        if (!extraMoveAvailable) return false;

        extraMoveAvailable = false;
        Debug.Log($"{unitName} 신속 발동: 한 번 더 행동 가능");
        return true;
    }

    private void AddTemporaryStatus(StatusEffectType type, int amount)
    {
        AddStatus(type, amount);
        temporaryTurnStatuses.Add(type);
    }

    private void ReduceSkillCooldowns()
    {
        if (data == null || data.skills == null) return;

        foreach (SkillData skill in data.skills)
        {
            if (skill == null) continue;
            if (!skillCooldowns.ContainsKey(skill.skillName))
            {
                skillCooldowns.Add(skill.skillName, 0);
            }

            if (skillCooldowns[skill.skillName] > 0)
            {
                skillCooldowns[skill.skillName]--;
            }
        }
    }

    private void SetRetreated(bool value)
    {
        isRetreated = value;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.enabled = !value;

        Collider unitCollider = GetComponent<Collider>();
        if (unitCollider != null) unitCollider.enabled = !value;
    }

    private int GetDefenseValue()
    {
        int defense = data != null ? data.def : 0;
        defense += GetStatusAmount(StatusEffectType.DefUp);
        defense -= GetStatusAmount(StatusEffectType.DefDown);
        if (HasTrait("바람타기")) defense += GetStatusAmount(StatusEffectType.Quick);
        return Mathf.Max(0, defense);
    }

    private int CalculateIncomingDamage(int damage)
    {
        int finalDamage = Mathf.Max(0, damage);

        if (ShouldIgnoreIncomingHit(finalDamage)) return 0;
        if (HasTrait("감내")) finalDamage -= GetDefenseValue();
        if (unyieldingProtectionActive) finalDamage -= 25;
        if (HasStatus(StatusEffectType.Shield)) finalDamage -= GetStatusAmount(StatusEffectType.Shield);
        if (HasStatus(StatusEffectType.Protection)) finalDamage -= GetStatusAmount(StatusEffectType.Protection);

        return Mathf.Max(0, finalDamage);
    }

    private void ApplyTraitsOnTurnStart()
    {
        if (HasTrait("신속")) extraMoveAvailable = true;
        if (HasTrait("정신오염")) AddTemporaryStatus(StatusEffectType.MentalProtection, 3);
        if (HasTrait("생체에너지")) AddStatus(StatusEffectType.Charge, 1);
        if (HasTrait("케이론류 심법"))
        {
            currentHp = Mathf.Min(maxHp, currentHp + Mathf.Max(1, Mathf.RoundToInt(maxHp * 0.16f)));
            currentSt = Mathf.Min(maxSt, currentSt + Mathf.Max(1, Mathf.RoundToInt(maxSt * 0.16f)));
        }
        if (HasTrait("절대성"))
        {
            int allyCount = 0;
            foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            {
                if (unit != null && unit.isAlly) allyCount++;
            }

            AddTemporaryStatus(StatusEffectType.AtkUp, Mathf.Max(1, allyCount));
            AddTemporaryStatus(StatusEffectType.Quick, Mathf.Max(1, allyCount));
        }
        if (HasTrait("유동") && GetStatusAmount(StatusEffectType.Breath) > 0 && HandUIManager.Instance != null)
        {
            HandUIManager.Instance.DrawCards(1);
        }
        if (HasTrait("이체동심")) ShareStatusWithLinkedPair();
    }

    private void ApplyTraitsOnTurnEnd()
    {
        if (HasTrait("유동"))
        {
            int quick = GetStatusAmount(StatusEffectType.Quick);
            if (quick > 0) RemoveStatus(StatusEffectType.Quick, Mathf.CeilToInt(quick * 0.5f));
        }

        if (HasTrait("따르는 자")) MoveNextToLinkedPair();
    }

    private void ApplyTraitsOnHit(Enemy attacker)
    {
        if (HasTrait("가드")) AddStatus(StatusEffectType.Shield, GetDefenseValue());
        if (HasTrait("아프터 할짝대기"))
        {
            currentHp = Mathf.Min(maxHp, currentHp + Mathf.Max(1, Mathf.RoundToInt(maxHp * 0.1f)));
            currentSt = Mathf.Min(maxSt, currentSt + Mathf.Max(1, Mathf.RoundToInt(maxSt * 0.1f)));
        }

        if (HasTrait("반격") && attacker != null)
        {
            int counterDamage = Mathf.Max(1, GetDefenseValue());
            attacker.TakeDamage(counterDamage);
            Debug.Log($"{unitName} 반격 발동: {counterDamage} 피해");
        }
    }

    private int ApplyTraitsToAttackDamage(int damage, Enemy enemy)
    {
        int finalDamage = damage;

        if (HasTrait("이름없는 크리처"))
        {
            finalDamage += Mathf.Max(0, maxHp - currentHp);
        }
        if (HasTrait("영웅마창"))
        {
            finalDamage = Mathf.RoundToInt(finalDamage * 1.3f);
        }

        return finalDamage;
    }

    private void ApplyTraitsOnAttackHit(Enemy target)
    {
        if (HasTrait("케이론류 심법"))
        {
            AddStatus(StatusEffectType.Breath, 5);
        }
        if (HasTrait("연격") && target != null)
        {
            target.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(GetAttackPower() * 0.5f)));
            Debug.Log($"{unitName} 연격 발동");
        }
    }

    private void ShareStatusWithLinkedPair()
    {
        Unit pair = FindLinkedPair();
        if (pair == null) return;

        foreach (StatusEffect effect in statusEffects)
        {
            if (effect != null && pair.GetStatusAmount(effect.type) < effect.amount)
            {
                pair.AddStatus(effect.type, effect.amount - pair.GetStatusAmount(effect.type), effect.count);
            }
        }
    }

    private void MoveNextToLinkedPair()
    {
        Unit pair = FindLinkedPair();
        if (pair == null || movement == null || MapManager.Instance == null) return;

        Vector2Int pairPos = new Vector2Int(
            Mathf.RoundToInt(pair.transform.position.x + 3.5f),
            Mathf.RoundToInt(pair.transform.position.y + 3.5f)
        );

        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        foreach (Vector2Int dir in dirs)
        {
            if (!MapManager.Instance.tiles.TryGetValue(pairPos + dir, out Tile tile)) continue;
            if (tile.isOccupied) continue;

            if (movement.currentTile != null)
            {
                movement.currentTile.isOccupied = false;
                movement.currentTile.currentUnit = null;
            }

            transform.position = tile.transform.position;
            movement.currentTile = tile;
            tile.isOccupied = true;
            tile.currentUnit = gameObject;
            Debug.Log($"{unitName} 따르는 자 발동: 연결 기물 주변으로 이동");
            return;
        }
    }

    private Unit FindLinkedPair()
    {
        string targetName = unitName.Contains("마창") ? "왈큐레" : "마창";
        foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit != null && unit != this && unit.unitName.Contains(targetName)) return unit;
        }

        return null;
    }
}
