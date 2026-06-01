// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Board/ 신규 추가
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Enemy : MonoBehaviour
{
    public EnemyData EnemyData;

    public int CurrentHp;
    public int CurrentSt;
    public int Sp;
    public int damage; // 현재 ATK (버프 포함)

    public bool isGroggy = false;
    private bool hasUsedGrogyEscape = false;

    public int wetStacks = 0;
    public int burnStacks = 0;
    public int manaStacks = 0; // 용의 마력 스킬로 쌓이는 마나 (거센 불길 트리거)
    public int shieldHp = 0;   // 용의 비늘 — 턴 시작 보호막

    // 바다의 재앙 — 바다 포인트 체류 시 활성화 (매 턴 리셋)
    public bool hasSwiftnessBuff = false;
    public bool hasSpreadBuff = false;
    public bool hasCorrosionBuff = false; // 혓바닥휘두르기 — 이번 턴 공격 적중 시 부식 부여

    // 보스 / 엘리트 HP 씬 간 유지
    private static readonly Dictionary<string, int> persistentBossHp = new Dictionary<string, int>();

    private void Awake()
    {
        if (EnemyData != null)
        {
            Sp = Random.Range(EnemyData.minSp, EnemyData.maxSp);
            gameObject.name = EnemyData.unitName;
        }
        if (EnemyData != null)
        {
            damage = Random.Range(EnemyData.minatk, EnemyData.maxatk);
            gameObject.name = EnemyData.unitName;
        }
    }

    private void Start()
    {
        if (EnemyData != null)
        {
            if (StageManager.SelectedStage.battleType == BattleType.Normal)
            {
                CurrentHp = 1;
                shieldHp = 0;
            }
            else if (persistentBossHp.TryGetValue(EnemyData.unitName, out int savedHp))
                CurrentHp = savedHp;
            else if (HasTrait(TraitEffect.elite))
                CurrentHp = Mathf.Max(1, Mathf.RoundToInt(EnemyData.maxHp * 0.5f));
            else
                CurrentHp = EnemyData.maxHp;

            CurrentSt = EnemyData.maxSt;
            damage = EnemyData.maxatk;
        }

        // 자바무너: 출격 시 1프레임 후 다리 8개 소환
        if (HasTrait(TraitEffect.javaSpawn))
            StartCoroutine(SpawnLegsDelayed());
    }

    private IEnumerator SpawnLegsDelayed()
    {
        yield return null;
        if (EnemySpawnManager.Instance == null) yield break;
        for (int i = 0; i < 8; i++)
            EnemySpawnManager.Instance.SpawnEnemy("자바문어의 다리");
        Debug.Log($"[자바무너] {EnemyData?.unitName} 다리 8개 소환");
    }

    private void OnDestroy()
    {
        if (EnemyData == null) return;
        if (HasTrait(TraitEffect.boss) || HasTrait(TraitEffect.elite))
        {
            if (CurrentHp > 0)
                persistentBossHp[EnemyData.unitName] = CurrentHp;
            else
                persistentBossHp.Remove(EnemyData.unitName);
        }
    }

    // ============================
    // 피해 / ST
    // ============================

    // 공격자 없이 호출되는 경우 (반격 피해, 디버그 등)
    public void TakeDamage(int dmg) => TakeDamage(dmg, null);

    // 플레이어 Unit이 공격할 때: 방어 특성 적용 후 피해 처리
    public void TakeDamage(int dmg, Unit attacker)
    {
        dmg = ApplyEnemyDefenses(dmg, attacker);
        if (dmg <= 0) return;

        CurrentHp -= dmg;
        Debug.Log($"{EnemyData.unitName} HP: {CurrentHp}/{EnemyData.maxHp} (-{dmg})");
        if (CurrentHp <= 0)
        {
            foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if (e != this) e.OnAllyDied();
            Debug.Log($"{EnemyData.unitName} 사망");
            Destroy(gameObject);
        }
    }

    // 방어 특성 계산 — 차단 시 0 반환, 통과 시 최종 피해량 반환
    private int ApplyEnemyDefenses(int dmg, Unit attacker)
    {
        int def = EnemyData?.maxdef ?? 0;

        // 회피: 자신의 DEF 이하 공격 차단
        if (HasTrait(TraitEffect.avoidance) && dmg <= def)
        {
            Debug.Log($"{EnemyData?.unitName} [회피] 피해 차단 ({dmg} ≤ DEF {def})");
            return 0;
        }

        // 공중곡예: 회피 취급 + Sp 4이하 공격 면역
        if (HasTrait(TraitEffect.aerialAcrobatics))
        {
            int attackerSp = attacker?.stats?.currentTurnSpeed ?? 999;
            if (dmg <= def || attackerSp <= 4)
            {
                Debug.Log($"{EnemyData?.unitName} [공중곡예] 피해 차단 (Sp:{attackerSp}, dmg:{dmg}, def:{def})");
                return 0;
            }
        }

        // 패마: 자신보다 ATK+DEF 합이 낮은 공격 차단, 차이만큼 ST 반격
        if (HasTrait(TraitEffect.parry) && attacker != null && EnemyData != null)
        {
            int attackerTotal = (attacker.stats?.baseAttack ?? 0) + (attacker.data?.def ?? 0);
            int selfTotal = damage + def;
            if (attackerTotal < selfTotal)
            {
                int stDmg = selfTotal - attackerTotal;
                attacker.currentSt = Mathf.Max(0, attacker.currentSt - stDmg);
                Debug.Log($"{EnemyData.unitName} [패마] {attacker.unitName} 공격 차단, ST -{stDmg}");
                return 0;
            }
        }

        // 용의 비늘 보호막 흡수
        if (shieldHp > 0)
        {
            int absorbed = Mathf.Min(shieldHp, dmg);
            shieldHp -= absorbed;
            dmg -= absorbed;
            Debug.Log($"{EnemyData?.unitName} 보호막 {absorbed} 흡수 (남은량: {shieldHp})");
            if (dmg <= 0) return 0;
        }

        return dmg;
    }

    public void AddBurn(int amount)
    {
        burnStacks += amount;
        Debug.Log($"{EnemyData?.unitName ?? name} 화상 +{amount} (총 {burnStacks})");
    }

    public void OnAllyDied()
    {
        if (!HasTrait(TraitEffect.struggling)) return;
        float resist = EnemyData != null ? EnemyData.mentalResist : 1f;
        int dmg = Mathf.Max(1, Mathf.RoundToInt(5f / resist));
        CurrentHp -= dmg;
        damage += 1;
        Debug.Log($"{EnemyData?.unitName} [생존발악] 아군 사망 — 정신 피해 {dmg}, ATK +1 (현재 {damage})");
        if (CurrentHp <= 0)
        {
            foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if (e != this) e.OnAllyDied();
            Destroy(gameObject);
        }
    }

    public void TakeStaggerDamage(int amount)
    {
        if (HasTrait(TraitEffect.machineSpirit)) return;  // 기계정신: ST/패닉 없음
        CurrentSt -= amount;
        if (CurrentSt <= 0 && !isGroggy)
            TryEnterGroggy();
    }

    public void RecoverSt(int amount)
    {
        if (EnemyData == null) return;
        CurrentSt = Mathf.Min(CurrentSt + amount, EnemyData.maxSt);
    }

    // ============================
    // 그로기
    // ============================
    private void TryEnterGroggy()
    {
        if (!hasUsedGrogyEscape && HasTrait(TraitEffect.grogyEscape))
        {
            hasUsedGrogyEscape = true;
            CurrentSt = EnemyData.maxSt;
            Debug.Log($"{EnemyData.unitName} [불허]: 그로기 무시, ST 전량 회복");
            return;
        }
        isGroggy = true;
        CurrentSt = 0;
        Debug.Log($"{EnemyData.unitName} 그로기 상태 돌입!");
    }

    public void RecoverFromGroggy()
    {
        if (!isGroggy) return;
        isGroggy = false;
        CurrentSt = EnemyData.maxSt;
        Debug.Log($"{EnemyData.unitName} 그로기 해제");
    }

    // ============================
    // 특성 처리
    // ============================
    public bool HasTrait(TraitEffect effect)
    {
        if (EnemyData == null || EnemyData.traits == null) return false;
        foreach (var trait in EnemyData.traits)
            if (trait != null && trait.traitEffect == effect) return true;
        return false;
    }

    // ============================
    // 젖음
    // ============================
    public void ApplyWet(int stacks)
    {
        // 범람하는 바다의 재앙: 젖음 완전 면역
        if (HasTrait(TraitEffect.floodingSeaDisaster)) return;
        int actual = HasTrait(TraitEffect.seaDisaster)
            ? Mathf.Max(1, Mathf.RoundToInt(stacks * 0.5f))
            : stacks;
        wetStacks += actual;
        Debug.Log($"{EnemyData.unitName} 젖음 +{actual} (총 {wetStacks})");
    }

    // 턴 시작 시 TurnManager가 호출
    public void OnTurnStart()
    {
        hasSwiftnessBuff = false;
        hasSpreadBuff = false;
        hasCorrosionBuff = false;
        shieldHp = 0;  // 보호막은 매 턴 초기화 후 드래곤 비늘이 재부여

        if (EnemyData == null || EnemyData.traits == null) return;
        foreach (var trait in EnemyData.traits)
        {
            if (trait == null) continue;
            switch (trait.traitEffect)
            {
                case TraitEffect.heroOfTribe:
                    ApplyHeroOfTribe(trait);
                    break;
                case TraitEffect.clearyourmind:
                    RecoverSt(trait.stAmount);
                    Debug.Log($"{EnemyData.unitName} [정신 가다듬기] ST +{trait.stAmount}");
                    break;
                case TraitEffect.seaDisaster:
                    ApplySeaDisasterBuff();
                    break;
                case TraitEffect.dragonScale:
                    shieldHp += 40;
                    Debug.Log($"{EnemyData.unitName} [용의 비늘] 보호막 +40 (현재 {shieldHp})");
                    break;
                case TraitEffect.giantKing:
                    ApplyGiantKing();
                    break;
                case TraitEffect.ragingFlame:
                    if (manaStacks >= 10)
                    {
                        manaStacks -= 10;
                        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
                            unit.AddStatus(StatusEffectType.Burn, 3);
                        Debug.Log($"{EnemyData.unitName} [거센 불길] 마나 10 소모 → 아군 전체 화상(3) 부여");
                    }
                    break;
            }
        }
    }

    // 턴 종료 시 TurnManager가 호출
    public void OnTurnEnd()
    {
        if (EnemyData == null || EnemyData.traits == null) return;
        foreach (var trait in EnemyData.traits)
        {
            if (trait == null) continue;
            switch (trait.traitEffect)
            {
                case TraitEffect.callOfTribe:
                    ApplyCallOfTribe(trait);
                    break;
                case TraitEffect.floodingSeaDisaster:
                    ExpandSeaTiles();
                    break;
            }
        }
    }

    private void ApplyHeroOfTribe(TraitData trait)
    {
        List<Enemy> allies = new List<Enemy>();
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e.EnemyData != null && e.EnemyData.affiliation == trait.affiliationTarget)
                allies.Add(e);

        foreach (var ally in allies)
            ally.RecoverSt(trait.stAmount);
        damage = EnemyData.maxatk + allies.Count * 2;
        Debug.Log($"{EnemyData.unitName} [바다민족의 영웅] 아군 {allies.Count}명, ATK={damage}, 아군 ST+{trait.stAmount}");
    }

    private void ApplyCallOfTribe(TraitData trait)
    {
        if (EnemySpawnManager.Instance == null) return;

        int cowardCount = CountEnemiesByName("겁쟁이 바다민족");
        if (cowardCount == 0 && CurrentSt >= 4)
        {
            EnemySpawnManager.Instance.SpawnEnemy("겁쟁이 바다민족");
            CurrentSt -= 4;
            Debug.Log($"{EnemyData.unitName} [민족의 부름] 겁쟁이 바다민족 소환, St-4");
        }

        int wildCount = CountEnemiesByName("야만적인 바다민족");
        int calmCount = CountEnemiesByName("냉정한 바다민족");
        int total = wildCount + calmCount;

        if (total < 5)
        {
            int needed = 5 - total;
            int summoned = 0;
            for (int i = 0; i < needed; i++)
            {
                if (CurrentSt < 5) break;
                string target = Random.Range(0, 2) == 0 ? "야만적인 바다민족" : "냉정한 바다민족";
                EnemySpawnManager.Instance.SpawnEnemy(target);
                CurrentSt -= 5;
                summoned++;
            }
            if (summoned > 0)
                Debug.Log($"{EnemyData.unitName} [민족의 부름] {summoned}명 소환, St-{summoned * 5}");
        }
    }

    private int CountEnemiesByName(string enemyName)
    {
        int count = 0;
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e.EnemyData != null && e.EnemyData.unitName == enemyName) count++;
        return count;
    }

    private void ApplySeaDisasterBuff()
    {
        if (HasTrait(TraitEffect.flight)) return; // 비행: 포인트 효과 무시
        EnemyUnit unit = GetComponent<EnemyUnit>();
        if (unit == null) return;

        if (MapManager.Instance == null) return;
        if (!MapManager.Instance.tiles.TryGetValue(unit.gridPosition, out Tile tile)) return;
        if (!tile.CompareTag("SeaPoint")) return;

        unit.ApplyBuff(new LegacyEnemyBuff(10, 1));
        hasSwiftnessBuff = true;
        hasSpreadBuff = true;
        Debug.Log($"{EnemyData.unitName} [바다의 재앙] 바다 포인트 — ATK+10, 재빠름, 확산 활성화");
    }

    // 거인왕: 아군 거인 ATK+3, 자신 ATK += 생존 거인 수 × 3
    private void ApplyGiantKing()
    {
        int giantCount = 0;
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e == this || e.EnemyData == null) continue;
            bool isGiant = e.EnemyData.affiliation == "거인연맹"
                           || (e.EnemyData.traitKeywords != null
                               && e.EnemyData.traitKeywords.Contains("거인"));
            if (!isGiant) continue;
            e.damage += 3;
            giantCount++;
        }
        damage = EnemyData.maxatk + giantCount * 3;
        Debug.Log($"{EnemyData.unitName} [거인왕] 아군 거인 {giantCount}명 ATK+3, 자신 ATK+{giantCount * 3}");
    }

    // 범람하는 바다의 재앙: 턴 종료 시 주변 포인트를 바다 포인트로 확장
    private void ExpandSeaTiles()
    {
        if (MapManager.Instance == null) return;
        EnemyUnit unit = GetComponent<EnemyUnit>();
        if (unit == null) return;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int expanded = 0;
        foreach (var dir in dirs)
        {
            if (MapManager.Instance.tiles.TryGetValue(unit.gridPosition + dir, out Tile tile))
            {
                tile.gameObject.tag = "SeaPoint";
                expanded++;
            }
        }
        Debug.Log($"{EnemyData.unitName} [범람하는 바다의 재앙] 주변 {expanded}칸 → 바다 포인트");
    }

    // 디버그: 스페이스바로 즉사
    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            TakeDamage(CurrentHp);
    }
}
