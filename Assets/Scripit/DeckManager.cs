using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
   /* [Header("카드 더미 상태")]
    // CardData는 우리가 앞서 만든 ScriptableObject (카드 틀) 입니다.
    public List<CardData> drawPile = new List<CardData>();    // 뽑을 카드 더미
    public List<CardData> hand = new List<CardData>();        // 현재 내 손에 있는 카드들
    public List<CardData> discardPile = new List<CardData>(); // 버린 카드 더미

    [Header("설정")]
    public int maxHandSize = 5; // 패에 들 수 있는 최대 카드 수

    // 턴이 시작될 때 N장의 카드를 뽑는 함수
    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // 1. 손패가 꽉 찼는지 확인
            if (hand.Count >= maxHandSize)
            {
                Debug.Log("✋ 손패가 꽉 차서 더 이상 뽑을 수 없습니다!");
                break;
            }

            // 2. 뽑을 카드 더미가 비어있다면, 버린 카드를 섞어서 다시 가져오기
            if (drawPile.Count == 0)
            {
                ShuffleDiscardPileIntoDrawPile();
            }

            // 3. (섞었는데도) 카드가 아예 없다면 중지
            if (drawPile.Count == 0) return;

            // 4. 맨 위의 카드(0번째)를 뽑아서 손으로 가져오고, 더미에서는 지우기
            CardData drawnCard = drawPile[0];
            hand.Add(drawnCard);
            drawPile.RemoveAt(0);

            Debug.Log($"🃏 카드 뽑음: {drawnCard.cardName}");

            // TODO: 여기서 화면(UI)에 카드를 생성하는 코드를 연결할 예정입니다.
        }
    }

    // 카드를 사용하거나 턴이 끝났을 때 버리는 함수
    public void DiscardCard(CardData cardToDiscard)
    {
        if (hand.Contains(cardToDiscard))
        {
            hand.Remove(cardToDiscard);
            discardPile.Add(cardToDiscard);
            Debug.Log($"🗑️ 카드 버림: {cardToDiscard.cardName}");
        }
    }

    // 뽑을 카드가 없을 때 버린 더미를 섞어서 다시 가져오는 함수
    private void ShuffleDiscardPileIntoDrawPile()
    {
        Debug.Log("🔄 덱을 다시 섞습니다!");
        drawPile.AddRange(discardPile);
        discardPile.Clear();

        // 리스트를 무작위로 섞는 간단한 알고리즘 (Fisher-Yates Shuffle)
        for (int i = 0; i < drawPile.Count; i++)
        {
            CardData temp = drawPile[i];
            int randomIndex = Random.Range(i, drawPile.Count);
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }*/
}