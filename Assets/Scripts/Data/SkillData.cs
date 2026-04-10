using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName;

    [TextArea(2, 5)]
    public string Description;

    public int coolTime = 0;
}
