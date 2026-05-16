using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

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
    private bool isGameOver = false;

    // 🌟 [추가됨] UI 패널 연결을 위한 빈칸
    [Header("UI 패널 연결")]
    public GameObject winPanel;
    public GameObject losePanel;

    void Awake() => Instance = this;

    IEnumerator Start()
    {
        SpawnUnitsFromStageManager();
        yield return null;
        GenerateTurnOrder();

        // 🌟 시작할 때 패널들이 켜져있으면 꺼줍니다.
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
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
            if (unit != null) { unit.unitName = info.unitData.unitName; allUnits.Add(unit); }

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
        if (isGameOver) return;

        TurnActor actor = GetCurrentActor();
        if (actor == null) { StartNewRound(); return; }

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
        if (CheckWinLossCondition()) return;

        currentTurnIndex++;
        if (currentTurnIndex >= finalTurnOrder.Count)
        {
            StartNewRound();
            return;
        }
        ProcessCurrentTurn();
    }

    private bool CheckWinLossCondition()
    {
        if (isGameOver) return true;

        allUnits.RemoveAll(u => u == null);

        // 패배
        if (allUnits.Count == 0)
        {
            isGameOver = true;
            Debug.Log("💀 [패배] 모든 아군이 전멸했습니다.");
            StartCoroutine(ShowGameOverPanel(false));
            return true;
        }

        // 승리
        EnemyUnit[] remainingEnemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        if (remainingEnemies.Length == 0)
        {
            isGameOver = true;
            Debug.Log("🎉 [승리] 모든 적을 처치했습니다!");
            StartCoroutine(ShowGameOverPanel(true));
            return true;
        }

        return false;
    }

    // 🌟 [수정됨] 씬을 바로 넘기지 않고 패널을 띄웁니다.
    private IEnumerator ShowGameOverPanel(bool isWin)
    {
        yield return new WaitForSeconds(1.5f); // 1.5초 여운

        if (isWin)
        {
            if (winPanel != null) winPanel.SetActive(true);
        }
        else
        {
            if (losePanel != null) losePanel.SetActive(true);
        }
    }

    // 🌟 [추가됨] UI 버튼 클릭 시 작동할 함수들 (인스펙터에서 버튼 OnClick에 등록하세요)
    public void OnClickGoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnClickNextStage()
    {
        // 추후 다음 스테이지 씬이 생기면 수정, 지금은 StageSelectScene으로 이동
        SceneManager.LoadScene("StageSelectScene");
    }

    // ============================
    // 적 행동 (그대로 보존)
    // ============================
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

        if (IsAdjacent(newPos, allyPos)) AttackAlly(enemyUnit, nearestAlly);

        yield return null;
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

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

            Vector2Int diff = allyGridPos - enemyGridPos;
            int dirX = diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1);
            int dirY = diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1);
            Vector2Int pushDir = new Vector2Int(dirX, dirY);
            Vector2Int knockBackGridPos = allyGridPos + pushDir;

            Tile knockBackTile = null;
            bool canKnockBack = MapManager.Instance != null && MapManager.Instance.tiles.TryGetValue(knockBackGridPos, out knockBackTile) && !knockBackTile.isOccupied;

            if (canKnockBack)
            {
                allyTile.isOccupied = false; allyTile.currentUnit = null;
                ally.transform.position = knockBackTile.transform.position;
                knockBackTile.isOccupied = true; knockBackTile.currentUnit = ally.gameObject;
                allyMovement.currentTile = knockBackTile;

                if (MapManager.Instance.tiles.TryGetValue(enemyGridPos, out Tile enemyOldTile)) { enemyOldTile.isOccupied = false; enemyOldTile.currentUnit = null; }
                enemyUnit.transform.position = allyTile.transform.position;
                enemyUnit.gridPosition = allyGridPos;
                allyTile.isOccupied = true; allyTile.currentUnit = enemyUnit.gameObject;
            }
        }

        int prevHp = ally.currentHp;
        ally.currentHp = Mathf.Max(0, ally.currentHp - dmg);

        if (ally.currentHp <= 0)
        {
            if (allyMovement?.currentTile != null) { allyMovement.currentTile.isOccupied = false; allyMovement.currentTile.currentUnit = null; }
            allUnits.Remove(ally);
            Destroy(ally.gameObject);
        }
    }

    private Unit FindNearestAlly(Vector2Int fromPos)
    {
        Unit nearest = null; float minDist = float.MaxValue;
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

        Tile bestTile = null; float bestDist = Vector2Int.Distance(enemyPos, allyPos);
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

    private void StartNewRound() { if (!isGameOver) GenerateTurnOrder(); }
}