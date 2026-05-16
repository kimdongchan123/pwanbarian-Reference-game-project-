using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement; // 🌟 씬(Scene) 이동을 위해 추가된 필수 부품!

// 아군/적 통합 턴 단위
public class TurnActor
{
    public Unit unit;           // 아군이면 채워짐, 적이면 null
    public EnemyUnit enemyUnit; // 적이면 채워짐, 아군이면 null
    public int speed;

    // 파괴된 유닛 에러 방지용 안전장치
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

    // 🌟 게임이 종료되었는지 확인하는 스위치 (중복 실행 방지)
    private bool isGameOver = false;

    void Awake() => Instance = this;

    IEnumerator Start()
    {
        SpawnUnitsFromStageManager();
        // EnemyBattleSetup.Instance?.SpawnEnemies();
        yield return null; // EnemySpawnManager.Start()가 먼저 실행되도록 1프레임 대기
        GenerateTurnOrder();
    }

    void SpawnUnitsFromStageManager()
    {
        allUnits.Clear();
        if (StageManager.SelectedPartyMembers == null || StageManager.SelectedPartyMembers.Length == 0)
        {
            Debug.LogWarning(" BattleData에 배치된 유닛이 없습니다!");
            return;
        }

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
            int speed = unit.stats != null ? Random.Range(unit.stats.minSpeed, unit.stats.maxSpeed + 1) : 5;
            if (unit.stats != null) unit.stats.currentTurnSpeed = speed;
            allies.Add(new TurnActor { unit = unit, speed = speed, isAllyFlag = true });
        }
        allies = allies.OrderByDescending(a => a.speed).ToList();

