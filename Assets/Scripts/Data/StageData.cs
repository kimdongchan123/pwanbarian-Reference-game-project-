using System.Collections.Generic;
using UnityEngine;

public enum File
{
    a = 1,
    b = 2,
    c = 3,
    d = 4,
    e = 5,
    f = 6,
    g = 7,
    h = 8,
}

public enum BattleType
{
    Normal,
    Boss,
}

[System.Serializable]
public class EnemyEntry
{
    public Enemy prefab;

    [Range(1, 8)]
    public int rank;
    public File file;
}

[CreateAssetMenu(menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageName;
    public List<EnemyEntry> enemyEntries;
    public BattleType battleType;
}
