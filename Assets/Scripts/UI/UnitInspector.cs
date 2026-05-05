using TMPro;
using UnityEngine;

public class UnitInspector : MonoBehaviour
{
    private static UnitInspector instance;
    public static UnitInspector Instance
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
    private TextMeshProUGUI unitNameText;

    [SerializeField]
    private TextMeshProUGUI HPText;

    [SerializeField]
    private TextMeshProUGUI ATKText;

    [SerializeField]
    private TextMeshProUGUI DEFText;

    [SerializeField]
    private TextMeshProUGUI STText;

    [SerializeField]
    private TextMeshProUGUI SPText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        gameObject.SetActive(false);
    }

    public void ShowEnemyInfo(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            HideInfo();
            return;
        }
        gameObject.SetActive(true);
        unitNameText.text = enemyData.unitName;
        HPText.text = enemyData.maxHp.ToString();
        ATKText.text = enemyData.atk.ToString();
        DEFText.text = enemyData.def.ToString();
        STText.text = enemyData.maxSt.ToString();
        SPText.text = enemyData.maxSp.ToString();
    }

    public void ShowUnitInfo(UnitData unitData)
    {
        if (unitData == null)
        {
            HideInfo();
            return;
        }
        gameObject.SetActive(true);
        unitNameText.text = unitData.unitName;
        HPText.text = unitData.maxHp.ToString();
        ATKText.text = unitData.maxAtk.ToString();
        DEFText.text = unitData.def.ToString();
        STText.text = unitData.maxSt.ToString();
        SPText.text = unitData.maxSp.ToString();
    }

    public void HideInfo()
    {
        gameObject.SetActive(false);
    }
}
