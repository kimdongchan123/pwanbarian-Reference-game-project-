using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("스포너")]
    public UnitSpawner allySpawner;
    public UnitSpawner enemySpawner;

    [Header("전투 설정")]
    public float turnDelay = 0.5f;
    public int maxRounds = 30;

    private readonly List<BattleUnit> allies = new List<BattleUnit>();
    private readonly List<BattleUnit> enemies = new List<BattleUnit>();

    private bool battleEnded = false;

    private void Start()
    {
        StartCoroutine(StartBattle());
    }

    private IEnumerator StartBattle()
    {
        yield return null;

        if (allySpawner == null || enemySpawner == null)
        {
            Debug.LogWarning("BattleManager: 스포너 연결 필요");
            yield break;
        }

        RegisterSpawnedUnits();

        if (allies.Count == 0 || enemies.Count == 0)
        {
            Debug.LogWarning("BattleManager: 아군 또는 적군이 없음");
            yield break;
        }

        Debug.Log("===== 전투 시작 =====");

        for (int round = 1; round <= maxRounds; round++)
        {
            if (battleEnded)
                yield break;

            Debug.Log($"===== {round} 라운드 시작 =====");

            List<BattleUnit> turnOrder = BuildTurnOrder();

            for (int i = 0; i < turnOrder.Count; i++)
            {
                BattleUnit actor = turnOrder[i];

                if (actor == null || actor.IsDead())
                    continue;

                if (IsTeamDefeated(BattleTeam.Ally) || IsTeamDefeated(BattleTeam.Enemy))
                {
                    EndBattle();
                    yield break;
                }

                yield return StartCoroutine(PlayUnitTurn(actor));

                if (battleEnded)
                    yield break;

                yield return new WaitForSeconds(turnDelay);
            }

            Debug.Log($"===== {round} 라운드 종료 =====");
        }

        Debug.Log("최대 라운드 도달");
        EndBattle();
    }

    private void RegisterSpawnedUnits()
    {
        allies.Clear();
        enemies.Clear();

        if (allySpawner.spawnedUnits != null)
        {
            foreach (BattleUnit unit in allySpawner.spawnedUnits)
            {
                if (unit != null)
                    allies.Add(unit);
            }
        }

        if (enemySpawner.spawnedUnits != null)
        {
            foreach (BattleUnit unit in enemySpawner.spawnedUnits)
            {
                if (unit != null)
                    enemies.Add(unit);
            }
        }
    }

    private List<BattleUnit> BuildTurnOrder()
    {
        List<BattleUnit> allUnits = new List<BattleUnit>();

        foreach (BattleUnit ally in allies)
        {
            if (ally != null && !ally.IsDead())
                allUnits.Add(ally);
        }

        foreach (BattleUnit enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead())
                allUnits.Add(enemy);
        }

        allUnits.Sort((a, b) =>
        {
            int speedA = a.RollSpeed();
            int speedB = b.RollSpeed();

            // 높은 속도 우선
            int compare = speedB.CompareTo(speedA);
            if (compare != 0)
                return compare;

            // 같으면 랜덤
            return Random.Range(0, 2) == 0 ? -1 : 1;
        });

        Debug.Log("턴 순서 정렬 완료");

        foreach (BattleUnit unit in allUnits)
        {
            Debug.Log($"{unit.data.unitName} 행동 예정");
        }

        return allUnits;
    }

    private IEnumerator PlayUnitTurn(BattleUnit actor)
    {
        if (actor == null || actor.IsDead())
            yield break;

        BattleUnit target = GetRandomAliveEnemy(actor.team);

        if (target == null)
        {
            EndBattle();
            yield break;
        }

        actor.OnTurnStart();
        yield return new WaitForSeconds(turnDelay);

        while (actor.remainingActions > 0 && actor.hand.Count > 0)
        {
            CardData selectedCard = actor.hand[0];

            if (selectedCard == null)
                break;

            if (selectedCard.targetType == CardTargetType.Self)
            {
                actor.UseCard(selectedCard, actor);
            }
            else
            {
                target = GetRandomAliveEnemy(actor.team);

                if (target == null)
                {
                    EndBattle();
                    yield break;
                }

                actor.UseCard(selectedCard, target);
            }

            yield return new WaitForSeconds(turnDelay);

            if (IsTeamDefeated(BattleTeam.Ally) || IsTeamDefeated(BattleTeam.Enemy))
            {
                EndBattle();
                yield break;
            }
        }

        actor.OnTurnEnd();
    }

    private BattleUnit GetRandomAliveEnemy(BattleTeam actorTeam)
    {
        List<BattleUnit> targetList = actorTeam == BattleTeam.Ally ? enemies : allies;
        List<BattleUnit> aliveTargets = new List<BattleUnit>();

        foreach (BattleUnit unit in targetList)
        {
            if (unit != null && !unit.IsDead())
                aliveTargets.Add(unit);
        }

        if (aliveTargets.Count == 0)
            return null;

        int randomIndex = Random.Range(0, aliveTargets.Count);
        return aliveTargets[randomIndex];
    }

    private bool IsTeamDefeated(BattleTeam team)
    {
        List<BattleUnit> targetList = team == BattleTeam.Ally ? allies : enemies;

        foreach (BattleUnit unit in targetList)
        {
            if (unit != null && !unit.IsDead())
                return false;
        }

        return true;
    }

    private void EndBattle()
    {
        if (battleEnded)
            return;

        battleEnded = true;

        bool allyDead = IsTeamDefeated(BattleTeam.Ally);
        bool enemyDead = IsTeamDefeated(BattleTeam.Enemy);

        if (allyDead && enemyDead)
        {
            Debug.Log("===== 무승부 =====");
        }
        else if (enemyDead)
        {
            Debug.Log("===== 아군 승리 =====");
        }
        else if (allyDead)
        {
            Debug.Log("===== 적군 승리 =====");
        }
        else
        {
            Debug.Log("===== 전투 종료 =====");
        }
    }
}
