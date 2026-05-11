using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic; // 🌟 리스트 사용을 위해 추가

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

    // 🌟 에러가 났던 StartGame() 함수 수정!
    public void StartGame()
    {
        // 1. 빈칸(null)을 제외하고 실제로 선택된 유닛들만 리스트에 담습니다.
        List<PartyMemberInfo> validMembers = new List<PartyMemberInfo>();

        for (int i = 0; i < partyMembers.Length; i++)
        {
            if (partyMembers[i] != null)
            {
                PartyMemberInfo info = new PartyMemberInfo();
                info.unitData = partyMembers[i];
                // 💡 지금은 아직 좌표(file, rank)를 모르는 상태이므로 비워둡니다.

                validMembers.Add(info);
            }
        }

        // 2. 완성된 리스트를 배열로 변환해서 매니저에 넘깁니다.
        StageManager.SelectedPartyMembers = validMembers.ToArray();

        Debug.Log($"✅ {validMembers.Count}명의 캐릭터가 선택되어 배치 씬으로 넘어갑니다.");

        // 3. 씬 이동
        SceneManager.LoadScene("SettingPlaceScene");
    }
}