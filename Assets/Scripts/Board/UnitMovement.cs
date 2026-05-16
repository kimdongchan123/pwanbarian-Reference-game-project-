using UnityEngine;
using System.Collections;

public class UnitMovement : MonoBehaviour
{
    private Unit myUnit;
    public Tile currentTile;

    void Awake()
    {
        myUnit = GetComponent<Unit>();
    }

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
        else
        {
            Debug.LogWarning($" {gameObject.name}의 발밑({myX}, {myY})에 등록된 타일이 없습니다!");
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
            bool isFriendlyBlocking = !isEnemy && targetUnit != null && targetUnit.isAlly == myUnit.isAlly;

            if (isEnemy)
            {
                string defenderName = enemy.EnemyData != null ? enemy.EnemyData.unitName : defenderGO.name;
                Debug.Log($"⚔ {myUnit.unitName}이(가) 적군 {defenderName}을(를) 공격합니다!");

                EnemyUnit eu = defenderGO.GetComponent<EnemyUnit>();

                Vector2Int attackerGridPos = currentTile != null
                    ? new Vector2Int(currentTile.x, currentTile.y)
                    : new Vector2Int(Mathf.RoundToInt(transform.position.x + 3.5f), Mathf.RoundToInt(transform.position.y + 3.5f));
                Vector2Int defenderGridPos = new Vector2Int(targetTile.x, targetTile.y);
                Vector2Int diff = defenderGridPos - attackerGridPos;

                // 🌟 [핵심 수정 1] 유니티 Mathf.Sign 버그 완전 차단! (0이면 정직하게 0으로 처리)
                int dirX = diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1);
                int dirY = diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1);
                Vector2Int pushDir = new Vector2Int(dirX, dirY);

                Vector2Int knockBackPos = defenderGridPos + pushDir;

                bool canKnockBack = MapManager.Instance.tiles.TryGetValue(knockBackPos, out Tile knockBackTile)
                                    && !knockBackTile.isOccupied;

                // 🌟 [핵심 수정 2] 회원님의 정석 넉백 & 착지 규칙 적용
                if (canKnockBack)
                {
                    // 1. 적이 뒤로 밀려남
                    targetTile.isOccupied = false;
                    targetTile.currentUnit = null;
                    defenderGO.transform.position = knockBackTile.transform.position;
                    knockBackTile.isOccupied = true;
                    knockBackTile.currentUnit = defenderGO;
                    if (eu != null) eu.gridPosition = knockBackPos;

                    // 2. 아군은 적이 있던 자리를 빼앗고 점유함
                    if (currentTile != null) { currentTile.isOccupied = false; currentTile.currentUnit = null; }
                    transform.position = targetTile.transform.position;
                    currentTile = targetTile;
                    targetTile.isOccupied = true;
                    targetTile.currentUnit = this.gameObject;

                    Debug.Log($"💨 넉백 성공! {defenderName} → {knockBackPos} / 아군은 {defenderGridPos} 자리 뺏음!");
                }
                else
                {
                    // 1. 벽에 막힘 -> 적은 밀려나지 않음
                    // 2. 아군은 적의 "바로 앞(때린 방향 한 칸 뒤)" 타일에 떨어짐!
                    Vector2Int inFrontPos = defenderGridPos - pushDir;

                    if (attackerGridPos != inFrontPos) // (이미 바로 앞에 붙어있다면 굳이 이동 안 함)
                    {
                        if (MapManager.Instance.tiles.TryGetValue(inFrontPos, out Tile inFrontTile))
                        {
                            if (currentTile != null) { currentTile.isOccupied = false; currentTile.currentUnit = null; }
                            transform.position = inFrontTile.transform.position;
                            currentTile = inFrontTile;
                            inFrontTile.isOccupied = true;
                            inFrontTile.currentUnit = this.gameObject;
                        }
                    }
                    Debug.Log($"🧱 벽에 막힘! 적은 고정, 아군은 적 앞({inFrontPos})에 착지!");
                }

                // 공격 데미지 적용
                enemy.TakeDamage(myUnit.GetAttackPower());

                MapManager.Instance.ClearHighlights();
                TurnManager.Instance.NextTurn();
            }
            else
            {
                string blockerName = targetUnit != null ? targetUnit.unitName : defenderGO.name;
                Debug.Log($"🛡️ 같은 편인 {blockerName}이(가) 길을 막고 있습니다!");
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

        Debug.Log($" {myUnit.unitName} 이동 완료!");

        MapManager.Instance.ClearHighlights();
        TurnManager.Instance.NextTurn();
    }
}