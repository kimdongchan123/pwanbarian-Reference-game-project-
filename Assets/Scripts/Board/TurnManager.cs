using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 아군/적 통합 턴 단위 클래스
public class TurnActor
{
    public Unit unit;
    public EnemyUnit enemyUnit;
    public int speed;
    public bool isAllyFlag;

    public string displayName
    {
        get
        {
            if (isAllyFlag) return unit != null ? unit.unitName : "사망한 기물";
            return enemyUnit != null ? enemyUnit.name : "사망한 기물";
        }
    }

    public bool IsAlive => isAllyFlag ? (unit != null) : (enemyUnit != null);
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public List<Unit> allUnits = new List<Unit>();
    private List<TurnActor> finalTurnOrder = new List<TurnActor>();
    private int currentTurnIndex = 0;

    void Awake()
    {
        Instance = this;
    }

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

            int gridX = (int)info.file - 1;
            int gridY = info.rank - 1;
            Vector3 spawnPos = new Vector3(gridX - 3.5f, gridY - 3.5f, 0f);

            GameObject go = Instantiate(info.unitData.unitPrefab, spawnPos, Quaternion.identity);
            Unit unit = go.GetComponent<Unit>();

            if (unit != null)
            {
                unit.unitName = info.unitData.unitName;
                allUnits.Add(unit);
            }

