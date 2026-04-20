/*using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("카드 프리팹")]
    public GameObject pawnCardPrefab;
    public GameObject knightCardPrefab;
    public GameObject bishopCardPrefab;

    [Header("카드 생성 위치")]
    public Transform handArea;

    [Header("현재 덱")]
    public List<CardData> deck = new List<CardData>();

    public void DrawCards(int amount)
    {
        if (handArea == null)
        {
            Debug.LogWarning("DeckManager: handArea가 연결되지 않음");
            return;
        }

        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0)
                break;

            CardData randomData = deck[Random.Range(0, deck.Count)];

            if (randomData == null)
            {
                Debug.LogWarning("DeckManager: deck 안에 null CardData가 있음");
                continue;
            }

            GameObject prefabToUse = GetCardPrefab(randomData.pieceType);

            if (prefabToUse == null)
            {
                Debug.LogWarning($"DeckManager: 프리팹이 없음 - {randomData.cardName}");
                continue;
            }

            GameObject newCardObj = Instantiate(prefabToUse, handArea);

            CardUI cardUI = newCardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(randomData);
                Debug.Log($"카드 생성 성공: {randomData.cardName}");
            }
            else
            {
                Debug.LogWarning("DeckManager: 생성된 카드 프리팹에 CardUI가 없음");
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
                return pawnCardPrefab;
        }
    }
}*/