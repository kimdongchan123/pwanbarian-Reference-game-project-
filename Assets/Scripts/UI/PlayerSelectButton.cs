using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectButton : MonoBehaviour
{
    [SerializeField]
    private UnitData unitData;
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private Image portraitImage;

    private RectTransform rectTransform;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (portraitImage == null)
        {
            portraitImage = GetComponentInChildren<Image>();
        }

        if (unitData != null)
        {
            Refresh();
        }
    }

    public void OnPointerEnter()
    {
        if (unitData == null) return;
        rectTransform.localScale = Vector3.one * 1.1f;
        UnitInspector.Instance?.ShowUnitInfo(unitData);
    }

    public void OnPointerExit()
    {
        rectTransform.localScale = Vector3.one;
        UnitInspector.Instance?.HideInfo();
    }

    public void AddToParty()
    {
        PlayerSelectManager.Instance.AddPartyMembers(unitData);
    }

    public void RemoveFromParty()
    {
        PlayerSelectManager.Instance.RemovePartyMembers(unitData);
        OnPointerExit();
    }

    public void SetUnitData(UnitData data)
    {
        unitData = data;
        if (unitData != null)
        {
            Refresh();
        }
        else
        {
            nameText.text = "--";
            ClearPortrait();
        }
    }

    private void Refresh()
    {
        nameText.text = unitData.unitName;
        ApplyPortrait();
    }

    private void ApplyPortrait()
    {
        if (portraitImage == null || unitData == null) return;

        Sprite sprite = unitData.portraitSprite != null ? unitData.portraitSprite : unitData.battleSprite;
        if (sprite == null) return;

        portraitImage.sprite = sprite;
        portraitImage.color = Color.white;
        portraitImage.type = Image.Type.Simple;
        portraitImage.preserveAspect = true;
    }

    private void ClearPortrait()
    {
        if (portraitImage == null) return;

        portraitImage.sprite = null;
        portraitImage.color = Color.clear;
    }
}
