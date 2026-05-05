using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActionController : MonoBehaviour
{
    public static PlayerActionController Instance;

    private CardData selectedCard;
    private GameObject selectedCardObj; // 선택한 카드의 UI 몸통
    private Unit currentUnit;

    private void Awake()
    {
        Instance = this;
    }

    public void OnCardSelected(CardData card, GameObject cardObj)
    {
        currentUnit = TurnManager.Instance.GetCurrentUnit();

        if (card == null) return;
        if (currentUnit == null) return;
        if (!currentUnit.isAlly) return;

        selectedCard = card;
        selectedCardObj = cardObj;

        Debug.Log("카드 선택됨: " + selectedCard.cardName);

        if (currentUnit.movement != null)
        {
            MovePattern movePattern = ConvertPieceTypeToMovePattern(selectedCard.pieceType);
            currentUnit.movement.ShowMoveRange(movePattern);
        }
    }

    private void Update()
    {
        if (selectedCard == null || currentUnit == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) TryUseSelectedCard();
        if (Mouse.current.rightButton.wasPressedThisFrame) CancelSelectedCard();
    }

    private void TryUseSelectedCard()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        if (!hit.collider.CompareTag("Tile")) return;

        Tile clickedTile = hit.collider.GetComponent<Tile>();
        if (clickedTile == null) return;

        if (!MapManager.Instance.IsValidMove(clickedTile))
        {
            Debug.Log("이동 불가능 타일");
            return;
        }

        Unit targetUnit = clickedTile.GetComponentInChildren<Unit>();

        // 이동 실행
        currentUnit.movement.TryMoveTo(clickedTile);

        // 카드 효과 적용 (데미지 및 버프)
        ApplySelectedCardEffect(targetUnit);

        // 보드판 정리
        MapManager.Instance.ClearHighlights();
        Debug.Log("카드 사용 완료: " + selectedCard.cardName);

        // 🌟 [핵심] 카드를 쓴 바로 그 자리에 새 카드를 채워 넣습니다!
        if (selectedCardObj != null)
        {
            int cardIndex = selectedCardObj.transform.GetSiblingIndex();

            Destroy(selectedCardObj);
            selectedCardObj = null;

            if (HandUIManager.Instance != null)
            {
                HandUIManager.Instance.DrawCards(1, cardIndex);
            }
        }

        selectedCard = null;
    }

    private void ApplySelectedCardEffect(Unit targetUnit)
    {
        if (selectedCard == null || currentUnit == null) return;

        // -----------------
        // 1. 데미지 처리
        // -----------------
        if (selectedCard.power > 0)
        {
            if (selectedCard.targetType == CardTargetType.Enemy && targetUnit != null && !targetUnit.isAlly)
            {
                targetUnit.TakeDamage(selectedCard.power);
                Debug.Log("카드 데미지 적용: " + selectedCard.power);
            }
            else if (selectedCard.targetType == CardTargetType.Self)
            {
                currentUnit.TakeDamage(selectedCard.power);
            }
        }

        // -----------------
        // 2. 상태 효과 (팀원들의 카드 데이터 활용)
        // -----------------
        if (!selectedCard.useEffect) return;
        if (selectedCard.effectType == StatusEffectType.None) return;

        // EffectToSelf 체크 여부에 따라 버프를 받을 대상을 정합니다.
        Unit effectTarget = selectedCard.effectToSelf ? currentUnit : targetUnit;

        if (effectTarget != null)
        {
            // Unit.cs에게 "야, 이 효과(버프) 좀 적용해 줘!" 하고 던집니다.
            effectTarget.AddStatus(selectedCard.effectType, selectedCard.effectAmount);
        }
    }

    private void CancelSelectedCard()
    {
        selectedCard = null;
        selectedCardObj = null;
        MapManager.Instance.ClearHighlights();
    }

    private MovePattern ConvertPieceTypeToMovePattern(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn: return MovePattern.Pawn;
            case PieceType.Knight: return MovePattern.Knight;
            case PieceType.Bishop: return MovePattern.Bishop;
            case PieceType.Rook: return MovePattern.Rook;
            case PieceType.Queen: return MovePattern.Queen;
            case PieceType.King: return MovePattern.King;
            default: return MovePattern.Pawn;
        }
    }
}