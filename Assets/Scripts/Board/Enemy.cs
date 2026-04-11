// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Board/ 신규 추가
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

    // 바다의 재앙 — 바다 포인트 체류 시 활성화 (매 턴 리셋)
    public bool hasSwiftnessBuff = false;
    public bool hasSpreadBuff = false;

    // 보스 / 엘리트 HP 씬 간 유지
    private static readonly Dictionary<string, int> persistentBossHp = new Dictionary<string, int>();

    private void Awake()
    {
        if (EnemyData != null)
        {
            Sp = Random.Range(EnemyData.minSp, EnemyData.maxSp);
            gameObject.name = EnemyData.unitName;
        }
    }

    private void Start()
    {
        if (EnemyData != null)
        {
            if (persistentBossHp.TryGetValue(EnemyData.unitName, out int savedHp))
                CurrentHp = savedHp;
            else if (HasTrait(TraitEffect.elite))
                CurrentHp = Mathf.Max(1, Mathf.RoundToInt(EnemyData.maxHp * 0.5f));
            else
                CurrentHp = EnemyData.maxHp;

            CurrentSt = EnemyData.maxSt;
            damage = EnemyData.atk;
        }
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
    public void TakeDamage(int dmg)
    {
        CurrentHp -= dmg;
        Debug.Log($"{EnemyData.unitName} HP: {CurrentHp}/{EnemyData.maxHp} (-{dmg})");
        if (CurrentHp <= 0)
        {
            Debug.Log($"{EnemyData.unitName} 사망");
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
            if (trait.traitEffect == TraitEffect.callOfTribe)
                ApplyCallOfTribe(trait);
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
        damage = EnemyData.atk + allies.Count * 2;
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
        EnemyUnit unit = GetComponent<EnemyUnit>();
        if (unit == null) return;

        // 타일 태그가 "SeaPoint"인 경우에만 발동
        if (MapManager.Instance == null) return;
        if (!MapManager.Instance.tiles.TryGetValue(unit.gridPosition, out Tile tile)) return;
        if (!tile.CompareTag("SeaPoint")) return;

        unit.ApplyBuff(new ActiveBuff(10, 1));
        hasSwiftnessBuff = true;
        hasSpreadBuff = true;
        Debug.Log($"{EnemyData.unitName} [바다의 재앙] 바다 포인트 — ATK+10, 재빠름, 확산 활성화");
    }

    // 디버그: 스페이스바로 즉사
    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            TakeDamage(CurrentHp);
    }
}
