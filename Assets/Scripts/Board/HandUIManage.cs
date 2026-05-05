using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class HandUIManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static HandUIManager Instance;

    [Header("UI 연결")]
    public GameObject pawnCardPrefab;
    public GameObject knightCardPrefab;
    public GameObject bishopCardPrefab;
    public RectTransform handArea;

    [Header("덱 데이터")]
    public List<CardData> deck = new List<CardData>();

    [Header("슬라이딩 설정")]
    public float hiddenY = -150f;
    public float visibleY = 50f;
    public float slideSpeed = 10f;

    private float targetY;

    private void Awake()
    {
        Instance = this;
        targetY = hiddenY;
    }

    private void Start()
    {
        StartCoroutine(AutoDrawAtStart(3));
    }

    private IEnumerator AutoDrawAtStart(int amount)
    {
        yield return new WaitForSeconds(0.5f);
        DrawCards(amount);
    }

    private void Update()
    {
        if (handArea == null) return;

        Vector2 currentPos = handArea.anchoredPosition;
        float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * slideSpeed);
        handArea.anchoredPosition = new Vector2(currentPos.x, newY);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Debug.Log("마우스 들어옴! 쑤욱 올라갑니다!");
        targetY = visibleY;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Debug.Log("마우스 나감! 다시 숨습니다.");
        targetY = hiddenY;
    }

    // 👇 (수정) targetIndex = -1 이라는 '선택형 번호표'를 추가했습니다!
    public void DrawCards(int amount, int targetIndex = -1)
    {
        if (handArea == null)
        {
            Debug.LogWarning("HandUIManager: handArea가 연결되지 않음");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0)
            {
                Debug.LogWarning("HandUIManager: deck이 비어 있음");
                break;
            }

            CardData randomData = deck[Random.Range(0, deck.Count)];
            if (randomData == null) continue;

            GameObject prefabToUse = GetCardPrefab(randomData.pieceType);
            if (prefabToUse == null) continue;

            GameObject newCardObj = Instantiate(prefabToUse, handArea);

            // 🌟 [핵심 마법] 만약 번호표를 받았다면, 새 카드를 무조건 맨 오른쪽이 아니라 '그 번호 자리'로 끼워 넣습니다!
            if (targetIndex != -1)
            {
                newCardObj.transform.SetSiblingIndex(targetIndex);
            }

            CardUI cardUI = newCardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(randomData);
            }
        }
    }

    private GameObject GetCardPrefab(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return pawnCardPrefab;

            case PieceType.Knight:
                return knightCardPrefab;

            case PieceType.Bishop:
                return bishopCardPrefab;

            default:
                Debug.LogWarning($"HandUIManager: 지원하지 않는 PieceType - {pieceType}");
                return pawnCardPrefab;
        }
    }
}