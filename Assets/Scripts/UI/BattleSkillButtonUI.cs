using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleSkillButtonUI : MonoBehaviour
{
    public static BattleSkillButtonUI Instance { get; private set; }

    private RectTransform root;
    private TextMeshProUGUI titleText;
    private TMP_FontAsset koreanFont;
    private readonly List<Button> skillButtons = new List<Button>();
    private Unit cachedUnit;

    private void Awake()
    {
        Instance = this;
        BuildUI();
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        Unit currentUnit = TurnManager.Instance != null ? TurnManager.Instance.GetCurrentUnit() : null;
        if (currentUnit != cachedUnit)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        cachedUnit = TurnManager.Instance != null ? TurnManager.Instance.GetCurrentUnit() : null;

        if (root == null)
        {
            BuildUI();
        }

        SkillData[] skills = cachedUnit != null ? cachedUnit.GetSkills() : System.Array.Empty<SkillData>();
        bool hasSkills = cachedUnit != null && skills.Length > 0;
        root.gameObject.SetActive(cachedUnit != null);

        titleText.text = cachedUnit == null ? "스킬" : $"{cachedUnit.unitName} 스킬";
        EnsureButtonCount(Mathf.Max(1, skills.Length));

        for (int i = 0; i < skillButtons.Count; i++)
        {
            Button button = skillButtons[i];
            button.gameObject.SetActive(i < Mathf.Max(1, skills.Length));
            button.onClick.RemoveAllListeners();

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (!hasSkills)
            {
                label.text = "스킬 없음";
                button.interactable = false;
                continue;
            }

            SkillData skill = skills[i];
            label.text = BuildSkillButtonText(cachedUnit, skill);
            button.interactable = skill != null && cachedUnit.CanUseSkill(skill);

            if (skill != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (cachedUnit != null && cachedUnit.UseSkill(skill))
                    {
                        Refresh();
                    }
                });
            }
        }
    }

    private void BuildUI()
    {
        if (root != null) return;

        koreanFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Mulmaru SDF");

        GameObject panel = new GameObject("Battle Skill Buttons", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);

        root = panel.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-24f, 92f);
        root.sizeDelta = new Vector2(230f, 154f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 10);
        layout.spacing = 6f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        GameObject title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        title.transform.SetParent(panel.transform, false);
        titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.text = "스킬";
        ApplyFont(titleText);
        titleText.fontSize = 18f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        title.AddComponent<LayoutElement>().preferredHeight = 24f;
    }

    private void EnsureButtonCount(int count)
    {
        while (skillButtons.Count < count)
        {
            GameObject buttonObject = new GameObject($"Skill Button {skillButtons.Count + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(root, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 1f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            button.colors = colors;

            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 34f;

            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 2f);
            labelRect.offsetMax = new Vector2(-8f, -2f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            ApplyFont(label);
            label.fontSize = 14f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.black;
            label.overflowMode = TextOverflowModes.Ellipsis;

            skillButtons.Add(button);
        }
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (text != null && koreanFont != null)
        {
            text.font = koreanFont;
        }
    }

    private string BuildSkillButtonText(Unit unit, SkillData skill)
    {
        if (unit == null || skill == null) return "스킬 없음";

        string text = skill.skillName;
        int cooldown = unit.GetSkillCooldown(skill);
        if (cooldown > 0)
        {
            text += $" (CT {cooldown})";
        }

        int limit = unit.GetSkillUseLimit(skill.skillName);
        if (limit > 0)
        {
            text += $" {unit.GetSkillUseCount(skill.skillName)}/{limit}";
        }

        return text;
    }
}
