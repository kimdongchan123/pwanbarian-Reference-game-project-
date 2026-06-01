using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInspector : MonoBehaviour
{
    private static UnitInspector instance;
    public static UnitInspector Instance
    {
        get
        {
            if (instance == null)
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

    [SerializeField]
    private TextMeshProUGUI skillText;

    [SerializeField]
    private TextMeshProUGUI statusText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        AutoBindOptionalTexts();
        SetupSkillTextLayout();
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
        ATKText.text = enemyData.minatk == enemyData.maxatk ? enemyData.maxatk.ToString() : $"{enemyData.minatk}~{enemyData.maxatk}";
        DEFText.text = enemyData.maxdef.ToString();
        STText.text = enemyData.maxSt.ToString();
        SPText.text = enemyData.minSp == enemyData.maxSp ? enemyData.maxSp.ToString() : $"{enemyData.minSp}~{enemyData.maxSp}";

        SetupSkillTextLayout();
        SetOptionalText(skillText, BuildSkillSummary(enemyData.skills));
        SetOptionalText(statusText, string.Empty);
    }

    public void ShowEnemyInfo(Enemy enemyPrefab)
    {
        if (enemyPrefab == null)
        {
            HideInfo();
            return;
        }

        ShowEnemyInfo(enemyPrefab.EnemyData);
        ApplyEnemyImage(enemyPrefab);
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
        ApplyUnitImage(unitData);
        HPText.text = unitData.maxHp.ToString();
        ATKText.text = unitData.minAtk == unitData.maxAtk ? unitData.maxAtk.ToString() : $"{unitData.minAtk}~{unitData.maxAtk}";
        DEFText.text = unitData.def.ToString();
        STText.text = unitData.maxSt.ToString();
        SPText.text = unitData.minSp == unitData.maxSp ? unitData.maxSp.ToString() : $"{unitData.minSp}~{unitData.maxSp}";

        SetupSkillTextLayout();
        SetOptionalText(skillText, BuildSkillSummary(unitData.skills));
        SetOptionalText(statusText, string.Empty);
    }

    public void ShowBattleUnitInfo(BattleUnit battleUnit)
    {
        if (battleUnit == null || battleUnit.data == null)
        {
            HideInfo();
            return;
        }

        ShowUnitInfo(battleUnit.data);
        HPText.text = $"{battleUnit.currentHp}/{battleUnit.data.maxHp}";
        STText.text = $"{battleUnit.currentSt}/{battleUnit.data.maxSt}";
        SetOptionalText(statusText, BuildStatusSummary(battleUnit));
    }

    private void ApplyUnitImage(UnitData unitData)
    {
        if (unitImage == null) return;

        Sprite sprite = unitData.portraitSprite != null ? unitData.portraitSprite : unitData.battleSprite;
        unitImage.sprite = sprite;
        unitImage.color = sprite != null ? Color.white : Color.clear;
        unitImage.type = Image.Type.Simple;
        unitImage.preserveAspect = true;
    }

    private void ApplyEnemyImage(Enemy enemyPrefab)
    {
        if (unitImage == null) return;

        SpriteRenderer spriteRenderer = enemyPrefab.GetComponent<SpriteRenderer>();
        Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        unitImage.sprite = sprite;
        unitImage.color = sprite != null ? Color.white : Color.clear;
        unitImage.type = Image.Type.Simple;
        unitImage.preserveAspect = true;
    }

    private string BuildSkillSummary(SkillData[] skills)
    {
        if (skills == null || skills.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < skills.Length; i++)
        {
            SkillData skill = skills[i];
            if (skill == null) continue;

            builder.AppendLine();
            builder.Append("- ");
            builder.Append(skill.skillName);
        }

        return builder.Length > 0 ? $"스킬 :{builder}" : string.Empty;
    }

    private string BuildStatusSummary(BattleUnit battleUnit)
    {
        if (battleUnit == null || battleUnit.statusEffects == null || battleUnit.statusEffects.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        foreach (StatusEffect status in battleUnit.statusEffects)
        {
            if (status == null || status.type == StatusEffectType.None) continue;

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(status.type);
            builder.Append($" {status.amount}");
            if (status.count > 0)
            {
                builder.Append($" / {status.count}");
            }
        }

        return builder.ToString();
    }

    private void SetOptionalText(TextMeshProUGUI target, string value)
    {
        if (target == null) return;
        target.text = value;
        target.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
    }

    private void AutoBindOptionalTexts()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            string lowerName = text.gameObject.name.ToLowerInvariant();
            if (skillText == null && lowerName.Contains("skill"))
            {
                skillText = text;
            }

            if (statusText == null && lowerName.Contains("status"))
            {
                statusText = text;
            }
        }
    }

    private void SetupSkillTextLayout()
    {
        if (SPText == null) return;

        if (skillText == null)
        {
            GameObject skillObject = new GameObject("SkillText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            skillObject.transform.SetParent(SPText.transform.parent, false);
            skillText = skillObject.GetComponent<TextMeshProUGUI>();
        }
        else if (skillText.transform.parent != SPText.transform.parent)
        {
            skillText.transform.SetParent(SPText.transform.parent, false);
        }

        skillText.transform.SetSiblingIndex(SPText.transform.GetSiblingIndex() + 1);
        skillText.font = SPText.font;
        skillText.fontSharedMaterial = SPText.fontSharedMaterial;
        skillText.fontSize = SPText.fontSize;
        skillText.color = SPText.color;
        skillText.alignment = TextAlignmentOptions.Left;
        skillText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        skillText.overflowMode = TextOverflowModes.Overflow;
        skillText.raycastTarget = false;

        RectTransform rectTransform = skillText.rectTransform;
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = SPText.rectTransform.anchorMin;
        rectTransform.anchorMax = SPText.rectTransform.anchorMax;
        rectTransform.pivot = SPText.rectTransform.pivot;
        rectTransform.sizeDelta = new Vector2(Mathf.Max(SPText.rectTransform.sizeDelta.x, 180f), 72f);

        LayoutElement layoutElement = skillText.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = skillText.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = false;
        layoutElement.preferredHeight = 72f;
    }

    public void HideInfo()
    {
        gameObject.SetActive(false);
    }
}
