using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

// 👇 매니저가 알아서 영역을 감시하므로, 여기서는 클릭(IPointerClickHandler)만 남깁니다!
public class CardUI : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI patternText;

    private CardData myData;

    public void SetupCard(CardData data)
    {
        myData = data;

        if (data == null)
        {
            Debug.LogWarning("CardUI: CardData가 null입니다.");
            return;
        }

        Debug.Log($"카드 세팅됨 | 이름: {data.cardName}, 파워: {data.power}, 타입: {data.pieceType}");

        nameText.text = data.cardName;
        costText.text = data.power.ToString();
        patternText.text = data.pieceType.ToString();
    }

    // 💡 [핵심] 카드를 클릭했을 때의 로직은 그대로 유지!
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($" [{myData.cardName}] 카드 찰칵! (스크립트 클릭 성공)");

        if (PlayerActionController.Instance != null)
        {
            // 🚨 카드 데이터와 함께, 이 UI 오브젝트(this.gameObject) 자체도 넘겨줍니다!
            PlayerActionController.Instance.OnCardSelected(myData, this.gameObject);
        }
        else
        {
            Debug.LogWarning(" PlayerActionController가 씬에 없습니다!");
        }
    }

    // ✂️ (기존에 있던 OnPointerEnter, OnPointerExit는 삭제되었습니다. 매니저가 알아서 하니까요!)
}