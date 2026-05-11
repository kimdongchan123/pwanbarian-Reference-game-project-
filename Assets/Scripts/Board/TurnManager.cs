using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TurnActor
{
    public Unit unit;
    public EnemyUnit enemyUnit;
    public int speed;
    public bool isAlly => unit != null;
    public string displayName => isAlly ? unit.unitName : enemyUnit?.name ?? "?";
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public List<Unit> allUnits = new List<Unit>();
    private List<TurnActor> finalTurnOrder = new List<TurnActor>();
    private int currentTurnIndex = 0;

    void Awake() => Instance = this;

    IEnumerator Start()
    {
        SpawnUnitsFromStageManager();
        yield return null;
        GenerateTurnOrder();
    }

    void SpawnUnitsFromStageManager()
    {
        allUnits.Clear();
        if (StageManager.SelectedPartyMembers == null || StageManager.SelectedPartyMembers.Length == 0) return;

        foreach (var info in StageManager.SelectedPartyMembers)
        {
            if (info == null || info.unitData == null || info.unitData.unitPrefab == null) continue;

            int gridX = (int)info.file;
            int gridY = info.rank - 1;
            Vector3 spawnPos = new Vector3(gridX - 3.5f, gridY - 3.5f, 0f);

            GameObject go = Instantiate(info.unitData.unitPrefab, spawnPos, Quaternion.identity);
            Unit unit = go.GetComponent<Unit>();

            if (unit != null)
            {
                unit.unitName = info.unitData.unitName;

                if (unit.battleUnit != null)
                {
                    unit.battleUnit.Initialize(info.unitData);
                }

                allUnits.Add(unit);
            }

            Vector2Int pos2D = new Vector2Int(gridX, gridY);
            if (MapManager.Instance != null && MapManager.Instance.tiles.TryGetValue(pos2D, out Tile tile))
            {
                tile.isOccupied = true;
                tile.currentUnit = go;
                if (unit != null && unit.movement != null) unit.movement.currentTile = tile;
            }
        }
    }

    public void GenerateTurnOrder()
    {
        currentTurnIndex = 0;
        finalTurnOrder.Clear();
        allUnits.RemoveAll(u => u == null);

        List<TurnActor> allies = new List<TurnActor>();
        foreach (var unit in allUnits)
        {
            int speed = unit.battleUnit != null ? unit.battleUnit.RollSpeed() : 5;
            allies.Add(new TurnActor { unit = unit, speed = speed });
        }
        allies = allies.OrderByDescending(a => a.speed).ToList();

        List<TurnActor> enemies = new List<TurnActor>();
        foreach (var eu in FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None))
        {
            Enemy enemy = eu.GetComponent<Enemy>();
            if (enemy?.EnemyData == null) continue;
            int speed = Random.Range(enemy.EnemyData.minSp, enemy.EnemyData.maxSp + 1);
            enemies.Add(new TurnActor { enemyUnit = eu, speed = speed });
        }
        enemies = enemies.OrderByDescending(a => a.speed).ToList();

        int max = Mathf.Max(allies.Count, enemies.Count);
        for (int i = 0; i < max; i++)
        {
            if (i < allies.Count) finalTurnOrder.Add(allies[i]);
            if (i < enemies.Count) finalTurnOrder.Add(enemies[i]);
        }

        Debug.Log("🏁 이번 라운드 행동 순서:");
        foreach (var a in finalTurnOrder)
            Debug.Log($"  {(a.isAlly ? "🟦아군" : "🟥적")} {a.displayName} (SP: {a.speed})");

        ProcessCurrentTurn();
    }

    private TurnActor GetCurrentActor()
    {
        if (currentTurnIndex < finalTurnOrder.Count) return finalTurnOrder[currentTurnIndex];
        return null;
    }

    public Unit GetCurrentUnit() => GetCurrentActor()?.unit;

    private void ProcessCurrentTurn()
    {
        TurnActor actor = GetCurrentActor();
        if (actor == null) { StartNewRound(); return; }

        if (actor.isAlly && actor.unit == null) { NextTurn(); return; }
        if (!actor.isAlly && actor.enemyUnit == null) { NextTurn(); return; }

        if (actor.isAlly)
        {
            Debug.Log($"➡️ [아군 턴] {actor.displayName} — 카드를 선택하세요.");
        }
        else
        {
            Debug.Log($"👹 [적 턴] {actor.displayName} 행동 시작");
            StartCoroutine(EnemyActAndNext(actor.enemyUnit));
        }
    }

    public void NextTurn()
    {
        currentTurnIndex++;
        if (currentTurnIndex >= finalTurnOrder.Count)
        {
            StartNewRound();
            return;
        }
        ProcessCurrentTurn();
    }

    private IEnumerator EnemyActAndNext(EnemyUnit enemyUnit)
    {
        if (enemyUnit == null) { NextTurn(); yield break; }

        Enemy enemy = enemyUnit.GetComponent<Enemy>();
        enemy?.OnTurnStart();
        enemyUnit.TickSkillCT();

        yield return StartCoroutine(MoveEnemyTowardAlly(enemyUnit));

        if (enemy != null && (enemy.HasTrait(TraitEffect.swiftness) || enemy.hasSwiftnessBuff))
        {
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(MoveEnemyTowardAlly(enemyUnit));
        }

        yield return new WaitForSeconds(0.5f);
        enemyUnit.OnEnemyTurnEnd();
        enemy?.OnTurnEnd();
        NextTurn();
    }

    private IEnumerator MoveEnemyTowardAlly(EnemyUnit enemyUnit)
    {
        Unit nearestAlly = FindNearestAlly(enemyUnit.gridPosition);
        if (nearestAlly == null) yield break;

        int ax = Mathf.RoundToInt(nearestAlly.transform.position.x + 3.5f);
        int ay = Mathf.RoundToInt(nearestAlly.transform.position.y + 3.5f);
        Vector2Int allyPos = new Vector2Int(ax, ay);

        if (IsAdjacent(enemyUnit.gridPosition, allyPos))
        {
            enemyUnit.UseNextSkillInSequence();
            AttackAlly(enemyUnit, nearestAlly);
            yield break;
        }

        Tile targetTile = FindStepTowardAlly(enemyUnit.gridPosition, nearestAlly);
        if (targetTile == null) yield break;

        if (MapManager.Instance.tiles.TryGetValue(enemyUnit.gridPosition, out Tile oldTile))
        {
            oldTile.isOccupied = false;
            oldTile.currentUnit = null;
        }

        Vector2Int newPos = new Vector2Int(targetTile.x, targetTile.y);
        enemyUnit.gridPosition = newPos;
        enemyUnit.transform.position = targetTile.transform.position;
        targetTile.isOccupied = true;
        targetTile.currentUnit = enemyUnit.gameObject;

        enemyUnit.UseNextSkillInSequence();

        if (IsAdjacent(newPos, allyPos))
            AttackAlly(enemyUnit, nearestAlly);

        yield return null;
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    private void AttackAlly(EnemyUnit enemyUnit, Unit ally)
    {
        UnitMovement allyMovement = ally.movement;
        if (allyMovement != null && allyMovement.currentTile != null)
        {
            Tile allyTile = allyMovement.currentTile;
            Vector2Int allyGridPos = new Vector2Int(allyTile.x, allyTile.y);
            Vector2Int enemyGridPos = enemyUnit.gridPosition;

            Vector2Int diff = allyGridPos - enemyGridPos;
            Vector2Int pushDir = (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                ? new Vector2Int((int)Mathf.Sign(diff.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(diff.y));
            Vector2Int knockBackGridPos = allyGridPos + pushDir;

            Tile knockBackTile = null;
            bool canKnockBack = MapManager.Instance != null
                                && MapManager.Instance.tiles.TryGetValue(knockBackGridPos, out knockBackTile)
                                && !knockBackTile.isOccupied;

            if (canKnockBack)
            {
                allyTile.isOccupied = false;
                allyTile.currentUnit = null;
                ally.transform.position = knockBackTile.transform.position;
                knockBackTile.isOccupied = true;
                knockBackTile.currentUnit = ally.gameObject;
                allyMovement.currentTile = knockBackTile;

                if (MapManager.Instance.tiles.TryGetValue(enemyGridPos, out Tile enemyOldTile))
                {
                    enemyOldTile.isOccupied = false;
                    enemyOldTile.currentUnit = null;
                }
                enemyUnit.transform.position = allyTile.transform.position;
                enemyUnit.gridPosition = allyGridPos;
                allyTile.isOccupied = true;
                allyTile.currentUnit = enemyUnit.gameObject;
            }
        }

        CombatManager.Instance.EnemyCollidePlayer(enemyUnit, ally.battleUnit);

        if (ally.currentHp <= 0)
        {
            Debug.Log($"💀 {ally.unitName} 사망");
            if (allyMovement?.currentTile != null)
            {
                allyMovement.currentTile.isOccupied = false;
                allyMovement.currentTile.currentUnit = null;
            }
            allUnits.Remove(ally);
            Destroy(ally.gameObject);
        }
    }

    private Unit FindNearestAlly(Vector2Int fromPos)
    {
        Unit nearest = null;
        float minDist = float.MaxValue;
        foreach (var unit in allUnits)
        {
            if (unit == null) continue;
            int ux = Mathf.RoundToInt(unit.transform.position.x + 3.5f);
            int uy = Mathf.RoundToInt(unit.transform.position.y + 3.5f);
            float dist = Vector2Int.Distance(fromPos, new Vector2Int(ux, uy));
            if (dist < minDist) { minDist = dist; nearest = unit; }
        }
        return nearest;
    }

    // 🌟 아까 빠졌던 바로 그 길찾기 함수입니다!
    private Tile FindStepTowardAlly(Vector2Int enemyPos, Unit ally)
    {
        int ax = Mathf.RoundToInt(ally.transform.position.x + 3.5f);
        int ay = Mathf.RoundToInt(ally.transform.position.y + 3.5f);
        Vector2Int allyPos = new Vector2Int(ax, ay);

        Tile bestTile = null;
        float bestDist = Vector2Int.Distance(enemyPos, allyPos);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int candidate = enemyPos + dir;
            if (!MapManager.Instance.tiles.TryGetValue(candidate, out Tile tile)) continue;
            if (tile.isOccupied) continue;
            float dist = Vector2Int.Distance(candidate, allyPos);
            if (dist < bestDist) { bestDist = dist; bestTile = tile; }
        }
        return bestTile;
    }

    private void StartNewRound()
    {
        Debug.Log("🚩 [라운드 종료] 새 라운드 시작");
        GenerateTurnOrder();
    }
}