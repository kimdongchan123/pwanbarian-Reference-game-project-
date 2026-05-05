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
    private TextMeshProUGUI stageNameText;

    [SerializeField]
    private PlayerSelectButton[] partyMemberSlots;
    [SerializeField]
    private GameObject enemyInfoPanel;
    [SerializeField]
    private GameObject enemySlotPrefab;

    private UnitData[] partyMembers = new UnitData[3];

    [SerializeField]
    private StageData testStageData;

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
            EnemySlot newSlot = Instantiate(enemySlotPrefab, enemyInfoPanel.transform).GetComponent<EnemySlot>();
            newSlot.SetEnemyPrefab(enemyData.prefab);
            newSlot.gameObject.GetComponent<Image>().sprite = enemyData.prefab.GetComponent<SpriteRenderer>().sprite;
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

    public void StartGame()
    {
        StageManager.SelectedPartyMembers = partyMembers;
        SceneManager.LoadScene("SettingPlaceScene");
    }
}