            Vector2Int pos2D = new Vector2Int(gridX, gridY);
            if (MapManager.Instance != null && MapManager.Instance.tiles.TryGetValue(pos2D, out Tile tile))
            {
                tile.isOccupied = true;
                tile.currentUnit = go;
                if (unit != null && unit.movement != null)
                {
                    unit.movement.currentTile = tile;
                }
            }
        }
    }

    public void GenerateTurnOrder()
    {
        currentTurnIndex = 0;
        finalTurnOrder.Clear();

        List<TurnActor> actors = new List<TurnActor>();
        foreach (var unit in allUnits)
        {
            if (unit == null) continue;
            int speed = 5;
            actors.Add(new TurnActor { unit = unit, speed = speed, isAllyFlag = true });
        }

        foreach (var eu in FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None))
        {
            Enemy enemy = eu.GetComponent<Enemy>();
            int speed = enemy != null ? Random.Range(enemy.Sp, enemy.Sp + 3) : 3;
            actors.Add(new TurnActor { enemyUnit = eu, speed = speed, isAllyFlag = false });
        }

        finalTurnOrder = actors.OrderByDescending(a => a.speed).ToList();
        ProcessCurrentTurn();
    }

    public Unit GetCurrentUnit()
    {
        if (currentTurnIndex < finalTurnOrder.Count) return finalTurnOrder[currentTurnIndex].unit;
        return null;
    }

    private void ProcessCurrentTurn()
    {
        if (currentTurnIndex >= finalTurnOrder.Count)
        {
            StartNewRound();
            return;
        }

        TurnActor actor = finalTurnOrder[currentTurnIndex];

        if (!actor.IsAlive)
        {
            NextTurn();
            return;
        }

        if (actor.isAllyFlag)
        {
            Debug.Log($"➡️ [아군 턴] {actor.displayName}");
        }
        else
        {
            Debug.Log($"👹 [적 턴] {actor.displayName}");
            StartCoroutine(EnemyActRoutine(actor.enemyUnit));
        }
    }

    public void NextTurn()
    {
        currentTurnIndex++;
        ProcessCurrentTurn();
    }

    // 🌟 [부활] 적 AI 행동 코루틴
    private IEnumerator EnemyActRoutine(EnemyUnit enemyUnit)
    {
        if (enemyUnit == null) { NextTurn(); yield break; }

        yield return new WaitForSeconds(0.5f);

        // 가장 가까운 아군을 찾아 이동 및 공격
        yield return StartCoroutine(MoveEnemyTowardAlly(enemyUnit));

        yield return new WaitForSeconds(0.5f);
        NextTurn();
    }

    // 🌟 [부활] 아군을 향해 길을 찾고 이동하는 로직
    private IEnumerator MoveEnemyTowardAlly(EnemyUnit enemyUnit)
    {
        Unit nearestAlly = FindNearestAlly(enemyUnit.gridPosition);
        if (nearestAlly == null) yield break;

        int ax = Mathf.RoundToInt(nearestAlly.transform.position.x + 3.5f);
        int ay = Mathf.RoundToInt(nearestAlly.transform.position.y + 3.5f);
        Vector2Int allyPos = new Vector2Int(ax, ay);

        // 이미 바로 옆에 있다면 바로 공격!
        if (IsAdjacent(enemyUnit.gridPosition, allyPos))
        {
            AttackAlly(enemyUnit, nearestAlly);
            yield break;
        }

        // 인접하지 않다면 한 칸 이동
        Tile targetTile = FindStepTowardAlly(enemyUnit.gridPosition, allyPos);
        if (targetTile != null)
        {
            // 기존 타일 비우기
            if (MapManager.Instance.tiles.TryGetValue(enemyUnit.gridPosition, out Tile oldTile))
            {
                oldTile.isOccupied = false;
                oldTile.currentUnit = null;
            }

            // 새 타일로 물리적 이동
            Vector2Int newPos = new Vector2Int(targetTile.x, targetTile.y);
            enemyUnit.gridPosition = newPos;
            enemyUnit.transform.position = targetTile.transform.position;

            targetTile.isOccupied = true;
            targetTile.currentUnit = enemyUnit.gameObject;

            yield return new WaitForSeconds(0.3f);

            // 이동했더니 바로 옆에 아군이 있다면 이어서 공격!
            if (IsAdjacent(newPos, allyPos))
            {
                AttackAlly(enemyUnit, nearestAlly);
            }
        }
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

    private Unit FindNearestAlly(Vector2Int fromPos)
    {
        Unit nearest = null;
        float minDist = float.MaxValue;

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;
            float dist = Vector2Int.Distance(fromPos, new Vector2Int(Mathf.RoundToInt(unit.transform.position.x + 3.5f), Mathf.RoundToInt(unit.transform.position.y + 3.5f)));
            if (dist < minDist) { minDist = dist; nearest = unit; }
        }
        return nearest;
    }

    private Tile FindStepTowardAlly(Vector2Int enemyPos, Vector2Int allyPos)
    {
        Tile bestTile = null;
        float bestDist = Vector2Int.Distance(enemyPos, allyPos);
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            Vector2Int candidate = enemyPos + dir;
            if (!MapManager.Instance.tiles.TryGetValue(candidate, out Tile tile) || tile.isOccupied) continue;

            float dist = Vector2Int.Distance(candidate, allyPos);
            if (dist < bestDist) { bestDist = dist; bestTile = tile; }
        }
        return bestTile;
    }

    // 🌟 [부활] 적이 아군을 밀어내는(넉백) 로직
    private void AttackAlly(EnemyUnit enemyUnit, Unit ally)
    {
        UnitMovement allyMovement = ally.movement;
        if (allyMovement != null && allyMovement.currentTile != null)
        {
            Tile allyTile = allyMovement.currentTile;
            Vector2Int diff = new Vector2Int(allyTile.x, allyTile.y) - enemyUnit.gridPosition;

            // 밀어낼 방향 설정
            Vector2Int pushDir = (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                ? new Vector2Int((int)Mathf.Sign(diff.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(diff.y));
            Vector2Int knockBackGridPos = new Vector2Int(allyTile.x, allyTile.y) + pushDir;

            // 넉백 타일이 비어있다면 아군을 밀침
            if (MapManager.Instance != null && MapManager.Instance.tiles.TryGetValue(knockBackGridPos, out Tile knockBackTile) && !knockBackTile.isOccupied)
            {
                allyTile.isOccupied = false; allyTile.currentUnit = null;
                ally.transform.position = knockBackTile.transform.position;
                knockBackTile.isOccupied = true; knockBackTile.currentUnit = ally.gameObject;
                allyMovement.currentTile = knockBackTile;

                if (MapManager.Instance.tiles.TryGetValue(enemyUnit.gridPosition, out Tile enemyOldTile))
                { enemyOldTile.isOccupied = false; enemyOldTile.currentUnit = null; }

                // 적이 그 자리로 전진
                enemyUnit.transform.position = allyTile.transform.position;
                enemyUnit.gridPosition = new Vector2Int(allyTile.x, allyTile.y);
                allyTile.isOccupied = true; allyTile.currentUnit = enemyUnit.gameObject;
            }
        }

        // 지금은 스탯 시스템이 꺼져있으므로 임시로 10 데미지만 줍니다.
        ally.currentHp -= 10;
        if (ally.currentHp <= 0)
        {
            allUnits.Remove(ally);
            Destroy(ally.gameObject);
        }
    }

    private void StartNewRound()
    {
        GenerateTurnOrder();
    }
}