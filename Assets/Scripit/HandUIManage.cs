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
        // 1. 전투 시작 시 자동으로 카드 3장 뽑기
        // (기물 소환 등이 끝날 시간을 벌기 위해 아주 잠깐 대기 후 실행)
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
        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0) break;
            GameObject newCardObj = Instantiate(cardPrefab, handArea);
            TestCardData randomData = deck[Random.Range(0, deck.Count)];
            newCardObj.GetComponent<CardUI>().SetupCard(randomData);
        }
    }

}