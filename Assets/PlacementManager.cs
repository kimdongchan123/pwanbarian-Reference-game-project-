using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
public class PlacementManager : MonoBehaviour
{
    [Header("기물 프리팹 리스트")]
    public GameObject[] unitPrefabs;

    [Header("UI 패널")]
    public GameObject selectionPanel;
    public GameObject confirmPanel;
    public GameObject recallPanel;
    [Header("배치 제한")]
    public int maxUnits = 3; // 최대 배치 가능 수
    public int currentUnitCount = 0; // 현재 배치된 기물 수 (인스펙터에서 보기 위해 public)
    private Tile selectedTileComponent;
    private Vector3 selectedTilePosition;
    private int selectedUnitIndex = -1;
    [Header("시각 효과 색상")]
    public Color hoverColor = new Color(0.5f, 1f, 0.5f, 1f); // 연한 녹색 (배치 가능)
    public Color errorColor = new Color(1f, 0.5f, 0.5f, 1f); // 연한 붉은색 (배치 불가)
    [Header("페이드 연출")]
    public Image fadeImage; // 캔버스에 만든 페이드용 검은 이미지
    public float fadeDuration = 1.0f; // 페이드 아웃에 걸리는 시간 (1초)
    private Tile hoveredTile; // 현재 마우스가 올라가 있는 타일 기억용
    void Start()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (recallPanel != null) recallPanel.SetActive(false);
    }
    void Update()
    {
        if (Mouse.current == null) return;

        //  방어 1단계: 3개의 패널 중 하나라도 켜져 있는지 확인 (recallPanel 포함)
        bool isAnyPanelActive = selectionPanel.activeSelf || confirmPanel.activeSelf || recallPanel.activeSelf;

        //  방어 2단계: 마우스 포인터가 현재 UI 요소(버튼, 패널 등) 위에 있는지 확인 (클릭 관통 방지)
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        //  1. 호버(마우스 오버) 처리: 마우스가 움직일 때마다 항상 체크
        if (!isAnyPanelActive && !isPointerOverUI)
        {
            ProcessHover();
        }
        else
        {
            ClearHover(); // UI 창이 켜져있거나 마우스가 UI 위에 있으면 호버 효과 끄기
        }

        // 🟢 2. 마우스 좌클릭 처리 (기물 배치 및 회수)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log(" 1. 마우스 클릭 감지됨!");

            // 패널이 모두 꺼져 있고, 마우스가 UI 위에 있지도 않을 때만 타일을 탐지합니다.
            if (!isAnyPanelActive && !isPointerOverUI)
            {
                Debug.Log(" 2. 패널 꺼짐 및 UI 관통 없음 확인, DetectTile 실행!");
                DetectTile();
            }
            else
            {
                // 패널이 켜져 있거나 마우스가 UI 위에 있으면 타일 클릭을 무시합니다.
                Debug.Log(" 클릭 무시됨: UI 패널이 켜져있거나 마우스가 UI를 가리키고 있습니다.");
            }
        }

        //  3. 마우스 우클릭 처리 (언제든 작업 취소)
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Debug.Log(" 우클릭 감지됨: 진행 중인 UI 작업을 취소합니다.");
            ResetPlacement();
        }
    }

    void DetectTile()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log(" 3. 레이저 명중! 맞은 물체: " + hit.collider.name);

            if (hit.collider.CompareTag("Tile"))
            {
                Tile clickedTile = hit.collider.GetComponent<Tile>();

                if (clickedTile != null)
                {
                    // 클릭한 타일 정보를 미리 저장해둡니다.
                    selectedTileComponent = clickedTile;
                    selectedTilePosition = hit.collider.transform.position;
                    if (!clickedTile.isDeployableZone)
                    {
                        Debug.Log(" 클릭 무시됨: 이곳은 적군 진영(배치 불가 구역)입니다!");
                        return; // 여기서 멈춥니다! (UI가 안 열림)
                    }
                    if (clickedTile.isOccupied)
                    {
                        Debug.Log(" 이미 기물이 있는 타일입니다! 회수(Recall) UI를 엽니다.");
                        recallPanel.SetActive(true);
                    }
                    else // 비어있는 타일일 때
                    {
                        //  [새로 추가] 배치 한도 검사!
                        if (currentUnitCount >= maxUnits)
                        {
                            Debug.Log($" 배치 한도 초과! (현재 {currentUnitCount}/{maxUnits}) 더 이상 기물을 배치할 수 없습니다.");
                            // 여기에 나중에 "배치 한도를 초과했습니다!" 라는 경고 텍스트를 화면에 띄우는 코드를 넣으면 좋습니다.
                            return;
                        }

                        Debug.Log(" 타일 확인 완료! 비어있는 칸입니다. 배치 선택 UI를 엽니다.");
                        OpenSelectionUI();
                    }
                }
            }
            else
            {
                Debug.Log(" 맞은 물체가 타일(Tile)이 아닙니다. 현재 태그: " + hit.collider.tag);
            }
        }
        else
        {
            Debug.Log(" 레이저가 허공을 갈랐습니다 (아무것도 안 맞음).");
        }
    }

    void OpenSelectionUI()
    {
        selectionPanel.SetActive(true);
    }

    public void SelectUnit(int index)
    {
        selectedUnitIndex = index;
        selectionPanel.SetActive(false);
        confirmPanel.SetActive(true);
    }

    public void ConfirmPlacement()
    {
        if (selectedUnitIndex != -1 && selectedTileComponent != null)
        {
            // [수정] 2D에서는 + new Vector3(0, 0.5f, 0) 같은 높이 보정이 필요 없습니다!
            // 대신 타일의 완벽한 '정중앙' 좌표를 가져와서 Z축만 0으로 맞춰줍니다.
            Vector3 spawnPos = selectedTileComponent.GetComponent<Collider>().bounds.center;
            spawnPos.z = 0f; // 2D 렌더링을 위해 Z축을 0으로 통일

            // 1. 프리팹 생성
            GameObject spawnedUnit = Instantiate(unitPrefabs[selectedUnitIndex], spawnPos, Quaternion.identity);

            // 2. 타일 스크립트에 정보 저장!
            selectedTileComponent.currentUnit = spawnedUnit;
            selectedTileComponent.isOccupied = true;
            selectedTileComponent.placedUnitIndex = selectedUnitIndex;
            currentUnitCount++;
            Debug.Log(" 배치가 완료되었습니다.");
            ResetPlacement();
        }
    }
    public void ConfirmRecall()
    {
        if (selectedTileComponent != null && selectedTileComponent.currentUnit != null)
        {
            // 1. 타일 위에 있던 기물 삭제
            Destroy(selectedTileComponent.currentUnit);

            // 2. 타일 상태 초기화 (비어있음으로 변경)
            selectedTileComponent.currentUnit = null;
            selectedTileComponent.isOccupied = false;
            selectedTileComponent.placedUnitIndex = -1;
            currentUnitCount--;
            Debug.Log(" 기물을 성공적으로 회수했습니다.");
            ResetPlacement();
        }
    }
    public void ResetPlacement()
    {
        selectedUnitIndex = -1;
        selectedTileComponent = null;
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (recallPanel != null) recallPanel.SetActive(false);
    }
    void ProcessHover()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Tile"))
            {
                Tile tileScript = hit.collider.GetComponent<Tile>();

                if (tileScript != null)
                {
                    // 마우스가 '새로운 타일'로 넘어갔을 때만 색상을 업데이트합니다.
                    if (hoveredTile != tileScript)
                    {
                        ClearHover(); // 이전 타일 색상 원상복구
                        hoveredTile = tileScript; // 새 타일 기억

                        // 타일 상태에 따라 색상을 다르게 입혀줍니다!
                        if (!tileScript.isDeployableZone || tileScript.isOccupied)
                        {
                            tileScript.SetHoverColor(errorColor); // 배치 불가 (빨간색)
                        }
                        else if (currentUnitCount >= maxUnits)
                        {
                            // [추가] 비어있는 아군 타일이지만, 유닛을 더 이상 놓을 수 없을 때도 빨간색
                            tileScript.SetHoverColor(errorColor);
                        }
                        else
                        {
                            tileScript.SetHoverColor(hoverColor); // 배치 가능 (초록색)
                        }
                    }
                    return; // 성공적으로 타일 위에 있으므로 함수 종료
                }
            }
        }

        // 레이저가 타일에 맞지 않았다면(허공을 가리키면) 효과 끄기
        ClearHover();
    }

    void ClearHover()
    {
        // 기억하고 있던 타일이 있다면 색상을 복구하고 기억을 지웁니다.
        if (hoveredTile != null)
        {
            hoveredTile.ResetColor();
            hoveredTile = null;
        }
    }
    public void OnClickSortie()
    {
        if (currentUnitCount == 0)
        {
            Debug.Log("⚠️ 배치된 기물이 없습니다! 최소 1개 이상 배치해 주세요.");
            return;
        }

        // 1. 기존 바구니를 깨끗하게 비웁니다.
        BattleData.placedUnits.Clear();

        // 2. 씬에 있는 모든 타일을 찾아서 기물이 있는지 검사합니다.
        Tile[] allTiles = FindObjectsOfType<Tile>();
        foreach (Tile tile in allTiles)
        {
            if (tile.isOccupied)
            {
                // 기물이 있다면 정보를 묶어서 바구니(BattleData)에 담습니다.
                BattleData.UnitInfo info = new BattleData.UnitInfo();
                info.unitIndex = tile.placedUnitIndex;

                Vector3 pos = tile.GetComponent<Collider>().bounds.center;
                pos.z = 0f;
                info.position = pos;

                BattleData.placedUnits.Add(info);
            }
        }

        Debug.Log($"출격! 총 {BattleData.placedUnits.Count}개의 기물 데이터를 저장했습니다. 전투 씬으로 이동합니다.");

        // 3. 전투 씬으로 넘어갑니다. ("BattleScene" 부분은 실제 만드실 스테이지 이름으로 바꿔주세요!)
        StartCoroutine(FadeOutAndLoadScene("BattleScene"));
    }
    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        if (fadeImage != null)
        {
            // 페이드 이미지를 활성화하고 투명도를 0(투명)으로 시작합니다.
            fadeImage.gameObject.SetActive(true);
            Color imageColor = fadeImage.color;
            imageColor.a = 0f;
            fadeImage.color = imageColor;

            float elapsedTime = 0f;

            // fadeDuration(1초) 동안 반복하면서 투명도(a)를 0에서 1로 서서히 올립니다.
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                imageColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
                fadeImage.color = imageColor;

                yield return null; // 다음 프레임까지 대기
            }

            // 완전히 검어지게 확실히 고정
            imageColor.a = 1f;
            fadeImage.color = imageColor;
        }

        // 화면이 완전히 검어지면 그때 씬을 넘깁니다!
        SceneManager.LoadScene(sceneName);
    }
}


