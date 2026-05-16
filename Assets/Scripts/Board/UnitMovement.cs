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

                // 🌟 [핵심 변경 1] 넉백이나 이동 전에 '데미지'부터 먼저 줍니다!
                enemy.TakeDamage(myUnit.GetAttackPower());

                // 🌟 [핵심 변경 2] 데미지를 받고 적이 죽었는지 확인합니다.
                // (유니티는 Destroy()가 호출되면 오브젝트를 null로 취급합니다)
                // 만약 Enemy 스크립트에 체력 변수(예: hp)가 있다면 "enemy.hp <= 0" 을 추가하셔도 좋습니다.
                bool isEnemyDead = (defenderGO == null || !defenderGO.activeInHierarchy);

                if (isEnemyDead)
                {
                    // 🩸 적이 죽었다면 넉백 무시! 빈자리가 된 타일로 멋지게 쏙 들어갑니다!
                    if (currentTile != null) { currentTile.isOccupied = false; currentTile.currentUnit = null; }
                    transform.position = targetTile.transform.position;
                    currentTile = targetTile;
                    targetTile.isOccupied = true;
                    targetTile.currentUnit = this.gameObject;

                    Debug.Log($"💀 {defenderName} 처치! {myUnit.unitName}이(가) 자리를 점거합니다!");
                }
                else
                {
                    // 🛡️ 적이 살았다면? 정상적인 물리 넉백 로직 실행!
                    Vector2Int attackerGridPos = currentTile != null
                        ? new Vector2Int(currentTile.x, currentTile.y)
                        : new Vector2Int(Mathf.RoundToInt(transform.position.x + 3.5f), Mathf.RoundToInt(transform.position.y + 3.5f));
                    Vector2Int defenderGridPos = new Vector2Int(targetTile.x, targetTile.y);
                    Vector2Int diff = defenderGridPos - attackerGridPos;

                    int dirX = diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1);
                    int dirY = diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1);
                    Vector2Int pushDir = new Vector2Int(dirX, dirY);

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

                        if (currentTile != null) { currentTile.isOccupied = false; currentTile.currentUnit = null; }
                        transform.position = targetTile.transform.position;
                        currentTile = targetTile;
                        targetTile.isOccupied = true;
                        targetTile.currentUnit = this.gameObject;

                        Debug.Log($"💨 넉백 성공! {defenderName} → {knockBackPos}");
                    }
                    else
                    {
                        Vector2Int inFrontPos = defenderGridPos - pushDir;
                        if (attackerGridPos != inFrontPos)
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
                        Debug.Log($"🧱 벽에 막힘! {myUnit.unitName}이(가) 적 앞({inFrontPos})에 착지!");
                    }
                }

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