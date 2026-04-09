using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    public string enemyName;
    public Enemy prefab;
    public bool isTopSide = true;
}

// 특성(민족의 부름 등)이 소환할 때 사용하는 싱글턴
// Inspector에서 소환 가능한 적 목록(spawnEntries)에 프리팹을 등록해야 함
public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance;

    [Header("소환 가능한 적 목록")]
    public List<EnemySpawnEntry> spawnEntries;

    private void Awake() => Instance = this;

    public Enemy SpawnEnemy(string enemyName)
    {
        EnemySpawnEntry entry = spawnEntries?.Find(e => e.enemyName == enemyName);
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"[EnemySpawnManager] '{enemyName}' 프리팹이 등록되지 않음");
            return null;
        }

        Vector2Int spawnPos = FindFreePosition(entry.isTopSide);
        if (spawnPos.x < 0)
        {
            Debug.LogWarning($"[EnemySpawnManager] '{enemyName}' 소환 실패 — 빈 칸 없음");
            return null;
        }

        Enemy spawned = Instantiate(entry.prefab);
        EnemyUnit unit = spawned.GetComponent<EnemyUnit>();
        if (unit != null)
            unit.SetGridPosition(spawnPos);
        else
            spawned.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);

        Debug.Log($"[EnemySpawnManager] {enemyName} 소환 @ {spawnPos}");
        return spawned;
    }

    private Vector2Int FindFreePosition(bool topSide)
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return new Vector2Int(-1, -1);

        int startRow = topSide ? gm.height - 1 : 0;
        int endRow   = topSide ? -1 : gm.height;
        int dir      = topSide ? -1 : 1;

        for (int row = startRow; row != endRow; row += dir)
        {
            for (int col = 0; col < gm.width; col++)
            {
                Vector2Int pos = new Vector2Int(col, row);
                if (!gm.IsBlocked(pos)) return pos;
            }
        }
        return new Vector2Int(-1, -1);
    }
}
