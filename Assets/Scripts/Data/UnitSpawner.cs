
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public BattleUnit unitPrefab;
    public UnitData[] unitDatas;
    public BattleTeam team;
    public Transform spawnParent;

    public BattleUnit[] spawnedUnits;

    private void Start()
    {
        SpawnUnits();
    }

    public void SpawnUnits()
    {
        if (unitDatas == null || unitDatas.Length == 0)
        {
            Debug.LogWarning("생성할 UnitData가 없음");
            return;
        }

        spawnedUnits = new BattleUnit[unitDatas.Length];

        for (int i = 0; i < unitDatas.Length; i++)
        {
            BattleUnit newUnit = Instantiate(unitPrefab);

            newUnit.transform.position = new Vector3(i * 2.5f, 0f, 0f);

            if (spawnParent != null)
            {
                newUnit.transform.SetParent(spawnParent);
            }

            newUnit.Initialize(unitDatas[i]);
            newUnit.team = team;
            newUnit.name = unitDatas[i].unitName;

            spawnedUnits[i] = newUnit;
        }
    }
}
