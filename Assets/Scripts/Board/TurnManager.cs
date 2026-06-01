using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

// 아군/적 통합 턴 단위
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

    [Header("기물 프리팹 리스트")]
    public GameObject[] unitPrefabs;

    [SerializeField] private BattleSideUI teamSidePannel;
    [SerializeField] private BattleSideUI enemySidePannel;

    [Header("UI 패널 연결")]
    public GameObject winPanel;
    public GameObject losePanel;

    public List<Unit> allUnits = new List<Unit>();
    private List<TurnActor> finalTurnOrder = new List<TurnActor>();
    private int currentTurnIndex = 0;

    private bool isGameOver = false;

    void Awake() => Instance = this;

    IEnumerator Start()
    {
        // =========================================================
        // 🌟 [사운드 로직] 회원님의 찐 변수명(SelectedStage) 적용 완료!
        // =========================================================
        if (SoundManager.Instance == null)
        {
            Debug.LogError("🚨 [BGM 실패] 씬에 SoundManager 오브젝트가 없거나 스크립트가 안 붙어있습니다!");
        }
        else if (StageManager.SelectedStage == null)
        {
            Debug.LogError("🚨 [BGM 실패] StageManager.SelectedStage가 비어있습니다! (데이터 전달 실패)");
        }
        else if (StageManager.SelectedStage.stageBGM == null)
        {
            Debug.LogError($"🚨 [BGM 실패] {StageManager.SelectedStage.stageName} 스테이지 데이터에 BGM 파일이 안 들어있습니다!");
        }
        else
        {
            // 모든 조건이 완벽하다면 정상적으로 재생합니다!
            Debug.Log($"🎵 [BGM 재생 성공] {StageManager.SelectedStage.stageBGM.name} 음악을 틉니다!");
            SoundManager.Instance.PlayBGM(StageManager.SelectedStage.stageBGM);
        }
        // =========================================================

        if (!SpawnUnitsFromSelectedPartyData())
        {
            SpawnUnitsFromBattleData();
        }

        EnsureBattleSkillButtonUI();
        yield return null;
        GenerateTurnOrder();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private bool SpawnUnitsFromSelectedPartyData()
    {
        allUnits.Clear();
        if (StageManager.SelectedPartyMembers == null || StageManager.SelectedPartyMembers.Length == 0) return false;

        bool spawnedAny = false;
        for (int i = 0; i < StageManager.SelectedPartyMembers.Length; i++)
        {
            PlayerEntry entry = StageManager.SelectedPartyMembers[i];
            if (entry == null || entry.unitData == null || unitPrefabs == null || unitPrefabs.Length == 0) continue;

            int prefabIndex = Mathf.Clamp(i, 0, unitPrefabs.Length - 1);
            Vector3 spawnPosition = GetSpawnPosition(entry);
            GameObject go = Instantiate(unitPrefabs[prefabIndex], spawnPosition, Quaternion.identity);
            Unit unit = go.GetComponent<Unit>();
            if (unit == null) continue;

            unit.Initialize(entry.unitData);
            allUnits.Add(unit);
            MarkSpawnTile(entry, go);
            spawnedAny = true;
        }
        return spawnedAny;
    }

    void SpawnUnitsFromBattleData()
    {
        allUnits.Clear();
        if (BattleData.placedUnits.Count == 0) return;
        foreach (var info in BattleData.placedUnits)
        {
            if (info.unitIndex < 0 || info.unitIndex >= unitPrefabs.Length) continue;
            GameObject go = Instantiate(unitPrefabs[info.unitIndex], info.position, Quaternion.identity);
            Unit unit = go.GetComponent<Unit>();
            if (unit != null)
            {
                UnitData selectedData = GetSelectedUnitData(info.unitIndex);
                if (selectedData != null) unit.Initialize(selectedData);
                allUnits.Add(unit);
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
            int speed = Random.Range(unit.stats.minSpeed, unit.stats.maxSpeed + 1);
            unit.stats.currentTurnSpeed = speed;
            allies.Add(new TurnActor { unit = unit, speed = speed });
        }
        allies = allies.OrderByDescending(a => a.speed).ToList();

        List<TurnActor> enemies = new List<TurnActor>();
        foreach (var eu in FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None))
        {
            Enemy enemy = eu.GetComponent<Enemy>();
            if (enemy?.EnemyData == null) continue;
            int speed = Random.Range(enemy.EnemyData.minSp, enemy.EnemyData.maxSp + 1);
            if (enemy.HasTrait(TraitEffect.antArmy))
            {
                int antCount = 0;
                foreach (var otherEu in FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None))
                {
                    Enemy otherEnemy = otherEu.GetComponent<Enemy>();
                    if (otherEnemy != null && otherEnemy != enemy && otherEnemy.EnemyData?.affiliation == "개미왕국") antCount++;
                }
                speed += antCount;
            }
            enemies.Add(new TurnActor { enemyUnit = eu, speed = speed });
        }
        enemies = enemies.OrderByDescending(a => a.speed).ToList();

        teamSidePannel.ClearNameplates();
        enemySidePannel.ClearNameplates();

        int max = Mathf.Max(allies.Count, enemies.Count);
        for (int i = 0; i < max; i++)
        {
            if (i < allies.Count)
            {
                teamSidePannel.AddNameplate(allies[i].unit.UnitData);
                finalTurnOrder.Add(allies[i]);
            }
            if (i < enemies.Count)
            {
                enemySidePannel.AddNameplate(enemies[i].enemyUnit.EnemyData);
                finalTurnOrder.Add(enemies[i]);
            }
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

        if (actor.isAlly && actor.unit == null) { NextTurn(); return; }
        if (!actor.isAlly && actor.enemyUnit == null) { NextTurn(); return; }

        if (actor.isAlly)
        {
            teamSidePannel.SetPortrait(actor.unit.UnitData.portraitSprite);
            actor.unit.OnTurnStart();
            if (HandUIManager.Instance != null) HandUIManager.Instance.RefreshForUnit(actor.unit);
            if (BattleSkillButtonUI.Instance != null) BattleSkillButtonUI.Instance.Refresh();
        }
        else
        {
            enemySidePannel.SetPortrait(actor.enemyUnit.EnemyData.portraitSprite);
            if (BattleSkillButtonUI.Instance != null) BattleSkillButtonUI.Instance.Refresh();
            StartCoroutine(EnemyActAndNext(actor.enemyUnit));
        }
    }

    public void NextTurn()
    {
        if (CheckWinLossCondition()) return;

        TurnActor currentActor = GetCurrentActor();
        if (currentActor != null && currentActor.isAlly && currentActor.unit != null)
        {
            currentActor.unit.OnTurnEnd();
        }

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

        if (allUnits.Count == 0)
        {
            isGameOver = true;
            StartCoroutine(ShowGameOverPanel(false));
            return true;
        }

        EnemyUnit[] remainingEnemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        if (remainingEnemies.Length == 0)
        {
            isGameOver = true;
            StartCoroutine(ShowGameOverPanel(true));
            return true;
        }
        return false;
    }

    private IEnumerator ShowGameOverPanel(bool isWin)
    {
        yield return new WaitForSeconds(1.5f);
        if (isWin && winPanel != null) winPanel.SetActive(true);
        else if (!isWin && losePanel != null) losePanel.SetActive(true);
    }

    public void OnClickGoToMainMenu() => SceneManager.LoadScene("MainMenu");
    public void OnClickNextStage() => SceneManager.LoadScene("StageSelectScene");

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
        Enemy enemy = enemyUnit.GetComponent<Enemy>();
        Unit nearestAlly = FindNearestAlly(enemyUnit.gridPosition);
        if (nearestAlly == null) yield break;

        int ax = Mathf.RoundToInt(nearestAlly.transform.position.x + 3.5f);
        int ay = Mathf.RoundToInt(nearestAlly.transform.position.y + 3.5f);
        Vector2Int allyPos = new Vector2Int(ax, ay);

        if (IsAdjacent(enemyUnit.gridPosition, allyPos))
        {
            bool sf = enemyUnit.UseNextSkillInSequence();
            yield return StartCoroutine(AttackAlly(enemyUnit, nearestAlly, sf));
            yield break;
        }

        bool canFly = enemy?.HasTrait(TraitEffect.flight) ?? false;
        Tile targetTile = FindStepTowardAlly(enemyUnit.gridPosition, nearestAlly, canFly);
        if (targetTile == null) yield break;

        if (MapManager.Instance.tiles.TryGetValue(enemyUnit.gridPosition, out Tile oldTile))
        {
            oldTile.isOccupied = false;
            oldTile.currentUnit = null;
        }

        Vector2Int newPos = new Vector2Int(targetTile.x, targetTile.y);
        enemyUnit.gridPosition = newPos;
        targetTile.isOccupied = true;
        targetTile.currentUnit = enemyUnit.gameObject;

        yield return StartCoroutine(AnimateMove(enemyUnit.transform, targetTile.transform.position, 1f));

        bool skillFired = enemyUnit.UseNextSkillInSequence();
        if (IsAdjacent(newPos, allyPos))
            yield return StartCoroutine(AttackAlly(enemyUnit, nearestAlly, skillFired));
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

    private IEnumerator AttackAlly(EnemyUnit enemyUnit, Unit ally, bool skillFired = false)
    {
        Enemy enemy = enemyUnit.GetComponent<Enemy>();
        int dmg = enemy != null ? enemy.damage : 1;
        bool hitIgnored = ally.ShouldIgnoreIncomingHit(dmg);
        bool willDie = ally.currentHp - dmg <= 0;

        UnitMovement allyMovement = ally.movement;

        if (enemy != null && enemy.HasTrait(TraitEffect.absorption) && ally.currentHp <= Mathf.Max(1, Mathf.RoundToInt(ally.maxHp * 0.2f)))
        {
            Tile allyTile = allyMovement?.currentTile;
            if (allyTile != null)
            {
                if (MapManager.Instance.tiles.TryGetValue(enemyUnit.gridPosition, out Tile oldTile)) { oldTile.isOccupied = false; oldTile.currentUnit = null; }
                enemyUnit.gridPosition = new Vector2Int(allyTile.x, allyTile.y);
                yield return StartCoroutine(AnimateMove(enemyUnit.transform, allyTile.transform.position, 0.5f));
                allyTile.currentUnit = enemyUnit.gameObject;
            }
            enemy.CurrentHp = Mathf.Min(enemy.CurrentHp + 40, enemy.EnemyData?.maxHp ?? enemy.CurrentHp + 40);
            enemy.RecoverSt(30);
            allUnits.Remove(ally);
            Destroy(ally.gameObject);
            yield break;
        }

        if (!hitIgnored && allyMovement != null && allyMovement.currentTile != null)
        {
            Tile allyTile = allyMovement.currentTile;
            Vector2Int allyGridPos = new Vector2Int(allyTile.x, allyTile.y);
            Vector2Int enemyGridPos = enemyUnit.gridPosition;

            Vector2Int diff = allyGridPos - enemyGridPos;
            int absX = Mathf.Abs(diff.x);
            int absY = Mathf.Abs(diff.y);
            int dirX = diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1);
            int dirY = diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1);

            Vector2Int pushDir;
            if (absX == absY) pushDir = new Vector2Int(dirX, dirY);
            else if (absX > absY) pushDir = new Vector2Int(dirX, 0);
            else pushDir = new Vector2Int(0, dirY);

            Vector2Int knockBackGridPos = allyGridPos + pushDir;
            Tile knockBackTile = null;
            bool canKnockBack = MapManager.Instance != null && MapManager.Instance.tiles.TryGetValue(knockBackGridPos, out knockBackTile) && !knockBackTile.isOccupied;

            if (willDie)
            {
                if (MapManager.Instance.tiles.TryGetValue(enemyGridPos, out Tile enemyOldTile)) { enemyOldTile.isOccupied = false; enemyOldTile.currentUnit = null; }
                enemyUnit.gridPosition = allyGridPos;
                allyTile.isOccupied = true;
                allyTile.currentUnit = enemyUnit.gameObject;
                yield return StartCoroutine(AnimateMove(enemyUnit.transform, allyTile.transform.position, 0.5f));
            }
            else if (canKnockBack)
            {
                allyTile.isOccupied = false; allyTile.currentUnit = null;
                knockBackTile.isOccupied = true; knockBackTile.currentUnit = ally.gameObject;
                allyMovement.currentTile = knockBackTile;

                if (MapManager.Instance.tiles.TryGetValue(enemyGridPos, out Tile enemyOldTile)) { enemyOldTile.isOccupied = false; enemyOldTile.currentUnit = null; }
                enemyUnit.gridPosition = allyGridPos;
                allyTile.isOccupied = true;
                allyTile.currentUnit = enemyUnit.gameObject;

                Coroutine defAnim = StartCoroutine(AnimateMove(ally.transform, knockBackTile.transform.position, 0.5f));
                Coroutine atkAnim = StartCoroutine(AnimateMove(enemyUnit.transform, allyTile.transform.position, 0.5f));
                yield return defAnim;
                yield return atkAnim;
            }
            else
            {
                Vector3 origin = enemyUnit.transform.position;
                Vector3 lunge = Vector3.Lerp(origin, allyTile.transform.position, 0.4f);
                yield return StartCoroutine(AnimateMove(enemyUnit.transform, lunge, 0.25f));
                yield return StartCoroutine(AnimateMove(enemyUnit.transform, origin, 0.25f));
            }
        }

        ally.TakeDamage(dmg, enemy);
        HitEffectSpawner.SpawnImpact(ally.transform.position);

        if (enemy != null && ally != null && ally.currentHp > 0)
        {
            if (enemy.HasTrait(TraitEffect.youngDragon) && Random.value < 0.2f) ally.AddStatus(StatusEffectType.Burn, 1);
            if (enemy.HasTrait(TraitEffect.wyvernBreath) && skillFired) { int burnCount = ally.GetStatusAmount(StatusEffectType.Burn); if (burnCount > 0) { ally.RemoveStatus(StatusEffectType.Burn, 1); ally.TakeDamage(burnCount * 5); } }
            if (enemy.HasTrait(TraitEffect.dragonBreath) && skillFired) { int burnCount = ally.GetStatusAmount(StatusEffectType.Burn); if (burnCount > 0) { ally.RemoveStatus(StatusEffectType.Burn, burnCount); ally.TakeDamage(burnCount * 15); } }
            if (enemy.HasTrait(TraitEffect.stealthTentacle)) { foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None)) unit.AddStatus(StatusEffectType.Poison, 1); if (Random.value < 0.15f) ally.AddStatus(StatusEffectType.Poison, 1); if (Random.value < 0.03f) ally.AddStatus(StatusEffectType.DamageDown, 1); if (Random.value < 0.10f) ally.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(ally.maxHp * 0.05f))); }
            if (enemy != null && enemy.hasCorrosionBuff) ally.AddStatus(StatusEffectType.Corrosion, 10);
            if (enemy.HasTrait(TraitEffect.seaDisaster) && Random.value < 0.5f) ally.AddStatus(StatusEffectType.Wet, 1);
            if (enemy.hasSpreadBuff) { int splashDmg = Mathf.Max(1, dmg / 2); int allyGx = Mathf.RoundToInt(ally.transform.position.x + 3.5f); int allyGy = Mathf.RoundToInt(ally.transform.position.y + 3.5f); foreach (var otherAlly in allUnits.ToList()) { if (otherAlly == null || otherAlly == ally) continue; int ox = Mathf.RoundToInt(otherAlly.transform.position.x + 3.5f); int oy = Mathf.RoundToInt(otherAlly.transform.position.y + 3.5f); if (Mathf.Abs(ox - allyGx) + Mathf.Abs(oy - allyGy) == 1) otherAlly.TakeDamage(splashDmg, enemy); } }
        }

        if (ally.currentHp <= 0)
        {
            if (allyMovement?.currentTile != null) { allyMovement.currentTile.isOccupied = false; allyMovement.currentTile.currentUnit = null; }
            allUnits.Remove(ally);
            Destroy(ally.gameObject);
        }
    }

    private IEnumerator AnimateMove(Transform target, Vector3 destination, float duration)
    {
        Vector3 start = target.position;
        destination.z = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            target.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }
        target.position = destination;
    }

    public void KnockBack(GameObject target, Vector2Int attackerGridPos)
    {
        if (MapManager.Instance == null) return;
        int tx = Mathf.RoundToInt(target.transform.position.x + 3.5f);
        int ty = Mathf.RoundToInt(target.transform.position.y + 3.5f);
        Vector2Int targetPos = new Vector2Int(tx, ty);

        Vector2Int diff = targetPos - attackerGridPos;
        int absX = Mathf.Abs(diff.x);
        int absY = Mathf.Abs(diff.y);
        int dirX = diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1);
        int dirY = diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1);
        Vector2Int pushDir;
        if (absX == absY) pushDir = new Vector2Int(dirX, dirY);
        else if (absX > absY) pushDir = new Vector2Int(dirX, 0);
        else pushDir = new Vector2Int(0, dirY);

        Vector2Int pushPos = targetPos + pushDir;

        if (!MapManager.Instance.tiles.TryGetValue(pushPos, out Tile pushTile) || pushTile.isOccupied) return;

        if (MapManager.Instance.tiles.TryGetValue(targetPos, out Tile currentTile)) { currentTile.isOccupied = false; currentTile.currentUnit = null; }
        target.transform.position = pushTile.transform.position;
        pushTile.isOccupied = true;
        pushTile.currentUnit = target;
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

    private Tile FindStepTowardAlly(Vector2Int enemyPos, Unit ally, bool canFly = false)
    {
        int ax = Mathf.RoundToInt(ally.transform.position.x + 3.5f);
        int ay = Mathf.RoundToInt(ally.transform.position.y + 3.5f);
        Vector2Int allyPos = new Vector2Int(ax, ay);

        Tile bestTile = null; float bestDist = Vector2Int.Distance(enemyPos, allyPos);
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int candidate = enemyPos + dir;
            if (!MapManager.Instance.tiles.TryGetValue(candidate, out Tile tile) || (!canFly && tile.isOccupied)) continue;
            float dist = Vector2Int.Distance(candidate, allyPos);
            if (dist < bestDist) { bestDist = dist; bestTile = tile; }
        }
        return bestTile;
    }

    private void StartNewRound() { if (!isGameOver) GenerateTurnOrder(); }

    private UnitData GetSelectedUnitData(int unitIndex) { if (StageManager.SelectedPartyMembers != null) { for (int i = 0; i < StageManager.SelectedPartyMembers.Length; i++) { PlayerEntry entry = StageManager.SelectedPartyMembers[i]; if (entry != null && entry.unitData != null && i == unitIndex) { return entry.unitData; } } } if (PlayerSelectionData.Instance != null && PlayerSelectionData.Instance.selectedUnit != null) { return PlayerSelectionData.Instance.selectedUnit; } return null; }
    private Vector3 GetSpawnPosition(PlayerEntry entry) { Tile tile = GetTileFromEntry(entry); if (tile != null) { Vector3 tilePosition = tile.GetComponent<Collider>().bounds.center; tilePosition.z = 0f; return tilePosition; } return Vector3.zero; }
    private void MarkSpawnTile(PlayerEntry entry, GameObject unitObject) { Tile tile = GetTileFromEntry(entry); if (tile == null) return; tile.isOccupied = true; tile.currentUnit = unitObject; }
    private Tile GetTileFromEntry(PlayerEntry entry) { if (entry == null || MapManager.Instance == null) return null; int x = Mathf.Clamp((int)entry.file - 1, 0, 7); int y = Mathf.Clamp(entry.rank - 1, 0, 7); MapManager.Instance.tiles.TryGetValue(new Vector2Int(x, y), out Tile tile); return tile; }
    private void EnsureBattleSkillButtonUI() { if (FindFirstObjectByType<BattleSkillButtonUI>() != null) return; Canvas canvas = FindFirstObjectByType<Canvas>(); if (canvas == null) return; canvas.gameObject.AddComponent<BattleSkillButtonUI>(); }
}