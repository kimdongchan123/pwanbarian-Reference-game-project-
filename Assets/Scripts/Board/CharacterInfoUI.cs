using TMPro;
using UnityEngine;

public class CharacterInfoUI : MonoBehaviour
{
    public TextMeshProUGUI infoText;

    public void ShowUnit(UnitData data)
    {
        if (data == null)
        {
            ClearUI();
            return;
        }

        infoText.text =
            $"{data.unitName}\n\n" +
            $"HP: {data.maxHp}\n" +
            $"ST: {data.maxSt}\n" +
            $"SP: {data.minSp} ~ {data.maxSp}\n" +
            $"ATK: {data.minAtk} ~ {data.maxAtk}\n" +
            $"DEF: {data.def}\n\n" +
            $"소속: {data.affiliation}\n" +
            $"유형: {data.unitTypeKeyword}\n" +
            $"특이사항: {string.Join(", ", data.traitKeywords)}\n\n" +
            $"물리: {data.physicalResist}\n" +
            $"정신: {data.mentalResist}\n" +
            $"특수: {data.specialResist}\n" +
            $"최종: {data.sinResist}";
    }

    public void ClearUI()
    {
        if (infoText != null)
        {
            infoText.text = "";
        }
    }
}