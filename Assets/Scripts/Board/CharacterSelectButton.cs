using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSelectButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public UnitData unitData;
    public CharacterInfoUI infoUI;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowInfo();
    }

    // 클릭하면 정보 표시 + 선택 저장
    public void OnPointerClick(PointerEventData eventData)
    {
        ShowInfo();

        if (PlayerSelectionData.Instance != null && unitData != null)
        {
            PlayerSelectionData.Instance.SelectUnit(unitData);
        }
    }

    private void ShowInfo()
    {
        if (unitData == null)
        {
            Debug.LogWarning($"{gameObject.name}: unitData가 연결되지 않음");
            return;
        }

        if (infoUI == null)
        {
            Debug.LogWarning($"{gameObject.name}: infoUI가 연결되지 않음");
            return;
        }

        infoUI.ShowUnit(unitData);
    }
}