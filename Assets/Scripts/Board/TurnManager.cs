using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 아군/적 통합 턴 단위
public class TurnActor
{
    public Unit unit;           // 아군이면 채워짐, 적이면 null
    public EnemyUnit enemyUnit; // 적이면 채워짐, 아군이면 null
    public int speed;
    public bool isAlly => unit != null;
    public string displayName => isAlly ? unit.unitName : enemyUnit?.name ?? "?";
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("기물 프리팹 리스트")]
    public GameObject[] unitPrefabs;

    public List<Unit> allUnits = new List<Unit>();
    private List<TurnActor> finalTurnOrder = new List<TurnActor>();
    private int currentTurnIndex = 0;

    void Awake() => Instance = this;

    void Start()
    {
        SpawnUnitsFromBattleData();
        EnemyBattleSetup.Instance?.SpawnEnemies();
        GenerateTurnOrder();
    }

    void SpawnUnitsFromBattleData()
    {
        allUnits.Clear();
        if (BattleData.placedUnits.Count == 0)
        {
            Debug.LogWarning("⚠️ BattleData에 배치된 유닛이 없습니다!");
            return;
        }
        foreach (var info in BattleData.placedUnits)
        {
            if (info.unitIndex < 0 || info.unitIndex >= unitPrefabs.Length)
            {
                Debug.LogWarning($"⚠️ unitIndex {info.unitIndex}이 범위를 벗어남 (배열 크기: {unitPrefabs.Length})");
                continue;
            }
            GameObject go = Instantiate(unitPrefabs[info.unitIndex], info.position, Quaternion.identity);
            Unit unit = go.GetComponent<Unit>();
            if (unit != null) allUnits.Add(unit);
        }
    }

    public void GenerateTurnOrder()
{
    currentTurnIndex = 0;
    finalTurnOrder.Clear();
    allUnits.RemoveAll(u => u == null);

    // 아군 SP 굴림
    List<TurnActor> allies = new List<TurnActor>();
    foreach (var unit in allUnits)
    {
        int speed = Random.Range(unit.stats.minSpeed, unit.stats.maxSpeed + 1);
        unit.stats.currentTurnSpeed = speed;
        allies.Add(new TurnActor { unit = unit, speed = speed });
    }
    allies = allies.OrderByDescending(a => a.speed).ToList();

    // 적 SP 굴림
    List<TurnActor> enemies = new List<TurnActor>();
    foreach (var eu in FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None))
    {
        Enemy enemy = eu.GetComponent<Enemy>();
        if (enemy?.EnemyData == null) continue;
        int speed = Random.Range(enemy.EnemyData.minSp, enemy.EnemyData.maxSp + 1);
        enemies.Add(new TurnActor { enemyUnit = eu, speed = speed });
    }
    enemies = enemies.OrderByDescending(a => a.speed).ToList();

    // 아군-적-아군-적 순으로 번갈아 배치
    int max = Mathf.Max(allies.Count, enemies.Count);
    for (int i = 0; i < max; i++)
    {
        if (i < allies.Count)  finalTurnOrder.Add(allies[i]);
        if (i < enemies.Count) finalTurnOrder.Add(enemies[i]);
    }

    Debug.Log("🏁 이번 라운드 행동 순서:");
    foreach (var a in finalTurnOrder)
        Debug.Log($"  {(a.isAlly ? "🟦아군" : "🟥적")} {a.displayName} (SP: {a.speed})");

    ProcessCurrentTurn();
}

    private TurnActor GetCurrentActor()
    {
        if (currentTurnIndex < finalTurnOrder.Count)
            return finalTurnOrder[currentTurnIndex];
        return null;
    }

    // PlayerActionController에서 사용 — 아군 턴일 때만 Unit 반환
    public Unit GetCurrentUnit()
    {
        return GetCurrentActor()?.unit;
    }

    private void ProcessCurrentTurn()
    {
        TurnActor actor = GetCurrentActor();
        if (actor == null) { StartNewRound(); return; }

        // 죽은 유닛 건너뜀
        if (actor.isAlly && actor.unit == null)  { NextTurn(); return; }
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

    // ============================
    // 적 행동
    // ============================
    private IEnumerator EnemyActAndNext(EnemyUnit enemyUnit)
    {
        if (enemyUnit == null) { NextTurn(); yield break; }

        enemyUnit.GetComponent<Enemy>()?.OnTurnStart();
        yield return StartCoroutine(MoveEnemyTowardAlly(enemyUnit));
        yield return new WaitForSeconds(0.5f);
        enemyUnit.GetComponent<Enemy>()?.OnTurnEnd();

        NextTurn();
    }

    private IEnumerator MoveEnemyTowardAlly(EnemyUnit enemyUnit)
    {
        Unit nearestAlly = FindNearestAlly(enemyUnit.gridPosition);
        if (nearestAlly == null) yield break;

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
        Debug.Log($"👹 {enemyUnit.name} → ({newPos.x}, {newPos.y}) 이동");
        yield return null;
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