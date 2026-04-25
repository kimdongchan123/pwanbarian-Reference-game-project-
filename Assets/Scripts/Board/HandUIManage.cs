using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems; // 마우스 호버 감지를 위해 필요

public class HandUIManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static HandUIManager Instance;

    [Header("UI 연결")]
    public GameObject cardPrefab;
    public RectTransform handArea; // RectTransform으로 변경 (위치 조절용)

    [Header("덱 데이터")]
    public List<TestCardData> deck = new List<TestCardData>();

    [Header("슬라이딩 설정")]
    public float hiddenY = -150f;   // 내려가 있을 때의 Y 좌표
    public float visibleY = 50f;    // 올라왔을 때의 Y 좌표
    public float slideSpeed = 10f;  // 올라오는 속도

    private float targetY;          // 현재 목표로 하는 Y 좌표

    void Awake()
    {
        Instance = this;
        targetY = hiddenY; // 처음에는 숨겨진 상태
    }

    void Start()
    {
        // 🚨 덱 매니저한테 덱 세팅하라고 명령!
        DeckManager.Instance.InitDeck();
        StartCoroutine(AutoDrawAtStart(3));
    }

    IEnumerator AutoDrawAtStart(int amount)
    {
        yield return new WaitForSeconds(0.5f);
        DrawCards(amount);
    }

    void Update()
    {
        // 2. 부드럽게 목표 위치로 이동 (슬레이 더 스파이어 방식)
        Vector2 currentPos = handArea.anchoredPosition;
        float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * slideSpeed);
        handArea.anchoredPosition = new Vector2(currentPos.x, newY);
    }

    // 마우스가 핸드 영역에 들어오면 실행
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("마우스 들어옴! 쑤욱 올라갑니다!");
        targetY = visibleY; // 위로 올라오기
    }

    // 마우스가 핸드 영역에서 나가면 실행
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("마우스 나감! 다시 숨습니다.");
        targetY = hiddenY; // 아래로 숨기기
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // 🚨 덱 매니저에게 진짜 카드를 뽑아달라고 요청! (손패가 꽉 찼으면 null을 줍니다)
            TestCardData drawnData = DeckManager.Instance.DrawCard();

            if (drawnData != null)
            {
                // 화면에 카드 껍데기를 만들고 데이터를 예쁘게 넣어줍니다.
                GameObject newCardObj = Instantiate(cardPrefab, handArea);
                newCardObj.GetComponent<CardUI>().SetupCard(drawnData);
            }
        }
    }

}