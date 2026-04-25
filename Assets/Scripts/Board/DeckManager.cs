using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("덱 설정 (인스펙터에서 카드를 넣어주세요)")]
    public List<TestCardData> mainDeck = new List<TestCardData>(); // 🌟 게임 시작 시 가지고 있는 전체 카드

    [Header("카드 더미 상태")]
    public List<TestCardData> drawPile = new List<TestCardData>();   // 뽑을 카드 더미
    public List<TestCardData> hand = new List<TestCardData>();       // 현재 내 손에 있는 카드들
    public List<TestCardData> discardPile = new List<TestCardData>(); // 버린 카드 더미

    [Header("설정")]
    public int maxHandSize = 5; // 패에 들 수 있는 최대 카드 수

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 🌟 게임(또는 전투) 시작 시 덱을 초기화하고 섞는 함수
    public void InitDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();

        drawPile.AddRange(mainDeck); // 원본 덱을 복사해서 뽑을 더미에 넣음
        ShuffleDiscardPileIntoDrawPile();
    }

    // 🃏 카드를 한 장 뽑아서 데이터를 리턴하는 함수
    public TestCardData DrawCard()
    {
        if (hand.Count >= maxHandSize)
        {
            Debug.Log("✋ 손패가 꽉 차서 더 이상 뽑을 수 없습니다!");
            return null;
        }

        if (drawPile.Count == 0) ShuffleDiscardPileIntoDrawPile();
        if (drawPile.Count == 0) return null; // 버린 카드도 없으면 못 뽑음

        TestCardData drawnCard = drawPile[0];
        hand.Add(drawnCard);
        drawPile.RemoveAt(0);

        Debug.Log($"🃏 카드 뽑음: {drawnCard.cardName} (남은 덱: {drawPile.Count}장)");
        return drawnCard;
    }

    // 🗑️ 카드를 사용한 뒤 버린 카드 더미로 보내는 함수
    public void DiscardCard(TestCardData cardToDiscard)
    {
        if (hand.Contains(cardToDiscard))
        {
            hand.Remove(cardToDiscard);
            discardPile.Add(cardToDiscard);
            Debug.Log($"🗑️ 카드 버림: {cardToDiscard.cardName} (버려진 더미: {discardPile.Count}장)");
        }
    }

    // 🔄 피셔-예이츠 셔플 알고리즘
    private void ShuffleDiscardPileIntoDrawPile()
    {
        Debug.Log("🔄 덱을 다시 섞습니다!");
        drawPile.AddRange(discardPile);
        discardPile.Clear();

        for (int i = 0; i < drawPile.Count; i++)
        {
            TestCardData temp = drawPile[i];
            int randomIndex = Random.Range(i, drawPile.Count);
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }
}