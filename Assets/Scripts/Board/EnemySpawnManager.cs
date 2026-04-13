// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Board/ 신규 추가
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    public string enemyName;
    public Enemy prefab;
    public bool isTopSide = true;
    public bool spawnOnStart = false;
    public int spawnCount = 1;
    public int spawnX = 0;
}

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance;

    [Header("소환 가능한 적 목록")]
    public List<EnemySpawnEntry> spawnEntries;

    private void Awake() => Instance = this;

    private void Start()
    {
        foreach (var entry in spawnEntries)
        {
            if (!entry.spawnOnStart) continue;
            for (int i = 0; i < entry.spawnCount; i++)
                SpawnEnemyAt(entry);
        }
    }

    public Enemy SpawnEnemy(string enemyName)
    {
        EnemySpawnEntry entry = spawnEntries?.Find(e => e.enemyName == enemyName);
        return SpawnEnemyAt(entry);
    }

    private Enemy SpawnEnemyAt(EnemySpawnEntry entry)
    {
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"[EnemySpawnManager] '{entry?.enemyName}' 프리팹이 등록되지 않음");
            return null;
        }

        Tile spawnTile = FindFreeTileInColumn(entry.isTopSide, entry.spawnX);
        if (spawnTile == null)
        {
            Debug.LogWarning($"[EnemySpawnManager] '{entry.enemyName}' 소환 실패 — x={entry.spawnX} 열에 빈 칸 없음");
            return null;
        }

        Vector2Int spawnPos = new Vector2Int(spawnTile.x, spawnTile.y);
        Vector3 worldPos = spawnTile.transform.position;
        worldPos.z = 0f;

    Enemy spawned = Instantiate(entry.prefab);
    EnemyUnit unit = spawned.GetComponent<EnemyUnit>();
    if (unit != null)
    {
        unit.gridPosition = spawnPos;
        unit.transform.position = worldPos;
        unit.hasMoved = true;
    }
    else
    {
        spawned.transform.position = worldPos;
    }
        // Tile 점유 상태 업데이트
        spawnTile.isOccupied = true;
        spawnTile.currentUnit = spawned.gameObject;

        Debug.Log($"[EnemySpawnManager] {entry.enemyName} 소환 @ {spawnPos}");
        return spawned;
    }

    // MapManager.tiles에서 지정 x열의 빈 타일을 찾아 반환
    private Tile FindFreeTileInColumn(bool topSide, int col)
    {
        if (MapManager.Instance == null || MapManager.Instance.tiles == null) return null;

        Tile result = null;
        int bestRow = topSide ? int.MinValue : int.MaxValue;

        foreach (var kv in MapManager.Instance.tiles)
        {
            if (kv.Key.x != col) continue;
            if (kv.Value.isOccupied) continue;

            int row = kv.Key.y;
            if (topSide && row > bestRow) { bestRow = row; result = kv.Value; }
            else if (!topSide && row < bestRow) { bestRow = row; result = kv.Value; }
        }

        return result;
    }
}
