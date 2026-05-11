using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            Debug.LogWarning($"이미 {member.unitName}이(가) 파티에 포함되어 있습니다.");
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
                if (i == 0)
                    instantiatedPlayer.GetComponent<Image>().color = Color.red;
                else if (i == 1)
                    instantiatedPlayer.GetComponent<Image>().color = Color.green;
                else if (i == 2)
                    instantiatedPlayer.GetComponent<Image>().color = Color.blue;
            }
        }

        partyUI.SetActive(false);
        positioningUI.SetActive(true);
    }

    public void StartGame()
    {
        StageManager.SelectedPartyMembers = new PlayerEntry[instantiatedPlayers.Length];
        for (int i = 0; i < instantiatedPlayers.Length; i++)
        {
            if (instantiatedPlayers[i] != null)
            {
                // 🌟 [수정된 부분] 과거 위치(PreviousParent)를 무시하고, '현재 속해있는 타일'의 위치만 정확하게 가져옵니다!
                int tileIndex = instantiatedPlayers[i].transform.parent.GetSiblingIndex();

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

    private IEnumerator ShowWarningMessage()
    {
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        warningText.gameObject.SetActive(false);
    }
}
