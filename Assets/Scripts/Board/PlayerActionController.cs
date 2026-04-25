// (PlayerActionController.cs 전체 코드를 이걸로 덮어써주세요!)

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActionController : MonoBehaviour
{
    public static PlayerActionController Instance;

    private TestCardData selectedCard; // 지금 마우스에 쥐고 있는 카드 데이터
    private GameObject selectedCardObj; // 🚨 [추가] 지금 쥐고 있는 카드의 실제 UI 몸통!
    private Unit currentUnit;

    void Awake()
    {
        Instance = this;
    }

    // 🃏 UI에서 카드를 클릭했을 때 실행됨 (매개변수에 GameObject가 추가됨)
    public void OnCardSelected(TestCardData card, GameObject cardObj)
    {
        currentUnit = TurnManager.Instance.GetCurrentUnit();

        if (currentUnit != null && currentUnit.isAlly)
        {
            selectedCard = card;
            selectedCardObj = cardObj; // 🚨 내 손에 카드의 몸통을 쥡니다.
            Debug.Log($"[{selectedCard.cardName}] 선택됨! 이동할 타일을 클릭하세요. (우클릭: 취소)");

            // 타일에 파란 불 켜기
            if (currentUnit.movement != null)
            {
                currentUnit.movement.ShowMoveRange(selectedCard.pattern);
            }
        }
    }

    // 🖱️ 매 프레임마다 마우스 클릭을 감지함
    void Update()
    {
        // 1. 카드를 쥐고 있지 않거나 내 기물이 없으면 마우스 클릭을 무시
        if (selectedCard == null || currentUnit == null) return;

        // 2. 좌클릭: 파란색 타일을 눌러서 이동/공격 확정!
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
                        // 1) 물리적 이동 및 공격 실행
                        currentUnit.movement.TryMoveTo(clickedTile);

                        // 2) 덱 매니저에게 장부상 카드 버리라고 알림
                        if (DeckManager.Instance != null)
                        {
                            DeckManager.Instance.DiscardCard(selectedCard);
                        }

                        // 3) 🚨 [핵심] 성공적으로 썼으니 화면에서 카드를 파괴합니다!
                        if (selectedCardObj != null)
                        {
                            Destroy(selectedCardObj);
                        }

                        // 4) 맵에 켜진 파란 불 끄고 손 비우기
                        MapManager.Instance.ClearHighlights();
                        selectedCard = null;
                        selectedCardObj = null; // 손에서 몸통도 놓아줌
                    }
                    else
                    {
                        Debug.Log("⚠️ 이동할 수 없는 타일입니다!");
                    }
                }
            }
        }

        // 3. 우클릭: 행동 취소 (파란 불 끄고 카드 다시 내려놓기)
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Debug.Log("🔄 카드 사용 취소");
            selectedCard = null;
            selectedCardObj = null;
            MapManager.Instance.ClearHighlights();
        }
    }
}