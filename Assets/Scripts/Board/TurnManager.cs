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

        List<TurnActor> allActors = new List<TurnActor>();

        foreach (var unit in allUnits)
        {
            int speed = Random.Range(unit.stats.minSpeed, unit.stats.maxSpeed + 1);
            unit.stats.currentTurnSpeed = speed;
            allActors.Add(new TurnActor { unit = unit, speed = speed });
        }

        foreach (var eu in FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None))
        {
            Enemy enemy = eu.GetComponent<Enemy>();
            if (enemy?.EnemyData == null) continue;

            int speed = Random.Range(enemy.EnemyData.minSp, enemy.EnemyData.maxSp + 1);
            allActors.Add(new TurnActor { enemyUnit = eu, speed = speed });
        }

        // 🚨 [규칙 적용] SP 내림차순 정렬 -> 동점일 경우 아군(true) 우선!
        finalTurnOrder = allActors
            .OrderByDescending(a => a.speed)
            .ThenByDescending(a => a.isAlly)
            .ToList();

        Debug.Log("🏁 이번 라운드 행동 순서:");
        for (int i = 0; i < finalTurnOrder.Count; i++)
        {
            var a = finalTurnOrder[i];
            Debug.Log($"[{i + 1}등] {(a.isAlly ? "🟦아군" : "🟥적")} {a.displayName} (SP: {a.speed})");
        }
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

    // (TurnManager.cs 내부)
    private void ProcessCurrentTurn()
    {
        TurnActor actor = GetCurrentActor();
        if (actor == null) { StartNewRound(); return; }

        if (actor.isAlly && actor.unit == null) { NextTurn(); return; }
        if (!actor.isAlly && actor.enemyUnit == null) { NextTurn(); return; }

        if (actor.isAlly)
        {
            Debug.Log($"➡️ [아군 턴] {actor.displayName} — 카드를 선택하세요.");

            // 🚨 [추가된 부분] 내 턴이 돌아왔으니 카드를 1장 뽑습니다!
            if (HandUIManager.Instance != null)
            {
                HandUIManager.Instance.DrawCards(1);
            }
        }
        else
        {
            Debug.Log($"👹 [적 턴] {actor.displayName} 행동 시작");
            EnemyAI ai = actor.enemyUnit.GetComponent<EnemyAI>();
            if (ai != null) ai.PlayTurn();
            else NextTurn();
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

    private void StartNewRound()
    {
        Debug.Log("🚩 [라운드 종료] 새 라운드 시작");
        GenerateTurnOrder();
    }
}