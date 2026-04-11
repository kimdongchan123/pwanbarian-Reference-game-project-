using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    // 🎯 맵의 모든 타일을 저장하는 사전 (좌표 기반 검색용)
    public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

    // 현재 파란 불이 들어와 있는 타일들의 목록
    public List<Tile> highlightedTiles = new List<Tile>();

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 🗺️ 게임 시작과 동시에 씬의 모든 타일을 스캔하여 좌표계를 구축합니다.
        AutoRegisterAllTiles();
    }

    // [핵심] 씬에 깔린 모든 타일을 찾아 좌표를 부여하고 명부에 등록하는 함수
    public void AutoRegisterAllTiles()
    {
        tiles.Clear();

        // 최신 유니티 방식(FindObjectsByType)으로 모든 타일을 찾습니다. (CS0618 경고 해결)
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);

        foreach (Tile t in allTiles)
        {
            // 타일의 월드 위치(Transform.position)를 정수로 반올림하여 좌표를 생성합니다.
            // 만약 3D 공간(X, Z평면)이라면 t.transform.position.z를 사용하세요.
            int gridX = Mathf.RoundToInt(t.transform.position.x + 3.5f);
            int gridY = Mathf.RoundToInt(t.transform.position.y + 3.5f);

            // 타일 스크립트 본인에게 좌표값을 넣어줍니다.
            t.x = gridX;
            t.y = gridY;

            // 매니저의 딕셔너리에 등록 (좌표를 열쇠로 타일을 저장)
            Vector2Int pos = new Vector2Int(gridX, gridY);
            tiles[pos] = t;
        }

        Debug.Log($" MapManager: 총 {tiles.Count}개의 타일을 좌표계에 등록 완료!");
    }

    // 💡 카드를 클릭했을 때 호출되는 '범위 표시' 함수
    // 매개변수에 bool movingUnitIsAlly 가 추가되었습니다!
    public void ShowMoveRange(Tile startTile, MovePattern pattern, bool movingUnitIsAlly)
    {
        ClearHighlights();
        if (startTile == null) return;

        List<Tile> validTiles = new List<Tile>();

        switch (pattern)
        {
            case MovePattern.Pawn:
            case MovePattern.King:
                int[] kdx = { -1, 0, 1, -1, 1, -1, 0, 1 };
                int[] kdy = { 1, 1, 1, 0, 0, -1, -1, -1 };
                for (int i = 0; i < 8; i++)
                {
                    CheckAndAddTile(startTile.x + kdx[i], startTile.y + kdy[i], validTiles, movingUnitIsAlly);
                }
                break;

            case MovePattern.Knight:
                // 💡 나이트는 원래 체스에서 '뛰어넘는' 기물입니다!
                // 하지만 도착 지점에 아군이 있으면 못 갑니다.
                int[] ndx = { 1, 2, 2, 1, -1, -2, -2, -1 };
                int[] ndy = { 2, 1, -1, -2, -2, -1, 1, 2 };
                for (int i = 0; i < 8; i++)
                {
                    CheckAndAddTile(startTile.x + ndx[i], startTile.y + ndy[i], validTiles, movingUnitIsAlly);
                }
                break;

            case MovePattern.Bishop:
                int[] bdx = { 1, 1, -1, -1 };
                int[] bdy = { 1, -1, 1, -1 };
                for (int i = 0; i < 4; i++)
                {
                    for (int dist = 1; dist <= 8; dist++)
                    {
                        if (CheckAndAddStraightTile(startTile.x + bdx[i] * dist, startTile.y + bdy[i] * dist, validTiles, movingUnitIsAlly) == false)
                            break;
                    }
                }
                break;

            case MovePattern.Rook:
                int[] rdx = { 0, 0, 1, -1 };
                int[] rdy = { 1, -1, 0, 0 };
                for (int i = 0; i < 4; i++)
                {
                    for (int dist = 1; dist <= 8; dist++)
                    {
                        if (CheckAndAddStraightTile(startTile.x + rdx[i] * dist, startTile.y + rdy[i] * dist, validTiles, movingUnitIsAlly) == false)
                            break;
                    }
                }
                break;

            case MovePattern.Queen:
                int[] qdx = { -1, 0, 1, -1, 1, -1, 0, 1 };
                int[] qdy = { 1, 1, 1, 0, 0, -1, -1, -1 };
                for (int i = 0; i < 8; i++)
                {
                    for (int dist = 1; dist <= 8; dist++)
                    {
                        if (CheckAndAddStraightTile(startTile.x + qdx[i] * dist, startTile.y + qdy[i] * dist, validTiles, movingUnitIsAlly) == false)
                            break;
                    }
                }
                break;
        }

        foreach (var t in validTiles)
        {
            t.SetHighlight(true);
            highlightedTiles.Add(t);
        }
    }

    // 🚶 단거리 기물 (폰, 킹, 나이트) 충돌 체크
    private void CheckAndAddTile(int x, int y, List<Tile> validTiles, bool movingUnitIsAlly)
    {
        Vector2Int targetPos = new Vector2Int(x, y);
        if (tiles.ContainsKey(targetPos))
        {
            Tile t = tiles[targetPos];

            // 타일에 누군가 있다면?
            if (t.isOccupied && t.currentUnit != null)
            {
                Unit targetUnit = t.currentUnit.GetComponent<Unit>();
                // 적군이라면 공격 가능 (불 켜기)
                if (targetUnit.isAlly != movingUnitIsAlly)
                {
                    validTiles.Add(t);
                }
                // 아군이라면 이동 불가 (불 안 켬)
            }
            else
            {
                // 비어있으면 이동 가능
                validTiles.Add(t);
            }
        }
    }

    // 🚀 장거리 기물 (룩, 비숍, 퀸) 전진 충돌 체크
    private bool CheckAndAddStraightTile(int x, int y, List<Tile> validTiles, bool movingUnitIsAlly)
    {
        Vector2Int targetPos = new Vector2Int(x, y);
        if (tiles.ContainsKey(targetPos))
        {
            Tile t = tiles[targetPos];

            if (t.isOccupied && t.currentUnit != null)
            {
                Unit targetUnit = t.currentUnit.GetComponent<Unit>();

                if (targetUnit.isAlly != movingUnitIsAlly)
                {
                    // ⚔️ 적군 발견: 공격해야 하니까 이 타일까지만 불을 켜고, 뒤로는 전진 중단(return false)
                    validTiles.Add(t);
                    return false;
                }
                else
                {
                    // 🛡️ 아군 발견: 내 편을 밟을 수 없으니 불도 안 켜고, 전진도 중단(return false)
                    return false;
                }
            }
            else
            {
                // 비어있음: 불을 켜고 계속 전진! (return true)
                validTiles.Add(t);
                return true;
            }
        }
        return false; // 맵 밖으로 나감
    }

    // 모든 파란 불 끄기
    public void ClearHighlights()
    {
        foreach (var t in highlightedTiles)
        {
            if (t != null) t.SetHighlight(false);
        }
        highlightedTiles.Clear();
    }

    // 선택한 타일이 이동 가능한 타일인지 확인하는 함수 (이동 시 사용)
    public bool IsValidMove(Tile tile)
    {
        return highlightedTiles.Contains(tile);
    }
}