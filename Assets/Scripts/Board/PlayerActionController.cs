using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActionController : MonoBehaviour
{
    public static PlayerActionController Instance;
    private CardData selectedCard;
    private Unit currentUnit;

    private void Awake() => Instance = this;

    public void OnCardSelected(CardData card)
    {
        currentUnit = TurnManager.Instance.GetCurrentUnit();
        if (card == null || currentUnit == null || !currentUnit.isAlly) return;

        selectedCard = card;
        Debug.Log("카드 선택됨: " + selectedCard.cardName);

        if (currentUnit.movement != null)
            currentUnit.movement.ShowMoveRange(ConvertPieceTypeToMovePattern(selectedCard.pieceType));
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
        if (!Physics.Raycast(ray, out RaycastHit hit) || !hit.collider.CompareTag("Tile")) return;

        Tile clickedTile = hit.collider.GetComponent<Tile>();
        if (!MapManager.Instance.IsValidMove(clickedTile)) return;

        Unit targetUnit = clickedTile.GetComponentInChildren<Unit>();
        EnemyUnit targetEnemy = clickedTile.GetComponentInChildren<EnemyUnit>();

        currentUnit.movement.TryMoveTo(clickedTile);
        ApplySelectedCardEffect(targetUnit, targetEnemy);

        MapManager.Instance.ClearHighlights();
        selectedCard = null;
    }

    private void ApplySelectedCardEffect(Unit targetUnit, EnemyUnit targetEnemy)
    {
        if (selectedCard == null || currentUnit == null) return;

        // 1. 진짜 CombatManager 데미지 연동
        if (selectedCard.power > 0)
        {
            if (selectedCard.targetType == CardTargetType.Enemy && targetEnemy != null)
            {
                CombatManager.Instance.PlayerAttackEnemy(currentUnit.battleUnit, selectedCard, targetEnemy);
            }
            else if (selectedCard.targetType == CardTargetType.Self)
            {
                currentUnit.battleUnit.TakeDamage(selectedCard.power, selectedCard.damageType);
            }
        }

        // 2. 진짜 BattleUnit 상태이상 연동
        if (selectedCard.useEffect && selectedCard.effectType != StatusEffectType.None)
        {
            if (selectedCard.effectToSelf)
            {
                currentUnit.battleUnit.AddStatus(selectedCard.effectType, selectedCard.effectAmount);
                Debug.Log($"자신에게 {selectedCard.effectType} {selectedCard.effectAmount} 적용");
            }
            else if (targetEnemy != null)
            {
                // 적의 상태이상은 추후 기믹 구현 시 추가될 예정이므로 로그만 남깁니다.
                Debug.Log($"적({targetEnemy.name})에게 {selectedCard.effectType} {selectedCard.effectAmount} 부여 시도 (적용 대기)");
            }
        }
    }

    private void CancelSelectedCard()
    {
        selectedCard = null;
        MapManager.Instance.ClearHighlights();
    }

    private MovePattern ConvertPieceTypeToMovePattern(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => MovePattern.Pawn,
            PieceType.Knight => MovePattern.Knight,
            PieceType.Bishop => MovePattern.Bishop,
            PieceType.Rook => MovePattern.Rook,
            PieceType.Queen => MovePattern.Queen,
            PieceType.King => MovePattern.King,
            _ => MovePattern.Pawn
        };
    }
}