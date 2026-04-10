using System.Collections.Generic;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    [Header("원본 데이터")]
    public UnitData data;

    [Header("현재 상태")]
    public int currentHp;
    public int currentSt;

    [Header("행동")]
    public int remainingActions = 1;
    public int extraMoveCount = 0;

    [Header("상태이상")]
    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    [Header("덱")]
    public List<CardData> drawDeck = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardDeck = new List<CardData>();

    [Header("스킬 쿨타임")]
    public Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    [Header("특성 관련")]
    public bool hasUsedUnyielding = false;

    [Header("진영")]
    public BattleTeam team;

    // -------------------------
    // 초기화
    // -------------------------
    public void Initialize(UnitData unitData)
    {
        data = unitData;

        currentHp = data.maxHp;
        currentSt = data.maxSt;

        remainingActions = 1;
        extraMoveCount = 0;
        hasUsedUnyielding = false;

        statusEffects.Clear();

        drawDeck.Clear();
        hand.Clear();
        discardDeck.Clear();

        if (data.deck != null)
        {
            drawDeck.AddRange(data.deck);
        }

        ShuffleDeck();

        skillCooldowns.Clear();
        if (data.skills != null)
        {
            foreach (SkillData skill in data.skills)
            {
                if (skill != null && !skillCooldowns.ContainsKey(skill.skillName))
                {
                    skillCooldowns.Add(skill.skillName, 0);
                }
            }
        }

        Debug.Log($"{data.unitName} 초기화 완료 | HP: {currentHp}, ST: {currentSt}");

        OnBattleStart();
    }

    public void OnBattleStart()
    {
        ApplyCommonTraitsOnBattleStart();
        ApplyUniqueTraitsOnBattleStart();
    }

    // -------------------------
    // 턴 시작 / 종료
    // -------------------------
    public void OnTurnStart()
    {
        Debug.Log($"{data.unitName} 턴 시작");

        extraMoveCount = 0;

        ApplyCommonTraitsOnTurnStart();
        ApplyUniqueTraitsOnTurnStart();
        ApplyStatusEffectsOnTurnStart();

        remainingActions = 1 + extraMoveCount;

        DrawCard(1);
        ReduceCooldowns();

        Debug.Log($"{data.unitName} 현재 행동 횟수: {remainingActions}");
    }

    public void OnTurnEnd()
    {
        Debug.Log($"{data.unitName} 턴 종료");

        ApplyCommonTraitsOnTurnEnd();
        ApplyUniqueTraitsOnTurnEnd();
        ApplyStatusEffectsOnTurnEnd();
    }

    // -------------------------
    // 덱
    // -------------------------
    public void ShuffleDeck()
    {
        for (int i = 0; i < drawDeck.Count; i++)
        {
            int randomIndex = Random.Range(i, drawDeck.Count);

            CardData temp = drawDeck[i];
            drawDeck[i] = drawDeck[randomIndex];
            drawDeck[randomIndex] = temp;
        }
    }

    public void DrawCard(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (drawDeck.Count == 0)
            {
                if (discardDeck.Count == 0)
                {
                    Debug.Log($"{data.unitName} - 더 이상 뽑을 카드가 없음");
                    return;
                }

                drawDeck.AddRange(discardDeck);
                discardDeck.Clear();
                ShuffleDeck();
            }

            CardData card = drawDeck[0];

            if (card == null)
            {
                Debug.LogWarning($"{data.unitName}의 덱에 null 카드가 있음");
                return;
            }

            drawDeck.RemoveAt(0);
            hand.Add(card);

            Debug.Log($"{data.unitName} 카드 드로우: {card.cardName}");
        }
    }

    // -------------------------
    // 카드 사용
    // -------------------------
    public bool UseCard(CardData card, BattleUnit target)
    {
        if (remainingActions <= 0)
        {
            Debug.LogWarning($"{data.unitName}은 더 이상 행동할 수 없음");
            return false;
        }

        if (card == null)
        {
            Debug.LogWarning("사용할 카드가 없음");
            return false;
        }

        if (!hand.Contains(card))
        {
            Debug.LogWarning($"{data.unitName}의 손패에 해당 카드가 없음");
            return false;
        }

        if (card.targetType == CardTargetType.Enemy && target == null)
        {
            Debug.LogWarning("적 대상이 없음");
            return false;
        }

        BattleUnit realTarget = card.targetType == CardTargetType.Self ? this : target;

        Debug.Log($"{data.unitName}가 {card.cardName} 사용");

        int damage = CalculateCardDamage(card, realTarget);

        if (card.targetType == CardTargetType.Enemy)
        {
            realTarget.TakeDamage(damage, card.damageType, this);
        }

        ApplyCardEffect(card, realTarget);

        ApplyCommonTraitsOnAttack(realTarget, card);
        ApplyUniqueTraitsOnAttack(realTarget, card);

        hand.Remove(card);
        discardDeck.Add(card);

        remainingActions--;

        Debug.Log($"{data.unitName} 남은 행동 횟수: {remainingActions}");

        return true;
    }

    public int CalculateCardDamage(CardData card, BattleUnit target)
    {
        int atk = Random.Range(data.minAtk, data.maxAtk + 1);
        int finalDamage = atk + card.power;

        int quickBonus = GetStatusAmount(StatusEffectType.Quick);
        finalDamage += quickBonus;

        if (target != null && target.HasKeyword("인간형"))
        {
            int huntBonus = GetStatusAmount(StatusEffectType.HuntHumanType);
            finalDamage += huntBonus;
        }

        int atkUp = GetStatusAmount(StatusEffectType.AtkUp);
        finalDamage += atkUp;

        int damageUp = GetStatusAmount(StatusEffectType.DamageUp);
        finalDamage += damageUp;

        return finalDamage;
    }

    public void ApplyCardEffect(CardData card, BattleUnit target)
    {
        if (!card.useEffect) return;
        if (card.effectType == StatusEffectType.None) return;

        target.AddStatus(card.effectType, card.effectAmount);
        Debug.Log($"{target.data.unitName}에게 {card.effectType} {card.effectAmount} 적용");
    }

    // -------------------------
    // 데미지 / 회복
    // -------------------------
    public void TakeDamage(int damage, DamageType damageType, BattleUnit attacker = null)
    {
        if (ShouldEvadeIncomingAttack(damage))
        {
            Debug.Log($"{data.unitName}의 회피 계열 특성 발동으로 공격 무효");
            return;
        }

        ApplyCommonTraitsOnHit(attacker);
        ApplyUniqueTraitsOnHit(attacker);

        int finalDamage = CalculateFinalDamage(damage, damageType);

        int shieldAmount = GetStatusAmount(StatusEffectType.Shield);
        if (shieldAmount > 0)
        {
            int reducedByShield = Mathf.Min(shieldAmount, finalDamage);
            finalDamage -= reducedByShield;
            RemoveStatus(StatusEffectType.Shield, reducedByShield);

            Debug.Log($"{data.unitName}의 보호막이 {reducedByShield} 피해를 막음");
        }

        if (data.unitName == "헬로 S. 모건" && HasTrait("황금시간-보험"))
        {
            if (currentHp - finalDamage <= 0 && GetStatusAmount(StatusEffectType.GoldenTime) <= 0)
            {
                AddStatus(StatusEffectType.GoldenTime, 1);
                currentHp = 1;
                Debug.Log("모건 황금시간-보험 발동: 황금시간 +1, HP 1로 버팀");
                return;
            }
        }

        if (data.unitName == "제이슨")
        {
            if (currentHp - finalDamage <= 0 && GetStatusAmount(StatusEffectType.Shield) > 0)
            {
                currentHp = 1;
                RemoveStatus(StatusEffectType.Shield, 1);
                Debug.Log("제이슨 생존 보정 발동: HP 1로 버팀");
                return;
            }
        }

        if (HasTrait("불굴") && !hasUsedUnyielding && currentHp - finalDamage <= 0)
        {
            hasUsedUnyielding = true;
            currentHp = 1;
            Debug.Log($"{data.unitName}의 불굴 발동");
            return;
        }

        currentHp -= finalDamage;

        Debug.Log($"{data.unitName}가 {finalDamage} 피해를 받음");
        Debug.Log($"{data.unitName} 현재 HP: {currentHp}");

        if (currentHp <= 0)
        {
            currentHp = 0;

            if (attacker != null)
            {
                attacker.OnKillEnemy(this);
            }

            Die();
        }
    }

    public int CalculateFinalDamage(int damage, DamageType damageType)
    {
        int bonusDef = 0;

        if (HasTrait("바람타기"))
        {
            bonusDef += GetStatusAmount(StatusEffectType.Quick);
        }

        int defUp = GetStatusAmount(StatusEffectType.DefUp);
        bonusDef += defUp;

        int totalDef = data.def + bonusDef;
        int reducedDamage = damage - totalDef;

        if (reducedDamage < 0)
        {
            reducedDamage = 0;
        }

        float resist = GetResistance(damageType);
        int finalDamage = Mathf.RoundToInt(reducedDamage * resist);

        if (finalDamage < 0)
        {
            finalDamage = 0;
        }

        return finalDamage;
    }

    public float GetResistance(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return data.physicalResist;
            case DamageType.Mental:
                return data.mentalResist;
            case DamageType.Special:
                return data.specialResist;
            case DamageType.Sin:
                return data.sinResist;
            default:
                return 1f;
        }
    }

    public void RecoverHp(int amount)
    {
        if (amount <= 0) return;

        currentHp += amount;
        if (currentHp > data.maxHp)
            currentHp = data.maxHp;

        Debug.Log($"{data.unitName} HP {amount} 회복");
    }

    public void RecoverSt(int amount)
    {
        if (amount <= 0) return;

        currentSt += amount;
        if (currentSt > data.maxSt)
            currentSt = data.maxSt;

        Debug.Log($"{data.unitName} ST {amount} 회복");
    }

    public bool IsPanicState()
    {
        return currentSt <= 0;
    }

    public void Die()
    {
        Debug.Log($"{data.unitName} 사망");
        ApplyCommonTraitsOnDeath();
        ApplyUniqueTraitsOnDeath();
        Destroy(gameObject);
    }

    public void OnKillEnemy(BattleUnit deadTarget)
    {
        if (data == null) return;

        switch (data.unitName)
        {
            case "알론소 키하노":
                if (HasTrait("편력기사의 정의"))
                {
                    RecoverSt(10);
                    Debug.Log("알론소 편력기사의 정의 발동: ST 10 회복");
                }
                break;

            case "디프테 라플리":
                RecoverHp(5);
                RecoverSt(5);
                Debug.Log("디프테 라플리 적 처치: HP/ST 5 회복");
                break;
        }
    }

    // -------------------------
    // 상태이상
    // -------------------------
    public void AddStatus(StatusEffectType type, int amount)
    {
        if (type == StatusEffectType.None || amount <= 0) return;

        StatusEffect found = statusEffects.Find(x => x.type == type);

        if (found != null)
        {
            found.amount += amount;
        }
        else
        {
            statusEffects.Add(new StatusEffect(type, amount));
        }
    }

    public void RemoveStatus(StatusEffectType type, int amount)
    {
        StatusEffect found = statusEffects.Find(x => x.type == type);

        if (found == null) return;

        found.amount -= amount;

        if (found.amount <= 0)
        {
            statusEffects.Remove(found);
        }
    }

    public int GetStatusAmount(StatusEffectType type)
    {
        StatusEffect found = statusEffects.Find(x => x.type == type);
        return found != null ? found.amount : 0;
    }

    // -------------------------
    // 키워드 / 특성
    // -------------------------
    public bool HasTrait(string traitName)
    {
        if (data == null || data.traits == null) return false;

        foreach (TraitData trait in data.traits)
        {
            if (trait != null && trait.traitName == traitName)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasKeyword(string keyword)
    {
        if (data == null) return false;

        if (!string.IsNullOrEmpty(data.affiliation) && data.affiliation == keyword)
            return true;

        if (!string.IsNullOrEmpty(data.unitTypeKeyword) && data.unitTypeKeyword == keyword)
            return true;

        if (data.traitKeywords != null && data.traitKeywords.Contains(keyword))
            return true;

        return false;
    }

    public bool ShouldEvadeIncomingAttack(int incomingDamage)
    {
        if (HasTrait("회피"))
        {
            return incomingDamage <= data.def;
        }

        return false;
    }

    // -------------------------
    // 공통 특성 처리
    // -------------------------
    public void ApplyCommonTraitsOnBattleStart()
    {
        if (data == null || data.traits == null) return;

        foreach (TraitData trait in data.traits)
        {
            if (trait == null) continue;

            switch (trait.traitName)
            {
                case "무장":
                    Debug.Log($"{data.unitName} 무장 발동");
                    break;
            }
        }
    }

    public void ApplyCommonTraitsOnTurnStart()
    {
        if (data == null || data.traits == null) return;

        foreach (TraitData trait in data.traits)
        {
            if (trait == null) continue;

            switch (trait.traitName)
            {
                case "신속":
                    extraMoveCount += 1;
                    Debug.Log($"{data.unitName} 신속 발동");
                    break;

                case "생체에너지ø":
                    AddStatus(StatusEffectType.Charge, 1);
                    Debug.Log($"{data.unitName} 생체에너지ø 발동");
                    break;

                case "정신오염":
                    AddStatus(StatusEffectType.Agitation, 4);
                    Debug.Log($"{data.unitName} 정신오염 발동");
                    break;
            }
        }
    }

    public void ApplyCommonTraitsOnTurnEnd()
    {
        // 공통 종료 특성 추가 자리
    }

    public void ApplyCommonTraitsOnAttack(BattleUnit target, CardData card)
    {
        if (data == null || data.traits == null) return;

        foreach (TraitData trait in data.traits)
        {
            if (trait == null) continue;

            switch (trait.traitName)
            {
                case "생체에너지ø":
                    if (GetStatusAmount(StatusEffectType.Charge) < 30)
                    {
                        AddStatus(StatusEffectType.Charge, 1);
                        Debug.Log($"{data.unitName} 공격 후 충전 +1");
                    }
                    break;
            }
        }
    }

    public void ApplyCommonTraitsOnHit(BattleUnit attacker)
    {
        if (data == null || data.traits == null) return;

        foreach (TraitData trait in data.traits)
        {
            if (trait == null) continue;

            switch (trait.traitName)
            {
                case "가드":
                    AddStatus(StatusEffectType.Shield, data.def);
                    Debug.Log($"{data.unitName} 가드 발동: 보호막 +{data.def}");
                    break;
            }
        }
    }

    public void ApplyCommonTraitsOnDeath()
    {
        if (data == null || data.traits == null) return;

        foreach (TraitData trait in data.traits)
        {
            if (trait == null) continue;

            switch (trait.traitName)
            {
                case "오염":
                    Debug.Log($"{data.unitName} 오염 발동");
                    break;
            }
        }
    }

    // -------------------------
    // 상태이상 턴 처리
    // -------------------------
    public void ApplyStatusEffectsOnTurnStart()
    {
        int breath = GetStatusAmount(StatusEffectType.Breath);

        if (breath > 0)
        {
            currentSt += breath;

            if (currentSt > data.maxSt)
            {
                currentSt = data.maxSt;
            }

            Debug.Log($"{data.unitName} 호흡 효과로 ST {breath} 회복");
        }
    }

    public void ApplyStatusEffectsOnTurnEnd()
    {
        int quick = GetStatusAmount(StatusEffectType.Quick);

        if (quick > 0)
        {
            if (HasTrait("유동"))
            {
                int remainQuick = Mathf.FloorToInt(quick * 0.5f);

                if (remainQuick <= 0)
                {
                    statusEffects.RemoveAll(x => x.type == StatusEffectType.Quick);
                }
                else
                {
                    StatusEffect found = statusEffects.Find(x => x.type == StatusEffectType.Quick);
                    if (found != null)
                    {
                        found.amount = remainQuick;
                    }
                }

                Debug.Log($"{data.unitName} 유동 발동: 재빠름 50% 유지");
            }
            else
            {
                statusEffects.RemoveAll(x => x.type == StatusEffectType.Quick);
            }
        }
    }

    public void ReduceCooldowns()
    {
        List<string> keys = new List<string>(skillCooldowns.Keys);

        foreach (string key in keys)
        {
            if (skillCooldowns[key] > 0)
            {
                skillCooldowns[key]--;
            }
        }
    }

    // -------------------------
    // 캐릭터별 고유 특성 훅
    // -------------------------
    public void ApplyUniqueTraitsOnBattleStart()
    {
        if (data == null) return;

        switch (data.unitName)
        {
            case "가헤리스":
                if (HasTrait("무장"))
                {
                    AddStatus(StatusEffectType.Shield, 1);
                    Debug.Log("가헤리스 무장 발동: 보호막 1 부여");
                }
                break;

            case "알론소 키하노":
                // 출격 시 별도 처리 필요하면 여기에 추가
                break;
        }
    }

    public void ApplyUniqueTraitsOnTurnStart()
    {
        if (data == null) return;

        switch (data.unitName)
        {
            case "가헤리스":
                if (HasTrait("유동"))
                {
                    AddStatus(StatusEffectType.Breath, 1);
                    DrawCard(1);
                    Debug.Log("가헤리스 유동 추가 효과 발동");
                }
                break;

            case "알론소 키하노":
                if (HasTrait("라만차의 사랑스럽고 정의로운 소녀영웅 돈키호테의 찬란하고 위대한 이야기를 들어보게나!!! 시리즈의 주인공"))
                {
                    if (GetStatusAmount(StatusEffectType.Charge) < 20)
                    {
                        AddStatus(StatusEffectType.Charge, 1);
                        Debug.Log("알론소 턴 시작: 충전 +1");
                    }
                    else
                    {
                        AddStatus(StatusEffectType.Shield, 16);
                        Debug.Log("알론소 턴 시작: 충전 최대라 보호막 +16");
                    }
                }

                if (HasTrait("환상 속 영웅"))
                {
                    if (GetStatusAmount(StatusEffectType.Hallucination) > 0)
                    {
                        AddStatus(StatusEffectType.Frenzy, 100);
                        Debug.Log("알론소 환상 속 영웅 발동: 폭주 +100");
                    }
                }
                break;

            case "헬로 S. 모건":
                // 황금시간 관련 시작 처리 자리
                break;

            case "디프테 라플리":
                AddStatus(StatusEffectType.Prey, 1);
                Debug.Log("디프테 라플리 턴 시작: Prey +1");
                break;

            case "윌리엄 T. 주니어":
                AddStatus(StatusEffectType.Logic, 1);
                Debug.Log("윌리엄 T. 주니어 턴 시작: Logic +1");
                AddStatus(StatusEffectType.DefUp, 2);
                Debug.Log("윌리엄 T. 주니어 턴 시작: DEF 증가 +2");
                break;

            case "제이슨":
                if (HasTrait("로망") || HasTrait("바다의 낭만") || HasTrait("모험가의 낭만"))
                {
                    AddStatus(StatusEffectType.DamageUp, 1);
                    Debug.Log("제이슨 고유 효과: 피해량 증가 +1");
                }
                break;

            case "우커 엑스 터러":
                if (HasTrait("용맹의 열기") || HasTrait("용맹의 뭔가"))
                {
                    Debug.Log("우커 엑스 터러 고유 효과: 포인트 생성/지형 효과는 2차 구현 예정");
                }

                if (HasTrait("용맹의 믿기") || HasTrait("응징"))
                {
                    AddStatus(StatusEffectType.DamageUp, 1);
                    Debug.Log("우커 엑스 터러 턴 시작: 피해량 증가 +1");
                }
                break;

            case "바지루 리아":
                if (IsPanicState())
                {
                    RecoverSt(5);
                    Debug.Log("바지루 리아 고유 효과: 패닉 상태 보정으로 ST 5 회복");
                }

                AddStatus(StatusEffectType.Shield, 3);
                Debug.Log("바지루 리아 턴 시작: 보호막 +3");
                break;

            case "길동":
                int randomBonus = Random.Range(0, 3);
                switch (randomBonus)
                {
                    case 0:
                        AddStatus(StatusEffectType.Quick, 1);
                        Debug.Log("길동 턴 시작: 재빠름 +1");
                        break;
                    case 1:
                        AddStatus(StatusEffectType.DefUp, 1);
                        Debug.Log("길동 턴 시작: DEF 증가 +1");
                        break;
                    case 2:
                        AddStatus(StatusEffectType.DamageUp, 1);
                        Debug.Log("길동 턴 시작: 피해량 증가 +1");
                        break;
                }
                break;

            case "왈큐레&마창":
                AddStatus(StatusEffectType.AtkUp, 1);
                Debug.Log("왈큐레&마창 턴 시작: ATK 증가 +1");
                break;

            case "소악":
                AddStatus(StatusEffectType.DamageUp, 2);
                Debug.Log("소악 턴 시작: 피해량 증가 +2");
                currentHp -= 1;
                if (currentHp < 1) currentHp = 1;
                Debug.Log("소악 턴 시작: 반동으로 HP 1 감소");
                break;

            case "제로":
                // 필요하면 추가
                break;
        }
    }

    public void ApplyUniqueTraitsOnTurnEnd()
    {
        if (data == null) return;

        switch (data.unitName)
        {
            case "헬로 S. 모건":
                if (HasTrait("자네의 시간을 보증하지"))
                {
                    if (GetStatusAmount(StatusEffectType.GoldenTime) >= 3)
                    {
                        RemoveStatus(StatusEffectType.GoldenTime, 3);
                        AddStatus(StatusEffectType.GoldenGuarantee, 1);
                        Debug.Log("모건 자네의 시간을 보증하지 발동: 황금시간 3 소모, 보증 +1");
                    }
                }
                break;

            case "바지루 리아":
                RemoveStatus(StatusEffectType.Agitation, 1);
                RemoveStatus(StatusEffectType.Hallucination, 1);
                Debug.Log("바지루 리아 턴 종료: 동요/환각 감소");
                break;

            case "윌리엄 T. 주니어":
                if (GetStatusAmount(StatusEffectType.Logic) >= 3)
                {
                    AddStatus(StatusEffectType.DamageUp, 2);
                    Debug.Log("윌리엄 T. 주니어 턴 종료: Logic 누적으로 피해량 증가 +2");
                }
                break;

            case "길동":
                if (hand.Count <= 1)
                {
                    DrawCard(1);
                    Debug.Log("길동 턴 종료: 손패 부족으로 카드 1장 추가 드로우");
                }
                break;

            case "왈큐레&마창":
                if (GetStatusAmount(StatusEffectType.Mark) > 0)
                {
                    AddStatus(StatusEffectType.DamageUp, 1);
                    Debug.Log("왈큐레&마창 턴 종료: 표식 연계로 피해량 증가 +1");
                }
                break;

            case "소악":
                if (GetStatusAmount(StatusEffectType.Frenzy) > 0)
                {
                    RecoverHp(1);
                    Debug.Log("소악 턴 종료: 폭주 상태 보정으로 HP 1 회복");
                }
                break;

            case "제로":
                // 광전사의 혼 등 종료 처리 자리
                break;
        }
    }

    public void ApplyUniqueTraitsOnAttack(BattleUnit target, CardData card)
    {
        if (data == null) return;

        switch (data.unitName)
        {
            case "제로":
                if (HasTrait("생체에너지ø"))
                {
                    if (GetStatusAmount(StatusEffectType.Charge) < 20)
                    {
                        AddStatus(StatusEffectType.Charge, 1);
                        Debug.Log("제로 고유 효과: 공격 후 충전 +1");
                    }
                }
                break;

            case "알론소 키하노":
                if (HasTrait("부서지는 별") && target != null)
                {
                    target.AddStatus(StatusEffectType.Rupture, 2);
                    Debug.Log("알론소 부서지는 별 발동: 대상에게 파열 +2");
                }
                break;

            case "우커 엑스 터러":
                if (target != null)
                {
                    target.AddStatus(StatusEffectType.Hallucination, 1);
                    Debug.Log("우커 엑스 터러 고유 효과: 대상에게 환각 +1");
                }
                break;

            case "바지루 리아":
                if (target != null)
                {
                    target.AddStatus(StatusEffectType.Slow, 1);
                    Debug.Log("바지루 리아 공격 효과: 대상에게 느릿 +1");
                }
                break;

            case "디프테 라플리":
                if (target != null)
                {
                    int repeatCount = Random.Range(3, 8);

                    for (int i = 0; i < repeatCount; i++)
                    {
                        int randomEffect = Random.Range(0, 3);

                        switch (randomEffect)
                        {
                            case 0:
                                target.AddStatus(StatusEffectType.Corrosion, 1);
                                break;
                            case 1:
                                target.AddStatus(StatusEffectType.Poison, 1);
                                break;
                            case 2:
                                target.AddStatus(StatusEffectType.Fear, 1);
                                break;
                        }
                    }

                    Debug.Log($"디프테 라플리 공격 효과: 상태이상 {repeatCount}회 부여");
                }
                break;

            case "윌리엄 T. 주니어":
                if (target != null)
                {
                    target.AddStatus(StatusEffectType.Mark, 1);
                    Debug.Log("윌리엄 T. 주니어 공격 효과: 대상에게 표식 +1");
                }
                break;

            case "길동":
                if (target != null)
                {
                    target.AddStatus(StatusEffectType.Mark, 1);
                    Debug.Log("길동 공격 효과: 대상에게 표식 +1");
                }
                break;

            case "왈큐레&마창":
                if (target != null)
                {
                    target.AddStatus(StatusEffectType.Mark, 2);
                    Debug.Log("왈큐레&마창 공격 효과: 대상에게 표식 +2");

                    if (target.HasKeyword("인간형"))
                    {
                        target.TakeDamage(3, DamageType.Physical, this);
                        Debug.Log("왈큐레&마창 추가 효과: 인간형 대상 추가 물리 피해 3");
                    }
                }
                break;

            case "소악":
                if (target != null)
                {
                    target.AddStatus(StatusEffectType.Fear, 1);
                    Debug.Log("소악 공격 효과: 대상에게 공포 +1");
                }
                break;
        }
    }

    public void ApplyUniqueTraitsOnHit(BattleUnit attacker)
    {
        if (data == null) return;

        switch (data.unitName)
        {
            case "제이슨":
                if (attacker != null)
                {
                    int myStatSum = data.minAtk + data.def;
                    int attackerStatSum = attacker.data.minAtk + attacker.data.def;

                    if (attackerStatSum < myStatSum)
                    {
                        int stDamage = myStatSum - attackerStatSum;
                        attacker.currentSt -= stDamage;
                        if (attacker.currentSt < 0) attacker.currentSt = 0;

                        Debug.Log($"제이슨 방어 특성 발동: {attacker.data.unitName}에게 ST {stDamage} 피해");
                    }
                }
                break;

            case "우커 엑스 터러":
                if (HasTrait("반격") && attacker != null)
                {
                    int counterDamage = data.def;
                    attacker.TakeDamage(counterDamage, DamageType.Physical, this);
                    Debug.Log($"우커 엑스 터러 반격 발동: {attacker.data.unitName}에게 {counterDamage} 피해");
                }
                break;

            case "헬로 S. 모건":
                if (HasTrait("반격") && attacker != null)
                {
                    Debug.Log("모건 반격 발동 준비");
                }
                break;

            case "디프테 라플리":
                AddStatus(StatusEffectType.Shield, data.def + 3);
                Debug.Log("디프테 라플리 피격 반응: 보호막 생성");
                break;

            case "윌리엄 T. 주니어":
                if (attacker != null)
                {
                    AddStatus(StatusEffectType.Shield, 2);
                    Debug.Log("윌리엄 T. 주니어 피격 반응: 보호막 +2");
                }
                break;

            case "길동":
                AddStatus(StatusEffectType.Shield, 1);
                Debug.Log("길동 피격 반응: 보호막 +1");
                break;

            case "왈큐레&마창":
                AddStatus(StatusEffectType.Shield, 2);
                Debug.Log("왈큐레&마창 피격 반응: 보호막 +2");
                break;

            case "소악":
                AddStatus(StatusEffectType.Frenzy, 1);
                Debug.Log("소악 피격 반응: 폭주 +1");
                break;
        }
    }

    public void ApplyUniqueTraitsOnDeath()
    {
        if (data == null) return;

        switch (data.unitName)
        {
            case "헬로 S. 모건":
                // 황금시간 관련 사망 처리 확장 자리
                break;
        }
    }

    public int RollSpeed()
    {
        if (data == null) return 0;

        int minSpeed = data.minSp;
        int maxSpeed = data.maxSp;

        int quickBonus = GetStatusAmount(StatusEffectType.Quick);
        int slowPenalty = GetStatusAmount(StatusEffectType.Slow);

        minSpeed += quickBonus;
        maxSpeed += quickBonus;

        minSpeed -= slowPenalty;
        maxSpeed -= slowPenalty;

        if (minSpeed < 0) minSpeed = 0;
        if (maxSpeed < minSpeed) maxSpeed = minSpeed;

        return Random.Range(minSpeed, maxSpeed + 1);
    }

    public bool IsDead()
    {
        return currentHp <= 0;
    }
}