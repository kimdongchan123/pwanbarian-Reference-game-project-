using TMPro;
using UnityEngine;

public class BattleSelectedUnitUI : MonoBehaviour
{
    public TMP_Text selectedNameText;
    public TMP_Text selectedStatText;

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (PlayerSelectionData.Instance == null || PlayerSelectionData.Instance.selectedUnit == null)
        {
            if (selectedNameText != null) selectedNameText.text = "선택된 캐릭터 없음";
            if (selectedStatText != null) selectedStatText.text = "";
            return;
        }

        UnitData unit = PlayerSelectionData.Instance.selectedUnit;

        if (selectedNameText != null)
        {
            selectedNameText.text = unit.unitName;
        }

        if (selectedStatText != null)
        {
            selectedStatText.text =
                $"HP {unit.maxHp}\n" +
                $"ST {unit.maxSt}\n" +
                $"ATK {unit.minAtk}~{unit.maxAtk}\n" +
                $"DEF {unit.def}";
        }
    }
}