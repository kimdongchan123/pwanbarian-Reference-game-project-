using System.Collections.Generic;
using UnityEngine;

// EnemyAI 대신 사용하는 고정 패턴 컨트롤러
// TurnManager의 enemyPatternController 필드에 할당하면 됨
// 카드 사용 순서: 폰 → 폰 → 나이트 → 제브라 → 캐멀 → 지라프 (반복)
public class EnemyPatternController : MonoBehaviour
{
    public EnemyUnit enemyUnit;
    public GridManager gridManager;
    public PlayerUnit playerUnit;
    public bool isTopSide = true;

    private static readonly PieceType[] cardSequence =
    {
        PieceType.Pawn,
        PieceType.Pawn,
        PieceType.Knight,
        PieceType.Zebra,
        PieceType.Camel,
        PieceType.Giraffe
    };

    [Header("현재 시퀀스 인덱스 (인스펙터 확인용)")]
    [SerializeField] private int sequenceIndex = 0;

    public void TakeTurn()
    {
        if (!TurnManager.Instance.IsEnemyTurn()) return;

        if (enemyUnit == null || gridManager == null)
        {
            Debug.LogWarning("EnemyPatternController 참조 누락");
            TurnManager.Instance.EndEnemyTurn();
            return;
        }

        // 그로기 중이면 행동 불가
        Enemy enemyComponent = enemyUnit.GetComponent<Enemy>();
        if (enemyComponent != null && enemyComponent.isGroggy)
        {
            Debug.Log($"{enemyUnit.name} 그로기 상태 — 행동 불가");
            TurnManager.Instance.EndEnemyTurn();
            return;
        }

        // 신속 / 재빠름(바다의 재앙): 행마 횟수 결정
        bool hasExtra = enemyComponent != null &&
            (enemyComponent.HasTrait(TraitEffect.swiftness) || enemyComponent.hasSwiftnessBuff);
        int maxActions = hasExtra ? 2 : 1;

        for (int i = 0; i < maxActions; i++)
        {
            if (!ExecuteSequenceAction()) break;
        }

        TurnManager.Instance.EndEnemyTurn();
    }

    private bool ExecuteSequenceAction()
    {
        PieceType currentType = cardSequence[sequenceIndex];
        sequenceIndex = (sequenceIndex + 1) % cardSequence.Length;

        CardData card = FindCard(currentType);
        if (card == null)
        {
            Debug.LogWarning($"{enemyUnit.name}: {currentType} 카드 없음 — 스킵");
            return false;
        }

        List<Vector2Int> validTargets = GetPossibleMoves(card);
        if (validTargets.Count == 0)
        {
            Debug.Log($"{enemyUnit.name}: {currentType} 이동 불가");
            return false;
        }

        Vector2Int chosenTarget = validTargets[Random.Range(0, validTargets.Count)];
        bool willAttack = gridManager.HasPlayer(chosenTarget);

        // 확산(바다의 재앙): 플레이어 인접 타일에서도 공격 가능
        Enemy ec = enemyUnit.GetComponent<Enemy>();
        if (!willAttack && ec != null && ec.hasSpreadBuff)
        {
            Vector2Int playerPos = gridManager.GetPlayerPosition();
            if (playerPos.x >= 0)
            {
                int dx = Mathf.Abs(chosenTarget.x - playerPos.x);
                int dy = Mathf.Abs(chosenTarget.y - playerPos.y);
                if (dx <= 1 && dy <= 1)
                    willAttack = true;
            }
        }

        SkillData readySkill = enemyUnit.GetReadySkill(out int skillIndex);
        if (readySkill != null && willAttack)
            enemyUnit.UseSkill(skillIndex);

        Vector2Int beforePos = enemyUnit.gridPosition;

        if (willAttack)
        {
            if (playerUnit != null && CombatManager.Instance != null)
                CombatManager.Instance.EnemyAttackPlayer(enemyUnit, card, playerUnit);
            Debug.Log($"{enemyUnit.name} | [{currentType}] | 플레이어 공격!");
        }
        else
        {
            enemyUnit.SetGridPosition(chosenTarget);
            Debug.Log($"{enemyUnit.name} | [{currentType}] | {beforePos} → {chosenTarget}");
        }

        return true;
    }

    private CardData FindCard(PieceType type)
    {
        if (enemyUnit.cards == null) return null;
        foreach (var card in enemyUnit.cards)
            if (card != null && card.pieceType == type)
                return card;
        return null;
    }

    // ============================
    // 이동 후보 계산
    // ============================
    private List<Vector2Int> GetPossibleMoves(CardData card)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        Vector2Int currentPos = enemyUnit.gridPosition;

        // 폰
        if (card.pieceType == PieceType.Pawn)
        {
            Vector2Int[] moves = PieceMoveDatabase.GetPawnMoveOffsets(isTopSide, enemyUnit.hasMoved);
            foreach (Vector2Int offset in moves)
            {
                Vector2Int target = currentPos + offset;
                if (!gridManager.IsInsideBoard(target)) continue;
                if (gridManager.IsBlocked(target)) continue;
                validMoves.Add(target);
            }

            Vector2Int[] attacks = isTopSide
                ? new[] { new Vector2Int(-1, -1), new Vector2Int(1, -1) }
                : new[] { new Vector2Int(-1, 1), new Vector2Int(1, 1) };

            foreach (Vector2Int atk in attacks)
            {
                Vector2Int target = currentPos + atk;
                if (!gridManager.IsInsideBoard(target)) continue;
                if (gridManager.HasPlayer(target)) validMoves.Add(target);
            }
        }
        // 슬라이딩 (룩, 비숍, 퀸)
        else if (PieceMoveDatabase.IsSlidingPiece(card.pieceType))
        {
            Vector2Int[] directions = PieceMoveDatabase.GetSlideMoves(card.pieceType);
            foreach (Vector2Int dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    Vector2Int target = currentPos + dir * i;
                    if (!gridManager.IsInsideBoard(target)) break;

                    if (gridManager.IsBlocked(target))
                    {
                        if (gridManager.HasPlayer(target)) validMoves.Add(target);
                        break;
                    }
                    validMoves.Add(target);
                }
            }
        }
        // 점프형 (나이트, 제브라, 캐멀, 지라프, 킹)
        else
        {
            Vector2Int[] offsets = PieceMoveDatabase.GetJumpMoves(card.pieceType);
            foreach (Vector2Int offset in offsets)
            {
                Vector2Int target = currentPos + offset;
                if (!gridManager.IsInsideBoard(target)) continue;

                if (gridManager.IsBlocked(target))
                {
                    if (gridManager.HasPlayer(target)) validMoves.Add(target);
                    continue;
                }
                validMoves.Add(target);
            }
        }

        return validMoves;
    }
}