        List<TurnActor> enemies = new List<TurnActor>();
        foreach (var eu in FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None))
        {
            Enemy enemy = eu.GetComponent<Enemy>();
            if (enemy?.EnemyData == null) continue;
            int speed = Random.Range(enemy.EnemyData.minSp, enemy.EnemyData.maxSp + 1);
            enemies.Add(new TurnActor { enemyUnit = eu, speed = speed, isAllyFlag = false });
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
            Debug.Log($"  {(a.isAllyFlag ? "🟦아군" : "🟥적")} {a.displayName} (SP: {a.speed})");

        ProcessCurrentTurn();
    }

    private TurnActor GetCurrentActor()
    {
        if (currentTurnIndex < finalTurnOrder.Count)
            return finalTurnOrder[currentTurnIndex];
        return null;
    }

    public Unit GetCurrentUnit()
    {
        return GetCurrentActor()?.unit;
    }

    private void ProcessCurrentTurn()
    {
        if (isGameOver) return; // 이미 끝났으면 아무것도 하지 않음

        TurnActor actor = GetCurrentActor();
        if (actor == null) { StartNewRound(); return; }

        // 죽은 유닛 건너뜀 (안전장치 적용)
        if (!actor.IsAlive) { NextTurn(); return; }

        if (actor.isAllyFlag)
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
        // 🌟 누군가 행동을 마쳤을 때, 게임이 끝났는지 확인합니다!
        if (CheckWinLossCondition()) return;

        currentTurnIndex++;
        if (currentTurnIndex >= finalTurnOrder.Count)
        {
            StartNewRound();
            return;
        }
        ProcessCurrentTurn();
    }

    // 🌟 [핵심 추가] 승리 및 패배 조건 검사 로직
    private bool CheckWinLossCondition()
    {
        if (isGameOver) return true;

        allUnits.RemoveAll(u => u == null); // 죽은 아군 명부 정리

        // 1. 패배 조건: 아군이 모두 사망했을 때
        if (allUnits.Count == 0)
        {
            isGameOver = true;
            Debug.Log("💀 [패배] 모든 아군이 전멸했습니다.");
            StartCoroutine(GameOverRoutine());
            return true;
        }

        // 2. 승리 조건: 맵 위에 적군이 0명일 때
        EnemyUnit[] remainingEnemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        if (remainingEnemies.Length == 0)
        {
            isGameOver = true;
            Debug.Log("🎉 [승리] 모든 적을 처치했습니다!");
            StartCoroutine(GameOverRoutine());
            return true;
        }

        return false;
    }

    // 🌟 [핵심 추가] 여운을 주고 메인 메뉴로 돌아가는 코루틴
    // 🌟 여운을 주고 메인 메뉴로 돌아가는 코루틴
    private IEnumerator GameOverRoutine()
    {
        // 2초 동안 방금 죽은 적(또는 아군)을 감상(?)할 시간을 줍니다.
        yield return new WaitForSeconds(2f);

        Debug.Log("🔄 메인 메뉴로 돌아갑니다...");
        // 🌟 올려주신 사진의 씬 이름에 맞춰 "MainMenu"로 정확하게 수정!
        SceneManager.LoadScene("MainMenu");
    }

    // ============================
    // 적 행동
    // ============================
    private IEnumerator EnemyActAndNext(EnemyUnit enemyUnit)
    {
        if (enemyUnit == null) { NextTurn(); yield break; }

        Enemy enemy = enemyUnit.GetComponent<Enemy>();
        enemy?.OnTurnStart();

        // 턴 시작 시 스킬 쿨타임 감소
        enemyUnit.TickSkillCT();

        yield return StartCoroutine(MoveEnemyTowardAlly(enemyUnit));

        if (enemy != null && (enemy.HasTrait(TraitEffect.swiftness) || enemy.hasSwiftnessBuff))
        {
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(MoveEnemyTowardAlly(enemyUnit));
        }

        yield return new WaitForSeconds(0.5f);

        // 턴 종료 시 버프 만료 처리 + 발동한 스킬 CT 설정
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
            Vector2Int allyGridPos = new Vector2Int(allyTile.x, allyTile.y);
            Vector2Int enemyGridPos = enemyUnit.gridPosition;

            // 넉백 방향: 적 → 아군 방향으로 1칸 더
            Vector2Int diff = allyGridPos - enemyGridPos;
            int absDiffX = Mathf.Abs(diff.x);
            int absDiffY = Mathf.Abs(diff.y);
            Vector2Int pushDir;

            if (absDiffX == absDiffY) // (1) 완벽한 사선 공격
            {
                pushDir = new Vector2Int(
                    diff.x > 0 ? 1 : (diff.x < 0 ? -1 : 0),
                    diff.y > 0 ? 1 : (diff.y < 0 ? -1 : 0)
                );
            }
            else if (absDiffX > absDiffY) // (2) 주로 가로축/십자형 공격
            {
                pushDir = new Vector2Int(
                    diff.x > 0 ? 1 : (diff.x < 0 ? -1 : 0),
                    0
                );
            }
            else // (3) 주로 세로축/십자형 공격
            {
                pushDir = new Vector2Int(
                    0,
                    diff.y > 0 ? 1 : (diff.y < 0 ? -1 : 0)
                );
            }
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

        int tx = Mathf.RoundToInt(target.transform.position.x + 3.5f);
        int ty = Mathf.RoundToInt(target.transform.position.y + 3.5f);
        Vector2Int targetPos = new Vector2Int(tx, ty);

        Vector2Int diff = targetPos - attackerGridPos;
        int absDiffX = Mathf.Abs(diff.x);
        int absDiffY = Mathf.Abs(diff.y);
        Vector2Int pushDir;

        if (absDiffX == absDiffY) // (1) 완벽한 사선 공격
        {
            pushDir = new Vector2Int(
                diff.x > 0 ? 1 : (diff.x < 0 ? -1 : 0),
                diff.y > 0 ? 1 : (diff.y < 0 ? -1 : 0)
            );
        }
        else if (absDiffX > absDiffY) // (2) 주로 가로축/십자형 공격
        {
            pushDir = new Vector2Int(
                diff.x > 0 ? 1 : (diff.x < 0 ? -1 : 0),
                0
            );
        }
        else // (3) 주로 세로축/십자형 공격
        {
            pushDir = new Vector2Int(
                0,
                diff.y > 0 ? 1 : (diff.y < 0 ? -1 : 0)
            );
        }
        Vector2Int pushPos = targetPos + pushDir;

        if (!MapManager.Instance.tiles.TryGetValue(pushPos, out Tile pushTile)) return;
        if (pushTile.isOccupied) return;

        if (MapManager.Instance.tiles.TryGetValue(targetPos, out Tile currentTile))
        {
            currentTile.isOccupied = false;
            currentTile.currentUnit = null;
        }

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
        if (isGameOver) return;
        Debug.Log("🚩 [라운드 종료] 새 라운드 시작");
        GenerateTurnOrder();
    }
}