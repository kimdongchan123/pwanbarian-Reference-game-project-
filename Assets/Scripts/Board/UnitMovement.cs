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

    // void Start() ?€??IEnumerator Start()ë¥??¬ìš©?©ë‹ˆ??
    IEnumerator Start()
    {
        // ???¤ë¥¸ ë§¤ë‹ˆ?€?¤ì´ ì¤€ë¹„ë  ?Œê¹Œì§€ ??1?„ë ˆ?„ë§Œ ê¸°ë‹¤?¤ì¤?ˆë‹¤.
        yield return null;

        // ?š¨ [?µì‹¬] ë§?ë§¤ë‹ˆ?€ê°€ ?†ë‹¤ë©? (?? ?¸íŒ… ?¬ì¼ ê²½ìš°)
        if (MapManager.Instance == null)
        {
            // ?ëŸ¬ë¥??„ìš°ì§€ ?Šê³ , ê·¸ëƒ¥ ì¡°ìš©?????¨ìˆ˜ë¥??ë‚´ë²„ë¦½?ˆë‹¤. (?¸íŒ… ???‰í™” ? ì?)
            yield break;
        }

        // ?? ë§?ë§¤ë‹ˆ?€ê°€ ?ˆë‹¤ë©? (?? ?„íˆ¬ ?¬ì¼ ê²½ìš°) ?•ìƒ?ìœ¼ë¡??€?¼ì„ ì°¾ìŠµ?ˆë‹¤.
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

    // ?ƒ ì¹´ë“œë¥??Œë??????´ë™ ë²”ìœ„ ?œì‹œ
    public void ShowMoveRange(MovePattern pattern)
    {
        // ???„ì¬ ?„ì¹˜(?•ìˆ˜ ì¢Œí‘œ) ?¤ì‹œ ?•ì¸
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

            // ?š¨ ë°”ë¡œ ??ë¶€ë¶„ì…?ˆë‹¤! (?ëŸ¬ ?´ê²°)
            // ë§¤ë‹ˆ?€?ê²Œ '?´ê? ?„êµ°?¸ì? ?êµ°?¸ì?(myUnit.isAlly)' 3ë²ˆì§¸ ?¬ë£Œë¡??˜ê²¨ì¤ë‹ˆ??
            MapManager.Instance.ShowMoveRange(currentTile, pattern, myUnit.isAlly);
        }
        else
        {
            Debug.LogWarning($" {gameObject.name}??ë°œë°‘({myX}, {myY})???±ë¡???€?¼ì´ ?†ìŠµ?ˆë‹¤!");
        }
    }

    // ?‘† ?Œë????€?¼ì„ ?´ë¦­?ˆì„ ???´ë™ ?œë„
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

        // ??? ë‹›?€ Enemy ì»´í¬?ŒíŠ¸ë¡??ë³„, ?„êµ°?€ Unit.isAllyë¡??ë³„
        bool isEnemy = myUnit.isAlly && enemy != null;
        bool isFriendlyBlocking = !isEnemy && targetUnit != null && targetUnit.isAlly == myUnit.isAlly;

        if (isEnemy)
        {
            string defenderName = enemy.EnemyData != null ? enemy.EnemyData.unitName : defenderGO.name;
            Debug.Log($"??{myUnit.unitName}??ê°€) ?êµ° {defenderName}??ë¥? ê³µê²©?©ë‹ˆ??");

            EnemyUnit eu = defenderGO.GetComponent<EnemyUnit>();

            // ?‰ë°± ë°©í–¥ ê³„ì‚°
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
            Debug.Log($"?›¡ï¸?ê°™ì? ?¸ì¸ {blockerName}??ê°€) ê¸¸ì„ ë§‰ê³  ?ˆìŠµ?ˆë‹¤!");
        }
    }

    private IEnumerator ExecuteAttack(Tile targetTile, GameObject defenderGO, EnemyUnit eu, Enemy enemy,
        string defenderName, Vector2Int knockBackPos, Tile knockBackTile, bool canKnockBack)
    {
        if (canKnockBack)
        {
            // ?¼ë¦¬???€???íƒœ ì¦‰ì‹œ ?…ë°?´íŠ¸
            targetTile.isOccupied = false;
            targetTile.currentUnit = null;
            knockBackTile.isOccupied = true;
            knockBackTile.currentUnit = defenderGO;
            if (eu != null) eu.gridPosition = knockBackPos;

            // ë°©ì–´???‰ë°± ? ë‹ˆë©”ì´??
            yield return StartCoroutine(AnimateMove(defenderGO.transform, knockBackTile.transform.position, MoveDuration * 0.5f));
            Debug.Log($"?’¨ {defenderName} ?‰ë°± ??({knockBackPos.x}, {knockBackPos.y})");

            // ê³µê²©???„ì§„ ? ë‹ˆë©”ì´??
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
            Debug.Log($"?§± {defenderName} ?‰ë°± ë¶ˆê? (ë²??ëŠ” ? ë‹›??ë§‰í˜)");
        }

        int finalDamage = myUnit.GetAttackDamageAgainst(enemy);
        enemy.TakeDamage(finalDamage);
        HitEffectSpawner.SpawnImpact(enemy.transform.position);
        myUnit.OnAttackHit(enemy);

        MapManager.Instance.ClearHighlights();
        if (!myUnit.ConsumeExtraMove())
        {
            TurnManager.Instance.NextTurn();
        }
    }

    // ?š¶ ?¤ì œ ?´ë™ ë°???ì¢…ë£Œ ë¡œì§
    private IEnumerator ExecuteMove(Tile targetTile)
    {
        // 1) ?ˆì „ ?€??ë¹„ìš°ê¸?
        if (currentTile != null)
        {
            currentTile.isOccupied = false;
            currentTile.currentUnit = null;
        }

        // 2) ? ë‹ˆë©”ì´?˜ìœ¼ë¡??´ë™
        yield return StartCoroutine(AnimateMove(transform, targetTile.transform.position, MoveDuration));

        // 3) ???€?¼ì— ???•ë³´ ?±ë¡?˜ê¸°
        currentTile = targetTile;
        targetTile.isOccupied = true;
        targetTile.currentUnit = this.gameObject;

        Debug.Log($" {myUnit.unitName} ?´ë™ ?„ë£Œ!");

        // 4) ?Œë? ë¶??„ê³  ?¤ìŒ ?¬ëŒ ?´ìœ¼ë¡?
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
