using UnityEngine;

public class BattleSpawner : MonoBehaviour
{
    [Header("기물 프리팹 리스트")]
    [Tooltip("배치 씬(PlacementManager)에서 넣었던 것과 똑같은 순서로 넣어주세요!")]
    public GameObject[] unitPrefabs;

    void Start()
    {
        // 씬이 시작되자마자 기물들을 소환합니다.
        SpawnUnits();
    }

    public void SpawnUnits()
    {
        // 1. 바구니가 비어있는지 먼저 확인합니다.
        if (BattleData.placedUnits == null || BattleData.placedUnits.Count == 0)
        {
            Debug.LogWarning(" 불러올 기물 데이터가 없습니다! (배치 씬을 거치지 않고 바로 전투 씬을 실행했나요?)");
            return;
        }

        Debug.Log($" 총 {BattleData.placedUnits.Count}개의 아군 기물을 전장에 소환합니다...");

        // 2. 바구니 안에 있는 정보(UnitInfo)를 하나씩 꺼내서 반복합니다.
        foreach (BattleData.UnitInfo info in BattleData.placedUnits)
        {
            // 혹시 모를 에러 방지 (프리팹 리스트 범위 안에 있는지 검사)
            if (info.unitIndex >= 0 && info.unitIndex < unitPrefabs.Length)
            {
                // 소환할 프리팹을 결정합니다.
                GameObject prefabToSpawn = unitPrefabs[info.unitIndex];

                //  기물 생성! (저장된 그 위치에 그대로 생성합니다)
                Instantiate(prefabToSpawn, info.position, Quaternion.identity);

                Debug.Log($" [{prefabToSpawn.name}] 소환 완료! 위치: {info.position}");
            }
            else
            {
                Debug.LogError($" 소환 에러: 존재하지 않는 프리팹 인덱스입니다 ({info.unitIndex}). 프리팹 리스트 순서를 확인해 주세요!");
            }
        }
    }
}