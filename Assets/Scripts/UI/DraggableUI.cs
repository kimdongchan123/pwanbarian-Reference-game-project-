using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform canvas;
    private Transform previousParent;
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Image image;

    private UnitData unitData;

    public Transform PreviousParent => previousParent;
    public UnitData UnitData => unitData;


    private void Awake()
    {
        canvas = FindFirstObjectByType<Canvas>().transform;
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
    }

    public void SetUnitData(UnitData data)
    {
        unitData = data;

        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (unitData == null)
        {
            Debug.LogWarning($"[DraggableUI] {gameObject.name}: UnitData is null.");
            return;
        }

        if (image == null)
        {
            Debug.LogWarning($"[DraggableUI] {gameObject.name}: Image component is missing.");
            return;
        }

        Sprite sprite = unitData.battleSprite != null ? unitData.battleSprite : unitData.portraitSprite;
        if (sprite == null)
        {
            Debug.LogWarning($"[DraggableUI] {unitData.unitName}: battleSprite/portraitSprite is empty. Using fallback color.");
            return;
        }

        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        Debug.Log($"[DraggableUI] Applied sprite '{sprite.name}' for {unitData.unitName}.");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        previousParent = transform.parent;

        transform.SetParent(canvas);
        transform.SetAsLastSibling();

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (transform.parent == canvas)
        {
            transform.SetParent(previousParent);
            rect.position = previousParent.GetComponent<RectTransform>().position;
        }

        previousParent = null;

        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;
    }
}
