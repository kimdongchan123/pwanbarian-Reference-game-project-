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
    public int damage;

    public bool isGroggy = false;
    private bool hasUsedGrogyEscape = false;

    public int wetStacks = 0;
    public int burnStacks = 0;
    public int manaStacks = 0;
    public int shieldHp = 0;

    public bool hasSwiftnessBuff = false;
    public bool hasSpreadBuff = false;
    public bool hasCorrosionBuff = false;

    // 🌟 [추가됨] 디스코드에 올려주신 개별 피격음들을 넣을 주머니!
    [Header("사운드")]
    public AudioClip[] hitSounds;

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

    public void TakeDamage(int dmg) => TakeDamage(dmg, null);

    public void TakeDamage(int dmg, Unit attacker)
    {
        dmg = ApplyEnemyDefenses(dmg, attacker);
        if (dmg <= 0) return;

        // 🌟 [추가됨] 데미지를 입을 때 주머니에서 랜덤 소리를 뽑아 재생합니다!
        if (hitSounds != null && hitSounds.Length > 0 && SoundManager.Instance != null)
        {
            AudioClip randomHit = hitSounds[Random.Range(0, hitSounds.Length)];
            SoundManager.Instance.PlaySFX(randomHit);
        }

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

    private int ApplyEnemyDefenses(int dmg, Unit attacker)
    {
        int def = EnemyData?.maxdef ?? 0;

        if (HasTrait(TraitEffect.avoidance) && dmg <= def) return 0;
        if (HasTrait(TraitEffect.aerialAcrobatics))
        {
            int attackerSp = attacker?.stats?.currentTurnSpeed ?? 999;
            if (dmg <= def || attackerSp <= 4) return 0;
        }
        if (HasTrait(TraitEffect.parry) && attacker != null && EnemyData != null)
        {
            int attackerTotal = (attacker.stats?.baseAttack ?? 0) + (attacker.data?.def ?? 0);
            int selfTotal = damage + def;
            if (attackerTotal < selfTotal)
            {
                int stDmg = selfTotal - attackerTotal;
                attacker.currentSt = Mathf.Max(0, attacker.currentSt - stDmg);
                return 0;
            }
        }
        if (shieldHp > 0)
        {
            int absorbed = Mathf.Min(shieldHp, dmg);
            shieldHp -= absorbed;
            dmg -= absorbed;
            if (dmg <= 0) return 0;
        }
        return dmg;
    }

    public void AddBurn(int amount) { burnStacks += amount; }

    public void OnAllyDied()
    {
        if (!HasTrait(TraitEffect.struggling)) return;
        float resist = EnemyData != null ? EnemyData.mentalResist : 1f;
        int dmg = Mathf.Max(1, Mathf.RoundToInt(5f / resist));
        CurrentHp -= dmg;
        damage += 1;
        if (CurrentHp <= 0)
        {
            foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if (e != this) e.OnAllyDied();
            Destroy(gameObject);
        }
    }

    public void TakeStaggerDamage(int amount)
    {
        if (HasTrait(TraitEffect.machineSpirit)) return;
        CurrentSt -= amount;
        if (CurrentSt <= 0 && !isGroggy) TryEnterGroggy();
    }

    public void RecoverSt(int amount)
    {
        if (EnemyData == null) return;
        CurrentSt = Mathf.Min(CurrentSt + amount, EnemyData.maxSt);
    }

    private void TryEnterGroggy()
    {
        if (!hasUsedGrogyEscape && HasTrait(TraitEffect.grogyEscape))
        {
            hasUsedGrogyEscape = true;
            CurrentSt = EnemyData.maxSt;
            return;
        }
        isGroggy = true;
        CurrentSt = 0;
    }

    public void RecoverFromGroggy()
    {
        if (!isGroggy) return;
        isGroggy = false;
        CurrentSt = EnemyData.maxSt;
    }

    public bool HasTrait(TraitEffect effect)
    {
        if (EnemyData == null || EnemyData.traits == null) return false;
        foreach (var trait in EnemyData.traits)
            if (trait != null && trait.traitEffect == effect) return true;
        return false;
    }

    public void ApplyWet(int stacks)
    {
        if (HasTrait(TraitEffect.floodingSeaDisaster)) return;
        int actual = HasTrait(TraitEffect.seaDisaster) ? Mathf.Max(1, Mathf.RoundToInt(stacks * 0.5f)) : stacks;
        wetStacks += actual;
    }

    public void OnTurnStart()
    {
        hasSwiftnessBuff = false;
        hasSpreadBuff = false;
        hasCorrosionBuff = false;
        shieldHp = 0;

        if (EnemyData == null || EnemyData.traits == null) return;
        foreach (var trait in EnemyData.traits)
        {
            if (trait == null) continue;
            switch (trait.traitEffect)
            {
                case TraitEffect.heroOfTribe: ApplyHeroOfTribe(trait); break;
                case TraitEffect.clearyourmind: RecoverSt(trait.stAmount); break;
                case TraitEffect.seaDisaster: ApplySeaDisasterBuff(); break;
                case TraitEffect.dragonScale: shieldHp += 40; break;
                case TraitEffect.giantKing: ApplyGiantKing(); break;
                case TraitEffect.ragingFlame:
                    if (manaStacks >= 10)
                    {
                        manaStacks -= 10;
                        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
                            unit.AddStatus(StatusEffectType.Burn, 3);
                    }
                    break;
            }
        }
    }

    public void OnTurnEnd()
    {
        if (EnemyData == null || EnemyData.traits == null) return;
        foreach (var trait in EnemyData.traits)
        {
            if (trait == null) continue;
            switch (trait.traitEffect)
            {
                case TraitEffect.callOfTribe: ApplyCallOfTribe(trait); break;
                case TraitEffect.floodingSeaDisaster: ExpandSeaTiles(); break;
            }
        }
    }

    private void ApplyHeroOfTribe(TraitData trait)
    {
        List<Enemy> allies = new List<Enemy>();
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e.EnemyData != null && e.EnemyData.affiliation == trait.affiliationTarget) allies.Add(e);

        foreach (var ally in allies) ally.RecoverSt(trait.stAmount);
        damage = EnemyData.maxatk + allies.Count * 2;
    }

    private void ApplyCallOfTribe(TraitData trait)
    {
        if (EnemySpawnManager.Instance == null) return;
        int cowardCount = CountEnemiesByName("겁쟁이 바다민족");
        if (cowardCount == 0 && CurrentSt >= 4)
        {
            EnemySpawnManager.Instance.SpawnEnemy("겁쟁이 바다민족");
            CurrentSt -= 4;
        }

        int wildCount = CountEnemiesByName("야만적인 바다민족");
        int calmCount = CountEnemiesByName("냉정한 바다민족");
        int total = wildCount + calmCount;

        if (total < 5)
        {
            int needed = 5 - total;
            for (int i = 0; i < needed; i++)
            {
                if (CurrentSt < 5) break;
                string target = Random.Range(0, 2) == 0 ? "야만적인 바다민족" : "냉정한 바다민족";
                EnemySpawnManager.Instance.SpawnEnemy(target);
                CurrentSt -= 5;
            }
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
        if (HasTrait(TraitEffect.flight)) return;
        EnemyUnit unit = GetComponent<EnemyUnit>();
        if (unit == null || MapManager.Instance == null) return;
        if (!MapManager.Instance.tiles.TryGetValue(unit.gridPosition, out Tile tile) || !tile.CompareTag("SeaPoint")) return;

        unit.ApplyBuff(new LegacyEnemyBuff(10, 1));
        hasSwiftnessBuff = true;
        hasSpreadBuff = true;
    }

    private void ApplyGiantKing()
    {
        int giantCount = 0;
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e == this || e.EnemyData == null) continue;
            bool isGiant = e.EnemyData.affiliation == "거인연맹" || (e.EnemyData.traitKeywords != null && e.EnemyData.traitKeywords.Contains("거인"));
            if (!isGiant) continue;
            e.damage += 3;
            giantCount++;
        }
        damage = EnemyData.maxatk + giantCount * 3;
    }

    private void ExpandSeaTiles()
    {
        if (MapManager.Instance == null) return;
        EnemyUnit unit = GetComponent<EnemyUnit>();
        if (unit == null) return;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            if (MapManager.Instance.tiles.TryGetValue(unit.gridPosition + dir, out Tile tile))
                tile.gameObject.tag = "SeaPoint";
        }
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) TakeDamage(CurrentHp);
    }
}