using UnityEngine;
using UnityEngine.InputSystem; // 👈 사용자님의 프로젝트 환경에 맞춘 새로운 입력 시스템!

public class PlayerActionController : MonoBehaviour
{
    public static PlayerActionController Instance;

    private TestCardData selectedCard; // 지금 마우스에 쥐고 있는 카드
    private Unit currentUnit;          // 지금 턴을 진행 중인 내 기물

    void Awake() => Instance = this;

    // (UI에서) 카드를 클릭했을 때 실행됨
    public void OnCardSelected(TestCardData card)
    {
        currentUnit = TurnManager.Instance.GetCurrentUnit();

        if (currentUnit != null && currentUnit.isAlly)
        {
            selectedCard = card; // 카드 쥐기
            Debug.Log($"[{selectedCard.cardName}] 선택됨! 이동할 타일을 클릭하세요. (우클릭: 취소)");

            // 타일에 파란 불 켜기
            if (currentUnit.movement != null)
                currentUnit.movement.ShowMoveRange(selectedCard.pattern);
        }
    }

    // 매 프레임마다 마우스 클릭을 감지함
    void Update()
    {
        // 1. 카드를 쥐고 있지 않으면 마우스 클릭을 무시합니다.
        if (selectedCard == null || currentUnit == null) return;

        // 2. 좌클릭: 파란색 타일을 눌러서 이동 확정!
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Tile"))
                {
                    Tile clickedTile = hit.collider.GetComponent<Tile>();

                    // 💡 클릭한 타일이 파란 불이 켜진 타일(이동 가능)인지 확인
                    if (MapManager.Instance.IsValidMove(clickedTile))
                    {
                        // 1) 진짜로 이동 실행!
                        currentUnit.movement.TryMoveTo(clickedTile);

                        // 2) 맵에 켜진 파란 불 끄기
                        MapManager.Instance.ClearHighlights();

                        // 3) 손에 쥔 카드 비우기 (추후 여기서 카드를 버리는(Discard) 함수 호출)
                        selectedCard = null;

                        // 4) 내 턴 끝내고 다음 사람 부르기
                        // TurnManager.Instance.NextTurn(); 
                    }
                    else
                    {
                        Debug.Log("이동할 수 없는 타일입니다!");
                    }
                }
            }
        }

        // 3. 우클릭: 행동 취소 (파란 불 끄고 카드 다시 내려놓기)
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Debug.Log("카드 사용 취소");
            selectedCard = null;
            MapManager.Instance.ClearHighlights();
        }
    }
}