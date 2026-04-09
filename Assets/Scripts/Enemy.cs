using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Enemy : MonoBehaviour
{
    public EnemyData EnemyData;

    public int CurrentHp;
    public int CurrentSt;
    public int Sp;
    public int damage;

    // 그로기 상태
    public bool isGroggy = false;
    private bool hasUsedGrogyEscape = false;

    // 젖음 스택
    public int wetStacks = 0;

    // 바다의 재앙 — 바다 포인트 체류 시 활성화되는 일시 버프 (매 턴 리셋)
    public bool hasSwiftnessBuff = false;
    public bool hasSpreadBuff = false;

    // 보스 / 엘리트 HP 씬 간 유지
    private static readonly Dictionary<string, int> persistentBossHp = new Dictionary<string, int>();

    private void Awake()
    {
        if (EnemyData != null)
        {
            Sp = Random.Range(EnemyData.minSp, EnemyData.maxSp);
            gameObject.name = EnemyData.EnemyName;
        }
    }

    private void Start()
    {
        if (EnemyData != null)
        {
            if (persistentBossHp.TryGetValue(EnemyData.EnemyName, out int savedHp))
            {
                // 보스/엘리트: 이전 전투 HP 불러오기
                CurrentHp = savedHp;
            }
            else if (HasTrait(TraitEffect.elite))
            {
                // 엘리트: 초기 HP = maxHp × 50%
                CurrentHp = Mathf.Max(1, Mathf.RoundToInt(EnemyData.maxHp * 0.5f));
            }
            else
            {
                CurrentHp = EnemyData.maxHp;
            }

            CurrentSt = EnemyData.maxSt;
            damage = EnemyData.ATK;
        }
    }

    private void OnDestroy()
    {
        if (EnemyData == null) return;
        // 보스/엘리트: HP 유지
        if (HasTrait(TraitEffect.boss) || HasTrait(TraitEffect.elite))
        {
            if (CurrentHp > 0)
                persistentBossHp[EnemyData.EnemyName] = CurrentHp;
            else
                persistentBossHp.Remove(EnemyData.EnemyName);
        }
    }

    // ============================
    // 피해 / ST
    // ============================
    public void TakeDamage(int dmg)
    {
        CurrentHp -= dmg;
        Debug.Log($"{EnemyData.EnemyName} HP: {CurrentHp}/{EnemyData.maxHp} (-{dmg})");
        if (CurrentHp <= 0)
        {
            Debug.Log($"{EnemyData.EnemyName} 사망");
            Destroy(gameObject);
        }
    }

    public void TakeStaggerDamage(int amount)
    {
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
            Debug.Log($"{EnemyData.EnemyName} [불허]: 그로기 무시, ST 전량 회복");
            return;
        }

        isGroggy = true;
        CurrentSt = 0;
        Debug.Log($"{EnemyData.EnemyName} 그로기 상태 돌입!");
    }

    public void RecoverFromGroggy()
    {
        if (!isGroggy) return;
        isGroggy = false;
        CurrentSt = EnemyData.maxSt;
        Debug.Log($"{EnemyData.EnemyName} 그로기 해제");
    }

    // ============================
    // 특성 처리
    // ============================
    public bool HasTrait(TraitEffect effect)
    {
        if (EnemyData == null || EnemyData.traitData == null) return false;
        foreach (var trait in EnemyData.traitData)
            if (trait != null && trait.traitEffect == effect) return true;
        return false;
    }

    // ============================
    // 젖음
    // ============================
    public void ApplyWet(int stacks)
    {
        // 바다의 재앙: 젖음 효과 50% 감소
        int actual = HasTrait(TraitEffect.seaDisaster)
            ? Mathf.Max(1, Mathf.RoundToInt(stacks * 0.5f))
            : stacks;
        wetStacks += actual;
        Debug.Log($"{EnemyData.EnemyName} 젖음 +{actual} (총 {wetStacks})");
    }

    // 턴 시작 시 TurnManager가 호출
    public void OnTurnStart()
    {
        // 일시 버프 리셋
        hasSwiftnessBuff = false;
        hasSpreadBuff = false;

        if (EnemyData == null || EnemyData.traitData == null) return;
        foreach (var trait in EnemyData.traitData)
        {
            if (trait == null) continue;
            switch (trait.traitEffect)
            {
                case TraitEffect.heroOfTribe:
                    ApplyHeroOfTribe(trait);
                    break;
                case TraitEffect.clearyourmind:
                    RecoverSt(trait.stAmount);
                    Debug.Log($"{EnemyData.EnemyName} [정신 가다듬기] ST +{trait.stAmount}");
                    break;
                case TraitEffect.seaDisaster:
                    ApplySeaDisasterBuff();
                    break;
            }
        }
    }

    // 턴 종료 시 TurnManager가 호출
    public void OnTurnEnd()
    {
        if (EnemyData == null || EnemyData.traitData == null) return;
        foreach (var trait in EnemyData.traitData)
        {
            if (trait == null) continue;
            if (trait.traitEffect == TraitEffect.callOfTribe)
                ApplyCallOfTribe(trait);
        }
    }

    private void ApplyHeroOfTribe(TraitData trait)
    {
        if (GridManager.Instance == null) return;

        List<Enemy> allies = GridManager.Instance.GetEnemiesByAffiliation(trait.affiliationTarget);

        // 모든 바다민족 ST 회복
        foreach (var ally in allies)
            ally.RecoverSt(trait.stAmount);

        // 자신 ATK = 기본 ATK + 생존 아군 수(자신 포함) × 2
        damage = EnemyData.ATK + allies.Count * 2;
        Debug.Log($"{EnemyData.EnemyName} [바다민족의 영웅] 아군 {allies.Count}명, ATK={damage}, 아군 ST+{trait.stAmount}");
    }

    private void ApplyCallOfTribe(TraitData trait)
    {
        if (EnemySpawnManager.Instance == null || GridManager.Instance == null) return;

        // 조건 1: 겁쟁이 바다민족이 없으면 1명 소환 (ST 4 소모)
        int cowardCount = GridManager.Instance.CountEnemiesByName("겁쟁이 바다민족");
        if (cowardCount == 0 && CurrentSt >= 4)
        {
            EnemySpawnManager.Instance.SpawnEnemy("겁쟁이 바다민족");
            CurrentSt -= 4;
            Debug.Log($"{EnemyData.EnemyName} [민족의 부름] 겁쟁이 바다민족 소환, St-4");
        }

        // 조건 2: 야만적인 + 냉정한 합계 5 미만이면 채우기 (1명당 ST 5 소모)
        int wildCount = GridManager.Instance.CountEnemiesByName("야만적인 바다민족");
        int calmCount = GridManager.Instance.CountEnemiesByName("냉정한 바다민족");
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
                Debug.Log($"{EnemyData.EnemyName} [민족의 부름] {summoned}명 소환, St-{summoned * 5}");
        }
    }

    private void ApplySeaDisasterBuff()
    {
        if (GridManager.Instance == null) return;
        EnemyUnit unit = GetComponent<EnemyUnit>();
        if (unit == null) return;

        if (!GridManager.Instance.IsSeaPoint(unit.gridPosition)) return;

        // ATK +10 (1턴 버프)
        unit.ApplyBuff(new ActiveBuff(10, 1));
        // 재빠름: 1회 추가 행마
        hasSwiftnessBuff = true;
        // 확산: 공격 범위 확장
        hasSpreadBuff = true;
        Debug.Log($"{EnemyData.EnemyName} [바다의 재앙] 바다 포인트 — ATK+10, 재빠름, 확산 활성화");
    }

    // 디버그: 스페이스바로 즉사
    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            TakeDamage(CurrentHp);
    }
}
