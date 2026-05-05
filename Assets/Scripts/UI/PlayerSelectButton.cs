using TMPro;
using UnityEngine;

public class PlayerSelectButton : MonoBehaviour
{
    [SerializeField]
    private UnitData unitData;
    [SerializeField]
    private TextMeshProUGUI nameText;

    private RectTransform rectTransform;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (unitData != null)
        {
            nameText.text = unitData.unitName;
        }
    }

    public void OnPointerEnter()
    {
        rectTransform.localScale = Vector3.one * 1.1f;
        UnitInspector.Instance.ShowUnitInfo(unitData);
    }

    public void OnPointerExit()
    {
        rectTransform.localScale = Vector3.one;
        UnitInspector.Instance.HideInfo();
    }

    public void AddToParty()
    {
        PlayerSelectManager.Instance.AddPartyMembers(unitData);
    }

    public void RemoveFromParty()
    {
        PlayerSelectManager.Instance.RemovePartyMembers(unitData);
    }

    public void SetUnitData(UnitData data)
    {
        unitData = data;
        if (unitData != null)
        {
            nameText.text = unitData.unitName;
        }
        else
        {
            nameText.text = "";
        }
    }
}
