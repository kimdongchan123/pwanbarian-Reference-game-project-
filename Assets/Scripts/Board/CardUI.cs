using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

// 👇 IPointerClickHandler 라는 '클릭 감지기'를 덧붙였습니다!
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI patternText;

    private TestCardData myData;

    public void SetupCard(TestCardData data)
    {
        myData = data;
        nameText.text = data.cardName;
        costText.text = data.cost.ToString();
        patternText.text = data.pattern.ToString();
    }

    // 💡 [핵심] 기존의 OnCardClicked()는 지우고, 이 녀석이 대신 클릭을 받습니다!
    // (CardUI.cs 내부)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"🎯 [{myData.cardName}] 카드 찰칵! (스크립트 클릭 성공)");

        if (PlayerActionController.Instance != null)
        {
            // 🚨 [수정된 부분] 데이터(myData)와 함께 내 몸통(this.gameObject)도 넘겨줍니다!
            PlayerActionController.Instance.OnCardSelected(myData, this.gameObject);
        }
        else
        {
            Debug.LogWarning("⚠️ PlayerActionController가 씬에 없습니다!");
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