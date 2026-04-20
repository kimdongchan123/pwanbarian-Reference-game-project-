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
        Debug.Log("마우스 들어옴! 쑤욱 올라갑니다!");
        targetY = visibleY;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("마우스 나감! 다시 숨습니다.");
        targetY = hiddenY;
    }

    public void DrawCards(int amount)
    {
        if (handArea == null)
        {
            Debug.LogWarning("HandUIManager: handArea가 연결되지 않음");
            return;
        }

        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0)
            {
                Debug.LogWarning("HandUIManager: deck이 비어 있음");
                break;
            }

            CardData randomData = deck[Random.Range(0, deck.Count)];

            if (randomData == null)
            {
                Debug.LogWarning("HandUIManager: deck 안에 null CardData가 있음");
                continue;
            }

            GameObject prefabToUse = GetCardPrefab(randomData.pieceType);

            if (prefabToUse == null)
            {
                Debug.LogWarning($"HandUIManager: 프리팹이 없음 - {randomData.cardName}");
                continue;
            }

            GameObject newCardObj = Instantiate(prefabToUse, handArea);

            CardUI cardUI = newCardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                Debug.Log($"HandUIManager: 카드 세팅 - {randomData.cardName}");
                cardUI.SetupCard(randomData);
            }
            else
            {
                Debug.LogWarning("HandUIManager: 생성된 카드 프리팹에 CardUI가 없음");
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