using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(EnemyUnit))]
public class EnemyAI : MonoBehaviour
{
    private UnitMovement myMovement;
    private EnemyUnit myEnemyUnit;
    private Unit myUnit;

    void Awake()
    {
        myMovement = GetComponent<UnitMovement>();
        myEnemyUnit = GetComponent<EnemyUnit>();
        myUnit = GetComponent<Unit>();
    }

    public void PlayTurn()
    {
        if (myEnemyUnit != null)
        {
            myEnemyUnit.TickBuffs();
            myEnemyUnit.TickSkillCT();
            myEnemyUnit.UseNextSkillInSequence();
        }
        StartCoroutine(ExecuteAIAction());
    }

    private IEnumerator ExecuteAIAction()
    {
        yield return new WaitForSeconds(0.3f);

        MapManager.Instance.ShowMoveRange(myMovement.currentTile, MovePattern.Pawn, myUnit.isAlly);
        List<Tile> possibleTiles = new List<Tile>(MapManager.Instance.highlightedTiles);

        if (possibleTiles.Count > 0)
        {
            Unit targetAlly = FindNearestAlly();
            Tile bestTile = possibleTiles[0];

            if (targetAlly != null)
            {
                float minDist = float.MaxValue;
                Vector2Int allyPos = new Vector2Int(
                    Mathf.RoundToInt(targetAlly.transform.position.x + 3.5f),
                    Mathf.RoundToInt(targetAlly.transform.position.y + 3.5f)
                );

                foreach (Tile t in possibleTiles)
                {
                    if (t.isOccupied && t.currentUnit != null)
                    {
                        Unit u = t.currentUnit.GetComponent<Unit>();
                        if (u != null && u.isAlly) { bestTile = t; break; }
                    }

                    float dist = Vector2Int.Distance(new Vector2Int(t.x, t.y), allyPos);
                    if (dist < minDist) { minDist = dist; bestTile = t; }
                }
            }
            myMovement.TryMoveTo(bestTile);
        }
        else
        {
            Debug.Log($"🤖 {gameObject.name}은(는) 길이 막혔습니다! 턴 종료.");
            TurnManager.Instance.NextTurn();
        }
    }

    private Unit FindNearestAlly()
    {
        Unit nearest = null;
        float minDist = float.MaxValue;
        Vector2Int myPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x + 3.5f),
            Mathf.RoundToInt(transform.position.y + 3.5f)
        );

        foreach (var unit in TurnManager.Instance.allUnits)
        {
            if (unit == null || !unit.isAlly) continue;
            Vector2Int allyPos = new Vector2Int(
                Mathf.RoundToInt(unit.transform.position.x + 3.5f),
                Mathf.RoundToInt(unit.transform.position.y + 3.5f)
            );

            float dist = Vector2Int.Distance(myPos, allyPos);
            if (dist < minDist) { minDist = dist; nearest = unit; }
        }
        return nearest;
    }
}