// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Board/ 신규 추가
//
// 사용법:
//   전투 씬의 TurnManager와 같은 오브젝트(또는 별도 오브젝트)에 붙여두면
//   TurnManager.Start()의 SpawnUnitsFromBattleData() 직후 적이 자동 소환됩니다.
//
//   또는 TurnManager.Start()에서 직접 호출:
//     EnemyBattleSetup.Instance.SpawnEnemies();
using System.Collections.Generic;
using UnityEngine;

public class EnemyBattleSetup : MonoBehaviour
{
    public static EnemyBattleSetup Instance;

    [System.Serializable]
    public class EnemySetupEntry
    {
        public string enemyName;   // EnemySpawnManager의 enemyName과 일치해야 함
        public int spawnX;         // 배치할 x 열
        public bool isTopSide = true;
    }

    [Header("전투 시작 시 배치할 적 목록")]
    public List<EnemySetupEntry> enemySetup;

    private void Awake()
    {
        Instance = this;
    }

    // TurnManager.Start() 또는 게임 시작 버튼 이벤트에서 호출
    public void SpawnEnemies()
    {
        if (EnemySpawnManager.Instance == null)
        {
            Debug.LogWarning("[EnemyBattleSetup] EnemySpawnManager가 씬에 없음");
            return;
        }

        foreach (var entry in enemySetup)
        {
            Enemy spawned = EnemySpawnManager.Instance.SpawnEnemy(entry.enemyName);
            if (spawned == null)
                Debug.LogWarning($"[EnemyBattleSetup] {entry.enemyName} 소환 실패");
        }

        Debug.Log($"[EnemyBattleSetup] 적 배치 완료 — {enemySetup.Count}종");
    }
}
