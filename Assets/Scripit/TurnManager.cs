using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance; // 어디서든 접근 가능하게 싱글톤 설정
    [Header("기물 프리팹 리스트")]
    public GameObject[] unitPrefabs;
    public List<Unit> allUnits = new List<Unit>();
    public List<Unit> finalTurnOrder = new List<Unit>();

    private int currentTurnIndex = 0; // 현재 누구 차례인지 가리키는 번호

    void Awake() => Instance = this;
    void Start()
    {
        SpawnUnitsFromBattleData();
        GenerateTurnOrder(); // 유닛 소환 후 바로 순서 결정!
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
            // 1. 프리팹 소환
            GameObject go = Instantiate(unitPrefabs[info.unitIndex], info.position, Quaternion.identity);

            // 2. Unit 컴포넌트 가져오기
            Unit unit = go.GetComponent<Unit>();
            if (unit != null)
            {
                allUnits.Add(unit); // 🎯 드디어 allUnits 명단에 실제 유닛이 등록됩니다!
            }
        }
    }
    // 턴 시작 시 호출: 전체 순서를 결정함
    public void GenerateTurnOrder()
    {
        currentTurnIndex = 0;
        finalTurnOrder.Clear();

        // 1. 모든 유닛 속도 주사위 굴림
        foreach (var unit in allUnits)
        {
            unit.stats.currentTurnSpeed = Random.Range(unit.stats.minSpeed, unit.stats.maxSpeed + 1);
        }

        // 2. 속도별 그룹화 및 정렬
        var speedGroups = allUnits
            .GroupBy(u => u.stats.currentTurnSpeed)
            .OrderByDescending(g => g.Key);

        foreach (var group in speedGroups)
        {
            List<Unit> allies = group.Where(u => u.isAlly).OrderBy(u => u.formationIndex).ToList();
            List<Unit> enemies = group.Where(u => !u.isAlly).ToList();

            if (allies.Count > 0 && enemies.Count > 0)
            {
                // 🎲 50% 확률로 진영 우선순위 결정
                if (Random.value > 0.5f) { finalTurnOrder.AddRange(allies); finalTurnOrder.AddRange(enemies); }
                else { finalTurnOrder.AddRange(enemies); finalTurnOrder.AddRange(allies); }
            }
            else
            {
                finalTurnOrder.AddRange(allies);
                finalTurnOrder.AddRange(enemies);
            }
        }

        Debug.Log("🏁 이번 턴 행동 순서가 확정되었습니다.");
    }

    // 현재 차례인 유닛을 반환하는 함수
    public Unit GetCurrentUnit()
    {
        if (currentTurnIndex < finalTurnOrder.Count)
            return finalTurnOrder[currentTurnIndex];
        return null;
    }

    // 다음 사람으로 넘기기
    public void NextTurn()
    {
        // 1. 다음 기물로 인덱스 증가 (개별 기물의 턴 종료)
        currentTurnIndex++;

        // 2. 모든 기물이 한 번씩 행동했는지 체크 (사용자님이 정의한 '1턴'의 끝)
        if (currentTurnIndex >= finalTurnOrder.Count)
        {
            Debug.Log("🚩 [라운드 종료] 모든 기물이 행동을 마쳤습니다. 새로운 라운드를 준비합니다.");
            StartNewRound();
        }
        else
        {
            // 아직 행동할 기물이 남았다면 다음 기물에게 기회를 줍니다.
            Unit nextUnit = GetCurrentUnit();
            Debug.Log($"➡️ [다음 턴] 이제 {nextUnit.unitName}의 차례입니다.");

            // UI 업데이트나 카메라 포커스 이동 등을 여기서 처리할 수 있습니다.
        }
    }

    private void StartNewRound()
    {
        // 라운드(1턴 전체)가 끝났으므로 인덱스를 초기화합니다.
        currentTurnIndex = 0;

        // 다시 속도 주사위를 굴려 새로운 행동 순서를 결정합니다.
        GenerateTurnOrder();

        Debug.Log("✨ [새 라운드 시작] 속도 주사위를 다시 굴려 새로운 순서가 결정되었습니다!");
    }
}
