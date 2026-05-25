using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandUIManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static HandUIManager Instance;

    [Header("UI")]
    public GameObject pawnCardPrefab;
    public GameObject knightCardPrefab;
    public GameObject bishopCardPrefab;
    public GameObject rookCardPrefab;
    public GameObject queenCardPrefab;
    public GameObject kingCardPrefab;
    public RectTransform handArea;

    [Header("Deck")]
    public List<CardData> deck = new List<CardData>();
    public int initialDrawCount = 3;

    [Header("Slide")]
    public float hiddenY = -150f;
    public float visibleY = 50f;
    public float slideSpeed = 10f;

    private readonly List<CardData> drawPile = new List<CardData>();
    private UnitData activeUnitData;
    private float targetY;

    private void Awake()
    {
        Instance = this;
        targetY = hiddenY;
    }

    private void Start()
    {
        StartCoroutine(AutoDrawAtStart(initialDrawCount));
    }

    private IEnumerator AutoDrawAtStart(int amount)
    {
        yield return new WaitForSeconds(0.5f);

        if (handArea != null && handArea.childCount > 0)
        {
            yield break;
        }

        LoadSelectedUnitDeck();
        ShuffleDrawPile();
        DrawCards(amount);
    }

    public void RefreshForUnit(Unit unit)
    {
        if (unit == null || unit.data == null)
        {
            return;
        }

        if (activeUnitData == unit.data && handArea != null && handArea.childCount > 0)
        {
            return;
        }

        activeUnitData = unit.data;
        LoadDeckFromUnitData(activeUnitData);
        ShuffleDrawPile();
        ClearHand();
        DrawCards(initialDrawCount);
    }

    private void Update()
    {
        if (handArea == null) return;

        Vector2 currentPos = handArea.anchoredPosition;
        float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * slideSpeed);
        handArea.anchoredPosition = new Vector2(currentPos.x, newY);
    }

    private void LoadSelectedUnitDeck()
    {
        UnitData sourceDeck = GetCurrentPlayerUnitData();

        if (sourceDeck == null)
        {
            Debug.LogWarning("HandUIManager: player unit deck source is missing. Using inspector deck.");
            drawPile.Clear();
            drawPile.AddRange(deck);
            return;
        }

        activeUnitData = sourceDeck;
        LoadDeckFromUnitData(sourceDeck);
    }

    private void LoadDeckFromUnitData(UnitData sourceDeck)
    {
        deck.Clear();

        CardData[] selectedDeck = sourceDeck.deck;
        if (selectedDeck != null)
        {
            deck.AddRange(selectedDeck);
        }

        drawPile.Clear();
        drawPile.AddRange(deck);

        Debug.Log($"HandUIManager: loaded {deck.Count} cards from {sourceDeck.unitName}.");
    }

    private void ClearHand()
    {
        if (handArea == null) return;

        for (int i = handArea.childCount - 1; i >= 0; i--)
        {
            Destroy(handArea.GetChild(i).gameObject);
        }
    }

    private UnitData GetCurrentPlayerUnitData()
    {
        if (TurnManager.Instance != null)
        {
            Unit currentUnit = TurnManager.Instance.GetCurrentUnit();
            if (currentUnit != null && currentUnit.isAlly && currentUnit.data != null)
            {
                return currentUnit.data;
            }
        }

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units)
        {
            if (unit != null && unit.isAlly && unit.data != null)
            {
                return unit.data;
            }
        }

        if (StageManager.SelectedPartyMembers != null)
        {
            foreach (PlayerEntry entry in StageManager.SelectedPartyMembers)
            {
                if (entry != null && entry.unitData != null)
                {
                    return entry.unitData;
                }
            }
        }

        if (PlayerSelectionData.Instance != null && PlayerSelectionData.Instance.selectedUnit != null)
        {
            return PlayerSelectionData.Instance.selectedUnit;
        }

        return null;
    }

    private void ShuffleDrawPile()
    {
        for (int i = 0; i < drawPile.Count; i++)
        {
            int randomIndex = Random.Range(i, drawPile.Count);
            CardData temp = drawPile[i];
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetY = visibleY;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetY = hiddenY;
    }

    public void DrawCards(int amount, int targetIndex = -1)
    {
        if (handArea == null)
        {
            Debug.LogWarning("HandUIManager: handArea is missing.");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            if (drawPile.Count == 0)
            {
                drawPile.AddRange(deck);
                ShuffleDrawPile();
            }

            if (drawPile.Count == 0)
            {
                Debug.LogWarning("HandUIManager: deck is empty.");
                break;
            }

            CardData randomData = drawPile[0];
            drawPile.RemoveAt(0);
            if (randomData == null) continue;

            GameObject prefabToUse = GetCardPrefab(randomData.pieceType);
            if (prefabToUse == null) continue;

            GameObject newCardObj = Instantiate(prefabToUse, handArea);

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
                return knightCardPrefab != null ? knightCardPrefab : pawnCardPrefab;
            case PieceType.Bishop:
                return bishopCardPrefab != null ? bishopCardPrefab : pawnCardPrefab;
            case PieceType.Rook:
                return rookCardPrefab != null ? rookCardPrefab : pawnCardPrefab;
            case PieceType.Queen:
                return queenCardPrefab != null ? queenCardPrefab : pawnCardPrefab;
            case PieceType.King:
                return kingCardPrefab != null ? kingCardPrefab : pawnCardPrefab;
            default:
                Debug.LogWarning($"HandUIManager: unsupported PieceType - {pieceType}");
                return pawnCardPrefab;
        }
    }
}
