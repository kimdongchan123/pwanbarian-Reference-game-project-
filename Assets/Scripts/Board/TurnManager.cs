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

    IEnumerator Start()
    {
        SpawnUnitsFromBattleData();
        // EnemyBattleSetup.Instance?.SpawnEnemies();
        yield return null; // EnemySpawnManager.Start()가 먼저 실행되도록 1프레임 대기
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

        Enemy enemy = enemyUnit.GetComponent<Enemy>();
        enemy?.OnTurnStart();
        yield return StartCoroutine(MoveEnemyTowardAlly(enemyUnit));

        if (enemy != null && (enemy.HasTrait(TraitEffect.swiftness) || enemy.hasSwiftnessBuff))
        {
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(MoveEnemyTowardAlly(enemyUnit));
        }

        yield return new WaitForSeconds(0.5f);
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

        // 이미 인접해 있으면 이동 없이 바로 공격
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
        Debug.Log($"👹 {enemyUnit.name} → ({newPos.x}, {newPos.y}) 이동");

        // 이동 후 인접하게 됐으면 공격
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
        Enemy enemy = enemyUnit.GetComponent<Enemy>();
        int dmg = enemy != null ? enemy.damage : 1;

        UnitMovement allyMovement = ally.movement;
        if (allyMovement != null && allyMovement.currentTile != null)
        {
            Tile allyTile = allyMovement.currentTile;
            Vector2Int allyGridPos   = new Vector2Int(allyTile.x, allyTile.y);
            Vector2Int enemyGridPos  = enemyUnit.gridPosition;

            // 넉백 방향: 적 → 아군 방향으로 1칸 더
            Vector2Int diff    = allyGridPos - enemyGridPos;
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
                // 아군 → 넉백 위치
                allyTile.isOccupied = false;
                allyTile.currentUnit = null;
                ally.transform.position = knockBackTile.transform.position;
                knockBackTile.isOccupied = true;
                knockBackTile.currentUnit = ally.gameObject;
                allyMovement.currentTile = knockBackTile;

                // 적 → 아군의 원래 위치
                if (MapManager.Instance.tiles.TryGetValue(enemyGridPos, out Tile enemyOldTile))
                {
                    enemyOldTile.isOccupied = false;
                    enemyOldTile.currentUnit = null;
                }
                enemyUnit.transform.position = allyTile.transform.position;
                enemyUnit.gridPosition = allyGridPos;
                allyTile.isOccupied = true;
                allyTile.currentUnit = enemyUnit.gameObject;

                Debug.Log($"💨 {ally.unitName} 넉백 → ({knockBackGridPos.x}, {knockBackGridPos.y})");
            }
            else
            {
                Debug.Log($"🧱 {ally.unitName} 넉백 불가 (벽 또는 유닛에 막힘)");
            }
        }

        // 데미지 + 로그
        int prevHp = ally.currentHp;
        ally.currentHp = Mathf.Max(0, ally.currentHp - dmg);
        Debug.Log($"👹 {enemyUnit.name} → {ally.unitName} | HP: {prevHp} → {ally.currentHp}/{ally.maxHp} (-{dmg})");

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

    public void KnockBack(GameObject target, Vector2Int attackerGridPos)
    {
        if (MapManager.Instance == null) return;

        // 피격 유닛의 현재 그리드 좌표 계산
        int tx = Mathf.RoundToInt(target.transform.position.x + 3.5f);
        int ty = Mathf.RoundToInt(target.transform.position.y + 3.5f);
        Vector2Int targetPos = new Vector2Int(tx, ty);

        // 밀려나는 방향: 공격자 → 피격자 방향으로 1칸
        Vector2Int diff = targetPos - attackerGridPos;
        Vector2Int pushDir = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
            ? new Vector2Int((int)Mathf.Sign(diff.x), 0)
            : new Vector2Int(0, (int)Mathf.Sign(diff.y));

        Vector2Int pushPos = targetPos + pushDir;

        if (!MapManager.Instance.tiles.TryGetValue(pushPos, out Tile pushTile)) return;
        if (pushTile.isOccupied) return; // 뒤가 막혀있으면 밀리지 않음

        // 기존 타일 점유 해제
        if (MapManager.Instance.tiles.TryGetValue(targetPos, out Tile currentTile))
        {
            currentTile.isOccupied = false;
            currentTile.currentUnit = null;
        }

        // 새 위치로 이동
        target.transform.position = pushTile.transform.position;
        pushTile.isOccupied = true;
        pushTile.currentUnit = target;

        Debug.Log($"💨 {target.name} 넉백 → ({pushPos.x}, {pushPos.y})");
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