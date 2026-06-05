using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandUIManager : MonoBehaviour
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

    [Header("호버링 영역 설정")]
    [Tooltip("에디터에서 '가장 크게 올라왔을 때(사진2)' 기준으로 영역을 덮어주세요.")]
    public RectTransform triggerZone;

    // 🌟 [핵심 추가] 카드가 숨겨져 있을 때 감지할 좁은 구역의 높이
    [Tooltip("카드가 내려가 있을 때 마우스를 감지할 맨 아래 영역의 높이입니다. (사진1의 빨간 구역)")]
    public float smallZoneHeight = 150f;

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
    private bool isForcedHidden = false;

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
        if (handArea != null && handArea.childCount > 0) yield break;

        LoadSelectedUnitDeck();
        ShuffleDrawPile();
        DrawCards(amount);
    }

    public void RefreshForUnit(Unit unit)
    {
        if (unit == null || unit.data == null) return;
        if (activeUnitData == unit.data && handArea != null && handArea.childCount > 0) return;

        activeUnitData = unit.data;
        LoadDeckFromUnitData(activeUnitData);
        ShuffleDrawPile();
        ClearHand();
        DrawCards(initialDrawCount);
    }

    // 카드를 선택했을 때 강제로 내립니다.
    public void ForceHideCards()
    {
        isForcedHidden = true;
        targetY = hiddenY;
    }

    private Camera GetUICamera()
    {
        if (triggerZone == null) return null;
        Canvas canvas = triggerZone.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return canvas.worldCamera;
        }
        return null;
    }

    private void Update()
    {
        if (handArea == null) return;

        if (triggerZone != null)
        {
            Camera cam = GetUICamera();
            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            // 마우스 좌표를 triggerZone 기준의 로컬 좌표계로 변환합니다.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(triggerZone, mousePos, cam, out Vector2 localPoint);

            bool isMouseInZone = false;
            Rect baseRect = triggerZone.rect;

            // 🌟 능동적 영역 변환 로직 (다이나믹 호버링)
            if (targetY == visibleY && !isForcedHidden)
            {
                // 1. 카드가 올라와 있을 때: 큼지막한 전체 구역(사진2)을 검사합니다.
                isMouseInZone = baseRect.Contains(localPoint);
            }
            else
            {
                // 2. 카드가 내려가 있거나(초기 상태), 카드를 선택해서 강제로 내려갔을 때
                // triggerZone의 '맨 아래쪽 smallZoneHeight' 만큼의 얇은 구역(사진1)만 검사합니다.
                Rect smallRect = baseRect;
                smallRect.yMax = smallRect.yMin + smallZoneHeight;
                isMouseInZone = smallRect.Contains(localPoint);
            }

            // 영역 안에 마우스가 들어오면 무조건 강제 숨김을 해제하고 카드를 올립니다.
            if (isMouseInZone)
            {
                isForcedHidden = false;
                targetY = visibleY;
            }
            else
            {
                targetY = hiddenY;
            }
        }

        Vector2 currentPos = handArea.anchoredPosition;
        float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * slideSpeed);
        handArea.anchoredPosition = new Vector2(currentPos.x, newY);
    }

    private void LoadSelectedUnitDeck()
    {
        UnitData sourceDeck = GetCurrentPlayerUnitData();
        if (sourceDeck == null)
        {
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
        if (selectedDeck != null) deck.AddRange(selectedDeck);

        drawPile.Clear();
        drawPile.AddRange(deck);
    }

    private void ClearHand()
    {
        if (handArea == null) return;
        for (int i = handArea.childCount - 1; i >= 0; i--) Destroy(handArea.GetChild(i).gameObject);
    }

    private UnitData GetCurrentPlayerUnitData()
    {
        if (TurnManager.Instance != null)
        {
            Unit currentUnit = TurnManager.Instance.GetCurrentUnit();
            if (currentUnit != null && currentUnit.isAlly && currentUnit.data != null) return currentUnit.data;
        }

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units) if (unit != null && unit.isAlly && unit.data != null) return unit.data;

        if (StageManager.SelectedPartyMembers != null)
        {
            foreach (PlayerEntry entry in StageManager.SelectedPartyMembers)
                if (entry != null && entry.unitData != null) return entry.unitData;
        }

        if (PlayerSelectionData.Instance != null && PlayerSelectionData.Instance.selectedUnit != null) return PlayerSelectionData.Instance.selectedUnit;
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

    public void DrawCards(int amount, int targetIndex = -1)
    {
        if (handArea == null) return;

        for (int i = 0; i < amount; i++)
        {
            if (drawPile.Count == 0) { drawPile.AddRange(deck); ShuffleDrawPile(); }
            if (drawPile.Count == 0) break;

            CardData randomData = drawPile[0];
            drawPile.RemoveAt(0);
            if (randomData == null) continue;

            GameObject prefabToUse = GetCardPrefab(randomData.pieceType);
            if (prefabToUse == null) continue;

            GameObject newCardObj = Instantiate(prefabToUse, handArea);
            if (targetIndex != -1) newCardObj.transform.SetSiblingIndex(targetIndex);

            CardUI cardUI = newCardObj.GetComponent<CardUI>();
            if (cardUI != null) cardUI.SetupCard(randomData);
        }
    }

    private GameObject GetCardPrefab(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn: return pawnCardPrefab;
            case PieceType.Knight: return knightCardPrefab != null ? knightCardPrefab : pawnCardPrefab;
            case PieceType.Bishop: return bishopCardPrefab != null ? bishopCardPrefab : pawnCardPrefab;
            case PieceType.Rook: return rookCardPrefab != null ? rookCardPrefab : pawnCardPrefab;
            case PieceType.Queen: return queenCardPrefab != null ? queenCardPrefab : pawnCardPrefab;
            case PieceType.King: return kingCardPrefab != null ? kingCardPrefab : pawnCardPrefab;
            default: return pawnCardPrefab;
        }
    }
}