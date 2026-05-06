using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

// 👇 IPointerClickHandler 라는 '클릭 감지기'를 덧붙였습니다!
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

    // 💡 [핵심] 기존의 OnCardClicked()는 지우고, 이 녀석이 대신 클릭을 받습니다!
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($" [{myData.cardName}] 카드 찰칵! (스크립트 클릭 성공)");

        if (PlayerActionController.Instance != null)
        {
            // 🚨 [수정됨] 카드 데이터와 함께, 이 UI 오브젝트(this.gameObject) 자체도 넘겨줍니다!
            PlayerActionController.Instance.OnCardSelected(myData, this.gameObject);
        }
        else
        {
            Debug.LogWarning(" PlayerActionController가 씬에 없습니다!");
        }
    }

    // 호버 감지 (기존과 동일)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (HandUIManager.Instance != null) HandUIManager.Instance.OnPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (HandUIManager.Instance != null) HandUIManager.Instance.OnPointerExit(eventData);
    }
}