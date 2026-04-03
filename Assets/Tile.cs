using UnityEngine;

public class Tile : MonoBehaviour
{
    // 기물이 배치되어 있는지 확인하는 변수
    // false면 비어있음, true면 기물이 있음
    public bool isOccupied = false;
    public GameObject currentUnit;
    public bool isDeployableZone = true;
    // --- [시각 효과를 위한 변수들 추가] ---
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

    // 외부(매니저)에서 이 함수를 부르면 타일 색이 변합니다.
    public void SetHoverColor(Color color)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    // 마우스가 나가면 다시 원래 색으로 돌아옵니다.
    public void ResetColor()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }
}