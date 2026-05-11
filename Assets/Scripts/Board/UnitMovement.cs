using UnityEngine;
using System.Collections;

public class UnitMovement : MonoBehaviour
{
    private Unit myUnit;
    public Tile currentTile;

    void Awake() => myUnit = GetComponent<Unit>();

    IEnumerator Start()
    {
        yield return null;
        if (MapManager.Instance == null) yield break;

        int myX = Mathf.RoundToInt(transform.position.x + 3.5f);
        int myY = Mathf.RoundToInt(transform.position.y + 3.5f);
        Vector2Int myPos = new Vector2Int(myX, myY);

        if (MapManager.Instance.tiles.ContainsKey(myPos))
        {
            currentTile = MapManager.Instance.tiles[myPos];
            currentTile.isOccupied = true;
            currentTile.currentUnit = this.gameObject;
        }
    }

    public void ShowMoveRange(MovePattern pattern)
    {
        int myX = Mathf.RoundToInt(transform.position.x + 3.5f);
        int myY = Mathf.RoundToInt(transform.position.y + 3.5f);
        Vector2Int myPos = new Vector2Int(myX, myY);
        Tile foundTile = null;

        if (MapManager.Instance.tiles.ContainsKey(myPos))
            foundTile = MapManager.Instance.tiles[myPos];

        if (foundTile != null)
        {
            currentTile = foundTile;
            MapManager.Instance.ShowMoveRange(currentTile, pattern, myUnit.isAlly);
        }
    }

    public void TryMoveTo(Tile targetTile)
    {
        if (!targetTile.isOccupied)
        {
            ExecuteMove(targetTile);
        }
        else
        {
            GameObject defenderGO = targetTile.currentUnit;
            Enemy enemy = defenderGO.GetComponent<Enemy>();
            Unit targetUnit = defenderGO.GetComponent<Unit>();

            bool isEnemy = myUnit.isAlly && enemy != null;

            if (isEnemy)
            {
                EnemyUnit eu = defenderGO.GetComponent<EnemyUnit>();
                string defenderName = enemy.EnemyData != null ? enemy.EnemyData.unitName : defenderGO.name;
                Debug.Log($"⚔ {myUnit.unitName}이(가) 적군 {defenderName}을(를) 공격합니다!");

                Vector2Int attackerGridPos = currentTile != null
                    ? new Vector2Int(currentTile.x, currentTile.y)
                    : new Vector2Int(Mathf.RoundToInt(transform.position.x + 3.5f), Mathf.RoundToInt(transform.position.y + 3.5f));
                Vector2Int defenderGridPos = new Vector2Int(targetTile.x, targetTile.y);
                Vector2Int diff = defenderGridPos - attackerGridPos;
                Vector2Int pushDir = (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                    ? new Vector2Int((int)Mathf.Sign(diff.x), 0)
                    : new Vector2Int(0, (int)Mathf.Sign(diff.y));
                Vector2Int knockBackPos = defenderGridPos + pushDir;

                bool canKnockBack = MapManager.Instance.tiles.TryGetValue(knockBackPos, out Tile knockBackTile)
                                    && !knockBackTile.isOccupied;

                if (canKnockBack)
                {
                    targetTile.isOccupied = false;
                    targetTile.currentUnit = null;
                    defenderGO.transform.position = knockBackTile.transform.position;
                    knockBackTile.isOccupied = true;
                    knockBackTile.currentUnit = defenderGO;
                    if (eu != null) eu.gridPosition = knockBackPos;

                    if (currentTile != null)
                    {
                        currentTile.isOccupied = false;
                        currentTile.currentUnit = null;
                    }
                    transform.position = targetTile.transform.position;
                    currentTile = targetTile;
                    targetTile.isOccupied = true;
                    targetTile.currentUnit = this.gameObject;

                    Debug.Log($"💨 {defenderName} 넉백 → ({knockBackPos.x}, {knockBackPos.y})");
                }

                // 🌟 [핵심] 진짜 데미지 연동!
                CombatManager.Instance.PlayerCollideEnemy(myUnit.battleUnit, eu);

                MapManager.Instance.ClearHighlights();
                TurnManager.Instance.NextTurn();
            }
        }
    }

    private void ExecuteMove(Tile targetTile)
    {
        if (currentTile != null)
        {
            currentTile.isOccupied = false;
            currentTile.currentUnit = null;
        }
        transform.position = targetTile.transform.position;
        currentTile = targetTile;
        targetTile.isOccupied = true;
        targetTile.currentUnit = this.gameObject;

        MapManager.Instance.ClearHighlights();
        TurnManager.Instance.NextTurn();
    }
}