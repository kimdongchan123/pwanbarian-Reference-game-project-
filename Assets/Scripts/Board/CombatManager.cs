// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Board/ 신규 추가 (또는 기존 파일 교체)
// ▶ 레퍼런스의 PlayerUnit 대신 BattleUnit 사용
using UnityEngine;

[System.Serializable]
public class SkillRuntimeSlot
{
    public string skillName;
    public int currentCT;
}

public class ActiveBuff
{
    public int damageBonus;
    public int turnsRemaining;

    public ActiveBuff(int bonus, int turns)
    {
        damageBonus = bonus;
        turnsRemaining = turns;
    }
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    private void Awake() => Instance = this;

    // 플레이어(BattleUnit)가 적을 공격
    public void PlayerAttackEnemy(BattleUnit attacker, CardData card, EnemyUnit defenderUnit)
    {
        Enemy defender = defenderUnit.GetComponent<Enemy>();
        if (defender == null || defender.EnemyData == null) return;

        // BattleUnit의 내부 계산 활용 (DamageType 포함)
        int finalDamage = attacker.CalculateCardDamage(card, null);

        // 적 내성 보정
        float resist = GetResist(defender, card.damageType);
        finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage / resist));

        defender.TakeDamage(finalDamage);
        Debug.Log($"플레이어 → {defender.EnemyData.unitName}: {finalDamage} 피해 (내성:{resist})");
    }

    // 적이 플레이어(BattleUnit)를 공격
    public void EnemyAttackPlayer(EnemyUnit attacker, CardData card, BattleUnit defender)
    {
        Enemy enemy = attacker.GetComponent<Enemy>();
        int enemyATK = enemy != null ? enemy.damage : 0;
        int baseDamage = card.power + enemyATK + attacker.GetDamageBonus();
        defender.TakeDamage(baseDamage, card.damageType, null);
        Debug.Log($"적 → 플레이어: {baseDamage} 피해");
    }

    // 충돌 전용 — 카드 없이 ATK 기반으로 계산
    public void PlayerCollideEnemy(BattleUnit attacker, EnemyUnit defenderUnit)
    {
        Enemy defender = defenderUnit.GetComponent<Enemy>();
        if (defender == null || defender.EnemyData == null) return;

        int baseDamage = (attacker.data != null ? attacker.data.minAtk : 0)
                         + attacker.GetStatusAmount(StatusEffectType.DamageUp);
        float resist = defender.EnemyData.physicalResist > 0 ? defender.EnemyData.physicalResist : 1f;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage / resist));

        defender.TakeDamage(finalDamage);
        Debug.Log($"[충돌] 플레이어 → {defender.EnemyData.unitName}: {finalDamage} 피해");
    }

    public void EnemyCollidePlayer(EnemyUnit attacker, BattleUnit defender)
    {
        Enemy enemy = attacker.GetComponent<Enemy>();
        int enemyATK = enemy != null ? enemy.damage : 0;
        int baseDamage = enemyATK + attacker.GetDamageBonus();
        defender.TakeDamage(baseDamage, DamageType.Physical, null);
        Debug.Log($"[충돌] 적 → 플레이어: {baseDamage} 피해");
    }

    // ============================
    // 적 내성 계산
    // ============================
    private float GetResist(Enemy enemy, DamageType type)
    {
        if (enemy.EnemyData == null) return 1f;
        return type switch
        {
            DamageType.Physical => enemy.EnemyData.physicalResist,
            DamageType.Mental   => enemy.EnemyData.mentalResist,
            DamageType.Special  => enemy.EnemyData.specialResist,
            DamageType.Sin      => enemy.EnemyData.sinResist,
            _                   => 1f
        };
    }
}
