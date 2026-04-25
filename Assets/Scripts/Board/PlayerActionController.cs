using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActionController : MonoBehaviour
{
    public static PlayerActionController Instance;

    private CardData selectedCard;
    private Unit currentUnit;

    private void Awake()
    {
        Instance = this;
    }

    // 카드 클릭 시
    public void OnCardSelected(CardData card)
    {
        currentUnit = TurnManager.Instance.GetCurrentUnit();

        if (card == null)
        {
            Debug.LogWarning("선택한 카드가 null입니다.");
            return;
        }

        if (currentUnit == null)
        {
            Debug.LogWarning("현재 턴 유닛이 없습니다.");
            return;
        }

        if (!currentUnit.isAlly)
        {
            Debug.Log("현재 유닛이 아군이 아닙니다.");
            return;
        }

        selectedCard = card;

        Debug.Log("카드 선택됨: " + selectedCard.cardName);

        if (currentUnit.movement != null)
        {
            MovePattern movePattern = ConvertPieceTypeToMovePattern(selectedCard.pieceType);
            currentUnit.movement.ShowMoveRange(movePattern);
        }
    }

    private void Update()
    {
        if (selectedCard == null || currentUnit == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryUseSelectedCard();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelSelectedCard();
        }
    }

    private void TryUseSelectedCard()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (!hit.collider.CompareTag("Tile"))
            return;

        Tile clickedTile = hit.collider.GetComponent<Tile>();

        if (clickedTile == null)
        {
            Debug.LogWarning("Tile 컴포넌트 없음");
            return;
        }

        if (!MapManager.Instance.IsValidMove(clickedTile))
        {
            Debug.Log("이동 불가능 타일");
            return;
        }

        Unit targetUnit = clickedTile.GetComponentInChildren<Unit>();

        // 이동
        currentUnit.movement.TryMoveTo(clickedTile);

        // 효과 적용
        ApplySelectedCardEffect(targetUnit);

        // 정리
        MapManager.Instance.ClearHighlights();

        Debug.Log("카드 사용 완료: " + selectedCard.cardName);

        selectedCard = null;
    }

    private void ApplySelectedCardEffect(Unit targetUnit)
    {
        if (selectedCard == null || currentUnit == null)
            return;

        // -----------------
        // 데미지
        // -----------------
        if (selectedCard.power > 0)
        {
            if (selectedCard.targetType == CardTargetType.Enemy)
            {
                if (targetUnit != null && !targetUnit.isAlly)
                {
                    targetUnit.TakeDamage(selectedCard.power);
                    Debug.Log("데미지 적용: " + selectedCard.power);
                }
            }
            else if (selectedCard.targetType == CardTargetType.Self)
            {
                currentUnit.TakeDamage(selectedCard.power);
            }
        }

        // -----------------
        // 상태 효과
        // -----------------
        if (!selectedCard.useEffect)
            return;

        if (selectedCard.effectType == StatusEffectType.None)
            return;

        Unit effectTarget = selectedCard.effectToSelf ? currentUnit : targetUnit;

        if (effectTarget == null)
        {
            Debug.Log("효과 대상 없음");
            return;
        }

        effectTarget.AddStatus(selectedCard.effectType, selectedCard.effectAmount);

        Debug.Log("상태 적용: " + selectedCard.effectType + " / " + selectedCard.effectAmount);
    }

    private void CancelSelectedCard()
    {
        Debug.Log("카드 취소");

        selectedCard = null;

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