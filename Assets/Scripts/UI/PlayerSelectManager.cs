using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerSelectManager : MonoBehaviour
{
    private static PlayerSelectManager instance;
    public static PlayerSelectManager Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }

    [SerializeField]
    private GameObject partyUI;
    [SerializeField]
    private TextMeshProUGUI stageNameText;
    [SerializeField]
    private PlayerSelectButton[] partyMemberSlots;
    [SerializeField]
    private GameObject enemyInfoPanel;
    [SerializeField]
    private TextMeshProUGUI warningText;

    [SerializeField]
    private GameObject enemySlotPrefab;
    private UnitData[] partyMembers = new UnitData[3];

    [SerializeField]
    private StageData testStageData;

    [SerializeField]
    private GameObject positioningUI;
    [SerializeField]
    private Transform board;
    [SerializeField]
    private GameObject enemy;
    [SerializeField]
    private GameObject player;

    private DraggableUI[] instantiatedPlayers = new DraggableUI[3];

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (!StageManager.SelectedStage)
        {
            StageManager.SelectedStage = testStageData;
        }

        if (StageManager.SelectedStage != null)
        {
            stageNameText.text = StageManager.SelectedStage.stageName;
            SetupEnemySlots();
        }
    }

    private void SetupEnemySlots()
    {
        foreach (EnemyEntry enemyData in StageManager.SelectedStage.enemyEntries)
        {
            EnemySlot newSlot = Instantiate(enemySlotPrefab, enemyInfoPanel.transform)
                .GetComponent<EnemySlot>();
            newSlot.SetEnemyPrefab(enemyData.prefab);
            newSlot.gameObject.GetComponent<Image>().sprite = enemyData
                .prefab.GetComponent<SpriteRenderer>()
                .sprite;

            Transform tile = board.GetChild((8 - enemyData.rank) * 8 + (int)enemyData.file - 1);
            GameObject instantiatedEnemy = Instantiate(enemy, tile);
            instantiatedEnemy.GetComponent<Image>().sprite = enemyData
                .prefab.GetComponent<SpriteRenderer>()
                .sprite;
        }
    }

    public void AddPartyMembers(UnitData member)
    {
        if (System.Array.IndexOf(partyMembers, member) >= 0)
        {
            Debug.LogWarning($"?��? {member.unitName}??가) ?�티???�함?�어 ?�습?�다.");
            return;
        }

        for (int i = 0; i < partyMembers.Length; i++)
        {
            if (partyMembers[i] == null)
            {
                partyMembers[i] = member;
                break;
            }
        }

        ShowPartyMembers();
    }

    public void RemovePartyMembers(UnitData member)
    {
        for (int i = 0; i < partyMembers.Length; i++)
        {
            if (partyMembers[i] == member)
            {
                partyMembers[i] = null;
                break;
            }
        }

        ShowPartyMembers();
    }

    public void ShowPartyMembers()
    {
        for (int i = 0; i < partyMemberSlots.Length; i++)
        {
            if (i < partyMembers.Length && partyMembers[i] != null)
            {
                partyMemberSlots[i].SetUnitData(partyMembers[i]);
            }
            else
            {
                partyMemberSlots[i].SetUnitData(null);
            }
        }
    }

    public void OpenPositioningUI()
    {
        if (!System.Array.Exists(partyMembers, member => member != null))
        {
            StopCoroutine(nameof(ShowWarningMessage));
            StartCoroutine(nameof(ShowWarningMessage));
            return;
        }

        for (int i = 0; i < partyMembers.Length; i++)
        {
            if (partyMembers[i] != null)
            {
                Transform tile = board.GetChild(63 - i);
                GameObject instantiatedPlayer = Instantiate(player, tile);
                instantiatedPlayers[i] = instantiatedPlayer.GetComponent<DraggableUI>();
                instantiatedPlayers[i].SetUnitData(partyMembers[i]);
                ApplyPartyMemberFallbackColor(instantiatedPlayer, partyMembers[i], i);
            }
        }

        partyUI.SetActive(false);
        positioningUI.SetActive(true);
    }


    private void ApplyPartyMemberFallbackColor(GameObject instantiatedPlayer, UnitData unitData, int index)
    {
        Image image = instantiatedPlayer.GetComponent<Image>();
        if (image == null) return;

        Sprite sprite = unitData != null && unitData.battleSprite != null ? unitData.battleSprite : unitData?.portraitSprite;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            Debug.Log($"[PlayerSelectManager] Applied sprite '{sprite.name}' for {unitData.unitName}.");
            return;
        }

        if (unitData != null)
        {
            Debug.LogWarning($"[PlayerSelectManager] {unitData.unitName}: sprite is not assigned. Using fallback color.");
        }

        if (index == 0)
            image.color = Color.red;
        else if (index == 1)
            image.color = Color.green;
        else if (index == 2)
            image.color = Color.blue;
    }

    public void StartGame()
    {
        StageManager.SelectedPartyMembers = new PlayerEntry[instantiatedPlayers.Length];
        for (int i = 0; i < instantiatedPlayers.Length; i++)
        {
            if (instantiatedPlayers[i] != null)
            {
                int tileIndex = instantiatedPlayers[i].PreviousParent
                    ? instantiatedPlayers[i].PreviousParent.GetSiblingIndex()
                    : instantiatedPlayers[i].transform.parent.GetSiblingIndex();
                StageManager.SelectedPartyMembers[i] = new PlayerEntry
                {
                    unitData = instantiatedPlayers[i].UnitData,
                    rank = 8 - tileIndex / 8,
                    file = (File)(tileIndex % 8 + 1),
                };
            }
        }
        SceneManager.LoadScene("BattleScene");
    }

    public void ReturnByEscape(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (positioningUI.activeSelf)
        {
            ReturnToPartySelect();
        }
        else
        {
            ReturnToStageSelect();
        }
    }

    public void ReturnToStageSelect()
    {
        SceneManager.LoadScene("StageSelectScene");
    }

    public void ReturnToPartySelect()
    {
        for (int i = 0; i < instantiatedPlayers.Length; i++)
        {
            if (instantiatedPlayers[i] != null)
            {
                Destroy(instantiatedPlayers[i].gameObject);
                instantiatedPlayers[i] = null;
            }
        }
        positioningUI.SetActive(false);
        partyUI.SetActive(true);
    }

    private IEnumerator ShowWarningMessage()
    {
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        warningText.gameObject.SetActive(false);
    }
}
