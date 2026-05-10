using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DroppableUI : MonoBehaviour, IPointerEnterHandler, IDropHandler, IPointerExitHandler
{
	[SerializeField]
	private bool active = true;

    private Image image;
    private RectTransform rect;
    private Color originalColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        originalColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
		if (!active) 
		{
			image.color = Color.red;
			return;
		}
        image.color = Color.green;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = originalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
		if (!active) return;
        if (eventData.pointerDrag != null)
        {
			
			if (transform.childCount > 0)
			{
				Transform ch = transform.GetChild(0);
				Transform targetParent = eventData.pointerDrag.GetComponent<DraggableUI>().PreviousParent;
				ch.SetParent(targetParent);
				ch.GetComponent<RectTransform>().position = targetParent.GetComponent<RectTransform>().position;
			}
            eventData.pointerDrag.transform.SetParent(transform);
            eventData.pointerDrag.GetComponent<RectTransform>().position = rect.position;
        }
    }
}
