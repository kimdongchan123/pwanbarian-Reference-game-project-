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
            StartCoroutine(ExecuteMove(targetTile));
            return;
        }

        GameObject defenderGO = targetTile.currentUnit;
        Enemy enemy = defenderGO.GetComponent<Enemy>();
        Unit targetUnit = defenderGO.GetComponent<Unit>();

        bool isEnemy = myUnit.isAlly && enemy != null;
        bool isFriendlyBlocking = !isEnemy && targetUnit != null && targetUnit.isAlly == myUnit.isAlly;

        if (isEnemy)
        {
            if (enemy.HasTrait(TraitEffect.boss))
            {
                bool otherAlive = false;
                foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                    if (e != enemy && e.CurrentHp > 0) { otherAlive = true; break; }
                if (otherAlive)
                {
                    Debug.Log($"[보스] {enemy.EnemyData?.unitName}은(는) 다른 적이 살아있는 동안 타겟할 수 없습니다!");
                    return;
                }
            }
            string defenderName = enemy.EnemyData != null ? enemy.EnemyData.unitName : defenderGO.name;
            Debug.Log($"⚔ {myUnit.unitName}이(가) 적군 {defenderName}을(를) 공격합니다!");

            EnemyUnit eu = defenderGO.GetComponent<EnemyUnit>();

            Vector2Int attackerGridPos = currentTile != null
                ? new Vector2Int(currentTile.x, currentTile.y)
                : new Vector2Int(Mathf.RoundToInt(transform.position.x + 3.5f), Mathf.RoundToInt(transform.position.y + 3.5f));
            Vector2Int defenderGridPos = new Vector2Int(targetTile.x, targetTile.y);

            // 🌟 [핵심 보존] 완벽한 넉백 각도 계산 (유니티 버그 회피)
            Vector2Int diff = defenderGridPos - attackerGridPos;
            int absX = Mathf.Abs(diff.x);
            int absY = Mathf.Abs(diff.y);
            int dirX = diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1);
            int dirY = diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1);

            Vector2Int pushDir;
            if (absX == absY) pushDir = new Vector2Int(dirX, dirY);
            else if (absX > absY) pushDir = new Vector2Int(dirX, 0);
            else pushDir = new Vector2Int(0, dirY);

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
        int finalDamage = myUnit.GetAttackDamageAgainst(enemy);

        // 🌟 [핵심 추가] 때리기 전에 적이 죽을 운명인지 미리 확인합니다!
        bool willDie = (enemy.CurrentHp - finalDamage) <= 0;

        if (willDie)
        {
            // 💀 1. 적이 죽을 운명이라면 무조건 빈자리가 되므로 내가 그 자리를 뺏습니다.
            if (currentTile != null) { currentTile.isOccupied = false; currentTile.currentUnit = null; }

            // 돌진 애니메이션 (적 앞까지)
            Vector3 origin = transform.position;
            Vector3 lungeTarget = Vector3.Lerp(origin, targetTile.transform.position, 0.6f);
            yield return StartCoroutine(AnimateMove(transform, lungeTarget, MoveDuration * 0.25f));

            // 타격 효과 및 사운드 발생
            HitEffectSpawner.SpawnImpact(enemy.transform.position);
            enemy.TakeDamage(finalDamage, myUnit);
            myUnit.OnAttackHit(enemy);

            // 적이 펑 터지는 것을 0.1초 기다려줌
            yield return new WaitForSeconds(0.1f);

            // 남은 거리 이동 후 적 타일 완벽 점거!
            yield return StartCoroutine(AnimateMove(transform, targetTile.transform.position, MoveDuration * 0.25f));
            currentTile = targetTile;
            targetTile.isOccupied = true;
            targetTile.currentUnit = this.gameObject;
        }
        else if (canKnockBack)
        {
            // 💨 2. 적이 살았고 넉백 가능할 때 (팀원 애니메이션 코루틴 병렬 처리 보존)
            targetTile.isOccupied = false;
            targetTile.currentUnit = null;
            knockBackTile.isOccupied = true;
            knockBackTile.currentUnit = defenderGO;
            if (eu != null) eu.gridPosition = knockBackPos;

            // 방어자 넉백과 공격자 전진을 동시에 시작
            Coroutine defAnim = StartCoroutine(AnimateMove(defenderGO.transform, knockBackTile.transform.position, MoveDuration * 0.5f));

            if (currentTile != null) { currentTile.isOccupied = false; currentTile.currentUnit = null; }
            Coroutine atkAnim = StartCoroutine(AnimateMove(transform, targetTile.transform.position, MoveDuration * 0.5f));

            yield return defAnim;
            yield return atkAnim;

            currentTile = targetTile;
            targetTile.isOccupied = true;
            targetTile.currentUnit = this.gameObject;

            HitEffectSpawner.SpawnImpact(defenderGO.transform.position);
            enemy.TakeDamage(finalDamage, myUnit);
            myUnit.OnAttackHit(enemy);
        }
        else
        {
            // 🧱 3. 벽에 막혀서 넉백 불가 시 짧은 돌진 타격 후 원위치
            Vector3 origin = transform.position;
            Vector3 lungeTarget = Vector3.Lerp(origin, targetTile.transform.position, 0.4f);
            yield return StartCoroutine(AnimateMove(transform, lungeTarget, MoveDuration * 0.25f));

            HitEffectSpawner.SpawnImpact(defenderGO.transform.position);
            enemy.TakeDamage(finalDamage, myUnit);
            myUnit.OnAttackHit(enemy);

            yield return StartCoroutine(AnimateMove(transform, origin, MoveDuration * 0.25f));
        }

        MapManager.Instance.ClearHighlights();
        if (!myUnit.ConsumeExtraMove())
        {
            TurnManager.Instance.NextTurn();
        }
    }

    private IEnumerator ExecuteMove(Tile targetTile)
    {
        if (currentTile != null) { currentTile.isOccupied = false; currentTile.currentUnit = null; }
        yield return StartCoroutine(AnimateMove(transform, targetTile.transform.position, MoveDuration));
        currentTile = targetTile;
        targetTile.isOccupied = true;
        targetTile.currentUnit = this.gameObject;

        Debug.Log($" {myUnit.unitName} 이동 완료!");
        MapManager.Instance.ClearHighlights();
        if (!myUnit.ConsumeExtraMove()) TurnManager.Instance.NextTurn();
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
            t = t * t * (3f - 2f * t);
            target.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }
        target.position = destination;
    }
}