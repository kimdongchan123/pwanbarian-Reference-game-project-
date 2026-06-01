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

    // void Start() ?�??IEnumerator Start()�??�용?�니??
    IEnumerator Start()
    {
        // ???�른 매니?�?�이 준비될 ?�까지 ??1?�레?�만 기다?�줍?�다.
        yield return null;

        // ?�� [?�심] �?매니?�가 ?�다�? (?? ?�팅 ?�일 경우)
        if (MapManager.Instance == null)
        {
            // ?�러�??�우지 ?�고, 그냥 조용?????�수�??�내버립?�다. (?�팅 ???�화 ?��?)
            yield break;
        }

        // ?? �?매니?�가 ?�다�? (?? ?�투 ?�일 경우) ?�상?�으�??�?�을 찾습?�다.
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

    // ?�� 카드�??��??????�동 범위 ?�시
    public void ShowMoveRange(MovePattern pattern)
    {
        // ???�재 ?�치(?�수 좌표) ?�시 ?�인
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

            // ?�� 바로 ??부분입?�다! (?�러 ?�결)
            // 매니?�?�게 '?��? ?�군?��? ?�군?��?(myUnit.isAlly)' 3번째 ?�료�??�겨줍니??
            MapManager.Instance.ShowMoveRange(currentTile, pattern, myUnit.isAlly);
        }
        else
        {
            Debug.LogWarning($" {gameObject.name}??발밑({myX}, {myY})???�록???�?�이 ?�습?�다!");
        }
    }

    // ?�� ?��????�?�을 ?�릭?�을 ???�동 ?�도
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

        // ???�닛?� Enemy 컴포?�트�??�별, ?�군?� Unit.isAlly�??�별
        bool isEnemy = myUnit.isAlly && enemy != null;
        bool isFriendlyBlocking = !isEnemy && targetUnit != null && targetUnit.isAlly == myUnit.isAlly;

        if (isEnemy)
        {
            // 보스 특성: 다른 적이 살아있는 동안 타겟 불가
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
            Debug.Log($"??{myUnit.unitName}??가) ?�군 {defenderName}??�? 공격?�니??");

            EnemyUnit eu = defenderGO.GetComponent<EnemyUnit>();

            // ?�백 방향 계산
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
            Debug.Log($"?���?같�? ?�인 {blockerName}??가) 길을 막고 ?�습?�다!");
        }
    }

    private IEnumerator ExecuteAttack(Tile targetTile, GameObject defenderGO, EnemyUnit eu, Enemy enemy,
        string defenderName, Vector2Int knockBackPos, Tile knockBackTile, bool canKnockBack)
    {
        if (canKnockBack)
        {
            // ?�리???�???�태 즉시 ?�데?�트
            targetTile.isOccupied = false;
            targetTile.currentUnit = null;
            knockBackTile.isOccupied = true;
            knockBackTile.currentUnit = defenderGO;
            if (eu != null) eu.gridPosition = knockBackPos;

            // 방어???�백 ?�니메이??
            yield return StartCoroutine(AnimateMove(defenderGO.transform, knockBackTile.transform.position, MoveDuration * 0.5f));
            Debug.Log($"?�� {defenderName} ?�백 ??({knockBackPos.x}, {knockBackPos.y})");

            // 공격???�진 ?�니메이??
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
            Debug.Log($"?�� {defenderName} ?�백 불�? (�??�는 ?�닛??막힘)");
        }

        int finalDamage = myUnit.GetAttackDamageAgainst(enemy);
        enemy.TakeDamage(finalDamage, myUnit);
        HitEffectSpawner.SpawnImpact(enemy.transform.position);
        myUnit.OnAttackHit(enemy);

        MapManager.Instance.ClearHighlights();
        if (!myUnit.ConsumeExtraMove())
        {
            TurnManager.Instance.NextTurn();
        }
    }

    // ?�� ?�제 ?�동 �???종료 로직
    private IEnumerator ExecuteMove(Tile targetTile)
    {
        // 1) ?�전 ?�??비우�?
        if (currentTile != null)
        {
            currentTile.isOccupied = false;
            currentTile.currentUnit = null;
        }

        // 2) ?�니메이?�으�??�동
        yield return StartCoroutine(AnimateMove(transform, targetTile.transform.position, MoveDuration));

        // 3) ???�?�에 ???�보 ?�록?�기
        currentTile = targetTile;
        targetTile.isOccupied = true;
        targetTile.currentUnit = this.gameObject;

        Debug.Log($" {myUnit.unitName} ?�동 ?�료!");

        // 4) ?��? �??�고 ?�음 ?�람 ?�으�?
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
