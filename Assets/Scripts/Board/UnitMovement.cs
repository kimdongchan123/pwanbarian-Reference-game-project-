using UnityEngine;
using System.Collections;
public class UnitMovement : MonoBehaviour
{
    private Unit myUnit;
    public Tile currentTile;

    private const float MoveDuration = 1f;

    void Awake()
    {
        myUnit = GetComponent<Unit>();
    }

    // void Start() 대신 IEnumerator Start()를 사용합니다!
    IEnumerator Start()
    {
        // ⏳ 다른 매니저들이 준비될 때까지 딱 1프레임만 기다려줍니다.
        yield return null;

        // 🚨 [핵심] 맵 매니저가 없다면? (예: 세팅 씬일 경우)
        if (MapManager.Instance == null)
        {
            // 에러를 띄우지 않고, 그냥 조용히 이 함수를 끝내버립니다. (세팅 씬 평화 유지)
            yield break;
        }

        // 🚀 맵 매니저가 있다면? (예: 전투 씬일 경우) 정상적으로 타일을 찾습니다.
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

    // 🃏 카드를 눌렀을 때 이동 범위 표시
    public void ShowMoveRange(MovePattern pattern)
    {
        // 내 현재 위치(정수 좌표) 다시 확인
        int myX = Mathf.RoundToInt(transform.position.x + 3.5f);
        int myY = Mathf.RoundToInt(transform.position.y + 3.5f);

        Vector2Int myPos = new Vector2Int(myX, myY);
        Tile foundTile = null;

        if (MapManager.Instance.tiles.ContainsKey(myPos))
        {
            foundTile = MapManager.Instance.tiles[myPos];
        }

        if (foundTile != null)
        {
            currentTile = foundTile;

            // 🚨 바로 이 부분입니다! (에러 해결)
            // 매니저에게 '내가 아군인지 적군인지(myUnit.isAlly)' 3번째 재료로 넘겨줍니다!
            MapManager.Instance.ShowMoveRange(currentTile, pattern, myUnit.isAlly);
        }
        else
        {
            Debug.LogWarning($" {gameObject.name}의 발밑({myX}, {myY})에 등록된 타일이 없습니다!");
        }
    }

    // 👆 파란색 타일을 클릭했을 때 이동 시도
    public void TryMoveTo(Tile targetTile)
    {
        if (!targetTile.isOccupied)
        {
            StartCoroutine(ExecuteMove(targetTile));
            return;
        }

        GameObject defenderGO = targetTile.currentUnit;
        Enemy enemy = defenderGO.GetComponent<Enemy>();
        Unit targetUnit = defenderGO.GetComponent<Unit>();

        // 적 유닛은 Enemy 컴포넌트로 판별, 아군은 Unit.isAlly로 판별
        bool isEnemy = myUnit.isAlly && enemy != null;
        bool isFriendlyBlocking = !isEnemy && targetUnit != null && targetUnit.isAlly == myUnit.isAlly;

        if (isEnemy)
        {
            string defenderName = enemy.EnemyData != null ? enemy.EnemyData.unitName : defenderGO.name;
            Debug.Log($"⚔ {myUnit.unitName}이(가) 적군 {defenderName}을(를) 공격합니다!");

            EnemyUnit eu = defenderGO.GetComponent<EnemyUnit>();

            // 넉백 방향 계산
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

            StartCoroutine(ExecuteAttack(targetTile, defenderGO, eu, enemy, defenderName, knockBackPos, knockBackTile, canKnockBack));
        }
        else
        {
            string blockerName = targetUnit != null ? targetUnit.unitName : defenderGO.name;
            Debug.Log($"🛡️ 같은 편인 {blockerName}이(가) 길을 막고 있습니다!");
        }
    }

    private IEnumerator ExecuteAttack(Tile targetTile, GameObject defenderGO, EnemyUnit eu, Enemy enemy,
        string defenderName, Vector2Int knockBackPos, Tile knockBackTile, bool canKnockBack)
    {
        if (canKnockBack)
        {
            // 논리적 타일 상태 즉시 업데이트
            targetTile.isOccupied = false;
            targetTile.currentUnit = null;
            knockBackTile.isOccupied = true;
            knockBackTile.currentUnit = defenderGO;
            if (eu != null) eu.gridPosition = knockBackPos;

            // 방어자 넉백 애니메이션
            yield return StartCoroutine(AnimateMove(defenderGO.transform, knockBackTile.transform.position, MoveDuration * 0.5f));
            Debug.Log($"💨 {defenderName} 넉백 → ({knockBackPos.x}, {knockBackPos.y})");

            // 공격자 전진 애니메이션
            if (currentTile != null)
            {
                currentTile.isOccupied = false;
                currentTile.currentUnit = null;
            }
            yield return StartCoroutine(AnimateMove(transform, targetTile.transform.position, MoveDuration));
            currentTile = targetTile;
            targetTile.isOccupied = true;
            targetTile.currentUnit = this.gameObject;
        }
        else
        {
            Debug.Log($"🧱 {defenderName} 넉백 불가 (벽 또는 유닛에 막힘)");
        }

        int finalDamage = myUnit.GetAttackDamageAgainst(enemy);
        enemy.TakeDamage(finalDamage);
        myUnit.OnAttackHit(enemy);

        MapManager.Instance.ClearHighlights();
        if (!myUnit.ConsumeExtraMove())
        {
            TurnManager.Instance.NextTurn();
        }
    }

    // 🚶 실제 이동 및 턴 종료 로직
    private IEnumerator ExecuteMove(Tile targetTile)
    {
        // 1) 예전 타일 비우기
        if (currentTile != null)
        {
            currentTile.isOccupied = false;
            currentTile.currentUnit = null;
        }

        // 2) 애니메이션으로 이동
        yield return StartCoroutine(AnimateMove(transform, targetTile.transform.position, MoveDuration));

        // 3) 새 타일에 내 정보 등록하기
        currentTile = targetTile;
        targetTile.isOccupied = true;
        targetTile.currentUnit = this.gameObject;

        Debug.Log($" {myUnit.unitName} 이동 완료!");

        // 4) 파란 불 끄고 다음 사람 턴으로!
        MapManager.Instance.ClearHighlights();
        if (!myUnit.ConsumeExtraMove())
        {
            TurnManager.Instance.NextTurn();
        }
    }

    private IEnumerator AnimateMove(Transform target, Vector3 destination, float duration)
    {
        Vector3 start = target.position;
        destination.z = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); // smoothstep easing
            target.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }
        target.position = destination;
    }
}
