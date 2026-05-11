using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
    [Header("기물 프리팹 리스트 (자동 세팅됨)")]
    public GameObject[] unitPrefabs;

    [Header("기물 데이터 리스트 (자동 세팅됨)")]
    public UnitData[] unitDatas;

    [Header("UI 패널")]
    public GameObject selectionPanel;
    public GameObject confirmPanel;
    public GameObject recallPanel;

    [Header("배치 제한")]
    public int maxUnits = 3;
    public int currentUnitCount = 0;
    private Tile selectedTileComponent;
    private Vector3 selectedTilePosition;
    private int selectedUnitIndex = -1;

    [Header("시각 효과 색상")]
    public Color hoverColor = new Color(0.5f, 1f, 0.5f, 1f);
    public Color errorColor = new Color(1f, 0.5f, 0.5f, 1f);

    [Header("페이드 연출")]
    public Image fadeImage;
    public float fadeDuration = 1.0f;
    private Tile hoveredTile;

    void Start()
    {
        BattleData.placedUnits.Clear();
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (recallPanel != null) recallPanel.SetActive(false);

        // 🌟 [자동화 마법] 이전 씬(캐릭터 선택)에서 골라둔 데이터를 자동으로 쏙 빼옵니다!
        if (StageManager.SelectedPartyMembers != null && StageManager.SelectedPartyMembers.Length > 0)
        {
            int count = StageManager.SelectedPartyMembers.Length;
            unitDatas = new UnitData[count];
            unitPrefabs = new GameObject[count];
            maxUnits = count; // 배치 한도도 선택한 캐릭터 숫자에 맞게 알아서 조절!

            for (int i = 0; i < count; i++)
            {
                if (StageManager.SelectedPartyMembers[i] != null && StageManager.SelectedPartyMembers[i].unitData != null)
                {
                    unitDatas[i] = StageManager.SelectedPartyMembers[i].unitData;
                    unitPrefabs[i] = unitDatas[i].unitPrefab;
                }
            }
            Debug.Log($"✅ 로비에서 선택한 {count}명의 캐릭터 목록을 자동 연동했습니다!");
        }
        else
        {
            Debug.LogWarning("⚠️ StageManager에서 넘어온 캐릭터 데이터가 없습니다! (수동 테스트 모드)");
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        bool isAnyPanelActive = selectionPanel.activeSelf || confirmPanel.activeSelf || recallPanel.activeSelf;
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (!isAnyPanelActive && !isPointerOverUI) ProcessHover();
        else ClearHover();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!isAnyPanelActive && !isPointerOverUI) DetectTile();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ResetPlacement();
        }
    }

    void DetectTile()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Tile"))
            {
                Tile clickedTile = hit.collider.GetComponent<Tile>();
                if (clickedTile != null)
                {
                    selectedTileComponent = clickedTile;
                    selectedTilePosition = hit.collider.transform.position;

                    if (!clickedTile.isDeployableZone) return;

                    if (clickedTile.isOccupied)
                    {
                        recallPanel.SetActive(true);
                    }
                    else
                    {
                        if (currentUnitCount >= maxUnits) return;
                        OpenSelectionUI();
                    }
                }
            }
        }
    }

    void OpenSelectionUI() => selectionPanel.SetActive(true);

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
            // 🚨 선택한 번호의 프리팹이 진짜 있는지 한 번 더 방어!
            if (unitPrefabs.Length <= selectedUnitIndex || unitPrefabs[selectedUnitIndex] == null)
            {
                Debug.LogWarning("🚨 선택한 유닛의 프리팹이 비어있습니다! UnitData를 확인해 주세요.");
                ResetPlacement();
                return;
            }

            Vector3 spawnPos = selectedTileComponent.GetComponent<Collider>().bounds.center;
            spawnPos.z = 0f;

            GameObject spawnedUnit = Instantiate(unitPrefabs[selectedUnitIndex], spawnPos, Quaternion.identity);

            selectedTileComponent.currentUnit = spawnedUnit;
            selectedTileComponent.isOccupied = true;
            selectedTileComponent.placedUnitIndex = selectedUnitIndex;
            currentUnitCount++;
            ResetPlacement();
        }
    }

    public void ConfirmRecall()
    {
        if (selectedTileComponent != null && selectedTileComponent.currentUnit != null)
        {
            Destroy(selectedTileComponent.currentUnit);
            selectedTileComponent.currentUnit = null;
            selectedTileComponent.isOccupied = false;
            selectedTileComponent.placedUnitIndex = -1;
            currentUnitCount--;
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

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Tile"))
        {
            Tile tileScript = hit.collider.GetComponent<Tile>();
            if (tileScript != null)
            {
                if (hoveredTile != tileScript)
                {
                    ClearHover();
                    hoveredTile = tileScript;

                    if (!tileScript.isDeployableZone || tileScript.isOccupied || currentUnitCount >= maxUnits)
                        tileScript.SetHoverColor(errorColor);
                    else
                        tileScript.SetHoverColor(hoverColor);
                }
                return;
            }
        }
        ClearHover();
    }

    void ClearHover()
    {
        if (hoveredTile != null)
        {
            hoveredTile.ResetColor();
            hoveredTile = null;
        }
    }

    public void OnClickSortie()
    {
        if (currentUnitCount == 0) return;

        System.Collections.Generic.List<PartyMemberInfo> newPartyList = new System.Collections.Generic.List<PartyMemberInfo>();
        Tile[] allTiles = FindObjectsOfType<Tile>();

        foreach (Tile tile in allTiles)
        {
            if (tile.isOccupied && tile.placedUnitIndex >= 0)
            {
                PartyMemberInfo info = new PartyMemberInfo();

                // 🌟 여기서 빈 상자 에러가 났었죠! 이제 무조건 데이터가 들어있습니다.
                info.unitData = unitDatas[tile.placedUnitIndex];

                int gridX = Mathf.RoundToInt(tile.transform.position.x + 3.5f);
                int gridY = Mathf.RoundToInt(tile.transform.position.y + 3.5f);

                info.file = (File)gridX;
                info.rank = gridY + 1;

                newPartyList.Add(info);
            }
        }

        StageManager.SelectedPartyMembers = newPartyList.ToArray();
        Debug.Log($"🚀 출격! 총 {newPartyList.Count}개의 기물을 StageManager에 저장했습니다.");

        StartCoroutine(FadeOutAndLoadScene("BattleScene"));
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color imageColor = fadeImage.color;
            imageColor.a = 0f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                imageColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
                fadeImage.color = imageColor;
                yield return null;
            }
            imageColor.a = 1f;
            fadeImage.color = imageColor;
        }
        SceneManager.LoadScene(sceneName);
    }
}