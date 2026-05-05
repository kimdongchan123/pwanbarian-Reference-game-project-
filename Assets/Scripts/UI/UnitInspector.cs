using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    private Image unitImage;

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
        // unitImage.sprite = enemyData.prefab.GetComponent<SpriteRenderer>().sprite;
        HPText.text = enemyData.maxHp.ToString();
        if (enemyData.minatk == enemyData.maxatk)
            ATKText.text = enemyData.maxatk.ToString();
        else
            ATKText.text = $"{enemyData.minatk}~{enemyData.maxatk}";
        DEFText.text = enemyData.def.ToString();
        STText.text = enemyData.maxSt.ToString();
        if (enemyData.minSp == enemyData.maxSp)
            SPText.text = enemyData.maxSp.ToString();
        else
            SPText.text = $"{enemyData.minSp}~{enemyData.maxSp}";
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
        if (unitData.minAtk == unitData.maxAtk)
            ATKText.text = unitData.maxAtk.ToString();
        else
            ATKText.text = $"{unitData.minAtk}~{unitData.maxAtk}";

        DEFText.text = unitData.def.ToString();
        STText.text = unitData.maxSt.ToString();
        if (unitData.minSp == unitData.maxSp)
            SPText.text = unitData.maxSp.ToString();
        else
            SPText.text = $"{unitData.minSp}~{unitData.maxSp}";
    }

    public void HideInfo()
    {
        gameObject.SetActive(false);
    }
}
