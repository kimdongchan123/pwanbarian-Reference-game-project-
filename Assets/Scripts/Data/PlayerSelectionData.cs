using UnityEngine;

public class PlayerSelectionData : MonoBehaviour
{
    public static PlayerSelectionData Instance;

    [Header("선택된 유닛")]
    public UnitData selectedUnit;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SelectUnit(UnitData unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("선택하려는 UnitData가 null임");
            return;
        }

        selectedUnit = unit;
        Debug.Log($"선택된 유닛: {unit.unitName}");
    }

    public bool HasSelectedUnit()
    {
        return selectedUnit != null;
    }

    public void ClearSelection()
    {
        selectedUnit = null;
    }
}