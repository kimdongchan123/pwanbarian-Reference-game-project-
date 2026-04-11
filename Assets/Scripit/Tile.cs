using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("맵 좌표 (MapManager에서 사용)")]
    public int x; // 👈 새로 추가됨: 맵에서의 X 좌표
    public int y; // 👈 새로 추가됨: 맵에서의 Y 좌표

    [Header("상태 및 배치 정보 (원본 유지)")]
    // 기물이 배치되어 있는지 확인하는 변수 (false면 비어있음, true면 기물이 있음)
    public bool isOccupied = false;
    public GameObject currentUnit;
    public bool isDeployableZone = true;
    public int placedUnitIndex = -1;

    // --- [시각 효과를 위한 변수들] ---
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // 2D 게임의 타일 이미지 컴포넌트를 가져옵니다.
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color; // 게임 시작 시 원래 색상(흰색 등)을 기억해둡니다!
        }
    }

    // --- [기존 기능: 마우스 올렸을 때 (배치용)] ---
    public void SetHoverColor(Color color)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    public void ResetColor()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    // --- [신규 기능: 카드 클릭 시 (이동 범위 표시용)] ---
    // 💡 기존에 만들어두신 색상 변경 함수들을 그대로 재활용합니다!
    public void SetHighlight(bool isHighlighted)
    {
        if (isHighlighted)
        {
            SetHoverColor(Color.cyan); // 파란색(Cyan)으로 불 켜기
        }
        else
        {
            ResetColor(); // 원래 색으로 불 끄기
        }
    }
}