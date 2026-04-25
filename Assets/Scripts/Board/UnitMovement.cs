using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitMovement : MonoBehaviour
{
    private Unit myUnit;
    public Tile currentTile;

    void Awake()
    {
        myUnit = GetComponent<Unit>();
    }

    // 🚀 전투 시작 시 또는 소환 시 타일 위치 초기화
    IEnumerator Start()
    {
        // 매니저들이 완전히 준비될 때까지 1프레임 대기
        yield return null;

        if (MapManager.Instance == null) yield break;

        // 현재 월드 좌표를 정수 좌표(0~7)로 변환 (+3.5f 오프셋 적용)
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

    // 🃏 카드를 선택했을 때 이동 가능 범위를 파란색으로 표시
    public void ShowMoveRange(MovePattern pattern)
    {
        int myX = Mathf.RoundToInt(transform.position.x + 3.5f);
        int myY = Mathf.RoundToInt(transform.position.y + 3.5f);
        Vector2Int myPos = new Vector2Int(myX, myY);

        if (MapManager.Instance.tiles.TryGetValue(myPos, out Tile foundTile))
        {
            currentTile = foundTile;
            // 내 진영 정보(isAlly)를 함께 넘겨 아군/적군을 판별하게 함
            MapManager.Instance.ShowMoveRange(currentTile, pattern, myUnit.isAlly);
        }
        else
        {
            Debug.LogWarning($"⚠️ {gameObject.name}의 위치({myX}, {myY})에 타일이 없습니다!");
        }
    }

    // 👆 타일을 클릭했을 때 이동 또는 공격(넉백) 실행
    public void TryMoveTo(Tile targetTile)
    {
        // 1. 빈 타일일 경우: 일반 이동
        if (!targetTile.isOccupied)
        {
            ExecuteMove(targetTile);
        }
        // 2. 타일에 누군가 있을 경우: 공격 판정
        else
        {
            Unit targetUnit = targetTile.currentUnit.GetComponent<Unit>();

            // 적군일 경우에만 공격 및 넉백 로직 실행
            if (myUnit.isAlly != targetUnit.isAlly)
            {
                HandleKnockbackAttack(targetTile, targetUnit);
            }
            else
            {
                Debug.Log($"🛡️ 아군 {targetUnit.unitName}이(가) 길을 막고 있습니다.");
            }
        }
    }

    // ⚔️ 넉백 및 돌진 공격 로직
    private void HandleKnockbackAttack(Tile targetTile, Unit targetUnit)
    {
        Debug.Log($"⚔ {myUnit.unitName}이(가) {targetUnit.unitName}을(를) 공격!");

        // 공격 방향 계산 (내 위치 -> 적 위치)
        int dirX = targetTile.x.CompareTo(currentTile.x);
        int dirY = targetTile.y.CompareTo(currentTile.y);

        // 적이 밀려날 위치와 내가 안착할 후보지 계산
        Vector2Int pushPos = new Vector2Int(targetTile.x + dirX, targetTile.y + dirY);
        Vector2Int frontPos = new Vector2Int(targetTile.x - dirX, targetTile.y - dirY);

        bool canPush = false;
        Tile pushTile = null;

        // 적의 뒤쪽 타일이 맵 안에 있고 비어있는지 확인
        if (MapManager.Instance.tiles.TryGetValue(pushPos, out pushTile))
        {
            if (!pushTile.isOccupied) canPush = true;
        }

        UnitMovement targetMovement = targetUnit.GetComponent<UnitMovement>();

        if (canPush)
        {
            // [상황 A] 적을 뒤로 밀어내고 나는 그 자리에 안착
            Debug.Log($"💨 {targetUnit.unitName} 넉백!");
            targetMovement.MoveBodyTo(pushTile);
            MoveBodyTo(targetTile);
        }
        else
        {
            // [상황 B] 적 뒤가 막혔을 경우, 적의 바로 앞 칸까지 돌진 착지
            Debug.Log($"💥 {targetUnit.unitName}이(가) 밀리지 않아 적의 정면에 안착합니다.");
            if (MapManager.Instance.tiles.TryGetValue(frontPos, out Tile frontTile))
            {
                if (frontTile != currentTile && !frontTile.isOccupied)
                {
                    MoveBodyTo(frontTile);
                }
            }
        }

        // 💡 이곳에 추후 데미지 계산(TakeDamage 등) 로직을 추가하면 됩니다.

        FinishTurn();
    }

    // 🚶 일반 이동 처리
    private void ExecuteMove(Tile targetTile)
    {
        MoveBodyTo(targetTile);
        Debug.Log($"✅ {myUnit.unitName} 이동 완료");
        FinishTurn();
    }

    // 🚀 물리적 위치와 타일 점유 데이터를 실제로 업데이트하는 핵심 함수
    public void MoveBodyTo(Tile targetTile)
    {
        // 이전 타일 정보 초기화
        if (currentTile != null)
        {
            currentTile.isOccupied = false;
            currentTile.currentUnit = null;
        }

        // 월드 좌표 이동
        transform.position = targetTile.transform.position;

        // 새 타일 정보 등록
        currentTile = targetTile;
        targetTile.isOccupied = true;
        targetTile.currentUnit = this.gameObject;

        // 적 유닛일 경우 팀원의 스킬/스탯 시스템 좌표(gridPosition)와도 동기화
        EnemyUnit eu = GetComponent<EnemyUnit>();
        if (eu != null)
        {
            eu.gridPosition = new Vector2Int(targetTile.x, targetTile.y);
        }
    }

    // ✨ 하이라이트를 끄고 턴을 종료
    private void FinishTurn()
    {
        MapManager.Instance.ClearHighlights();
        TurnManager.Instance.NextTurn();
    }
}