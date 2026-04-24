using System.Collections.Generic;
using UnityEngine;

public enum Col
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

[System.Serializable]
public class EnemyEntry
{
    public Enemy prefab;

    [Range(1, 8)]
    public int row;
    public Col column;
}

[CreateAssetMenu(menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageName;
    public List<EnemyEntry> enemyEntries;
}
