using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    [Header("기물 프리팹 리스트")]
    public GameObject[] unitPrefabs;

    [Header("UI 패널")]
    public GameObject selectionPanel;
    public GameObject confirmPanel;

    private Vector3 selectedTilePosition;
    private int selectedUnitIndex = -1;

    void Update()
    {
        if (Mouse.current == null) return;


        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log(" 1. 마우스 클릭 감지됨!");

            if (!selectionPanel.activeSelf && !confirmPanel.activeSelf)
            {
                Debug.Log(" 2. 패널 꺼짐 확인, DetectTile 실행!");
                DetectTile();
            }
            else
            {
                Debug.Log(" 클릭 무시됨: UI 패널이 켜져있습니다.");
            }
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


        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log(" 3. 레이저 명중! 맞은 물체: " + hit.collider.name);

            if (hit.collider.CompareTag("Tile"))
            {
                Debug.Log("4. 타일 확인 완료! UI를 엽니다.");
                selectedTilePosition = hit.collider.transform.position;
                OpenSelectionUI();
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
        if (selectedUnitIndex != -1)
        {
            Instantiate(unitPrefabs[selectedUnitIndex], selectedTilePosition, Quaternion.identity);
            ResetPlacement();
        }
    }

    public void ResetPlacement()
    {
        selectedUnitIndex = -1;
        selectionPanel.SetActive(false);
        confirmPanel.SetActive(false);
    }
}