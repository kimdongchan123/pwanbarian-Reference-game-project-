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
            Debug.LogWarning($"⚠️ {gameObject.name}의 발밑({myX}, {myY})에 등록된 타일이 없습니다!");
        }
    }

    // 👆 파란색 타일을 클릭했을 때 이동 시도
    public void TryMoveTo(Tile targetTile)
    {
        if (!targetTile.isOccupied)
        {
            ExecuteMove(targetTile);
        }
        else
        {
            Unit targetUnit = targetTile.currentUnit.GetComponent<Unit>();

            if (myUnit.isAlly != targetUnit.isAlly)
            {
                Debug.Log($"⚔ {myUnit.unitName}이(가) 적군 {targetUnit.unitName}을(를) 공격합니다!");
                // 💡 나중에 공격 애니메이션과 체력 깎는 로직을 여기에 넣으시면 됩니다.

                // 공격을 마친 후에도 턴을 넘겨야 하니까 아래 2줄을 실행합니다.
                // MapManager.Instance.ClearHighlights(); 
                // TurnManager.Instance.NextTurn(); 
            }
            else
            {
                Debug.Log($"🛡️ 같은 편인 {targetUnit.unitName}이(가) 길을 막고 있습니다!");
            }
        }
    }

    // 🚶 실제 이동 및 턴 종료 로직
    private void ExecuteMove(Tile targetTile)
    {
        // 1) 예전 타일 비우기
        if (currentTile != null)
        {
            currentTile.isOccupied = false;
            currentTile.currentUnit = null;
        }

        // 2) 내 몸 이동시키기
        transform.position = targetTile.transform.position;

        // 3) 새 타일에 내 정보 등록하기
        currentTile = targetTile;
        targetTile.isOccupied = true;
        targetTile.currentUnit = this.gameObject;

        Debug.Log($"✅ {myUnit.unitName} 이동 완료!");

        // 4) 파란 불 끄고 다음 사람 턴으로!
        MapManager.Instance.ClearHighlights();
        TurnManager.Instance.NextTurn();
    }
}