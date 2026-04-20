using UnityEngine;

public class BattleSceneSetup : MonoBehaviour
{
    [Header("플레이어")]
    public BattleUnit playerPrefab;
    public Transform playerSpawnPoint;

    [Header("적")]
    public BattleUnit[] enemyPrefabs;
    public Transform[] enemySpawnPoints;

    [Header("자동 생성된 플레이어")]
    public BattleUnit spawnedPlayer;

    private void Start()
    {
        SpawnPlayer();
        SpawnEnemies();
    }

    private void SpawnPlayer()
    {
        if (PlayerSelectionData.Instance == null)
        {
            Debug.LogWarning("PlayerSelectionData가 없음");
            return;
        }

        if (PlayerSelectionData.Instance.selectedUnit == null)
        {
            Debug.LogWarning("선택된 UnitData가 없음");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogWarning("playerPrefab이 연결되지 않음");
            return;
        }

        Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;

        spawnedPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        spawnedPlayer.Initialize(PlayerSelectionData.Instance.selectedUnit);
        spawnedPlayer.team = BattleTeam.Ally;
        spawnedPlayer.name = PlayerSelectionData.Instance.selectedUnit.unitName;

        Debug.Log($"플레이어 생성 완료: {spawnedPlayer.data.unitName}");
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.Log("적 프리팹이 없음");
            return;
        }

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyPrefabs[i] == null)
                continue;

            Vector3 spawnPos = Vector3.zero;

            if (enemySpawnPoints != null && i < enemySpawnPoints.Length && enemySpawnPoints[i] != null)
            {
                spawnPos = enemySpawnPoints[i].position;
            }
            else
            {
                spawnPos = new Vector3(3f + i * 2f, 0f, 0f);
            }

            BattleUnit enemy = Instantiate(enemyPrefabs[i], spawnPos, Quaternion.identity);
            enemy.team = BattleTeam.Enemy;
        }
    }
}