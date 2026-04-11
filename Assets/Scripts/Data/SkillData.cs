// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Data/SkillData.cs 교체
using UnityEngine;
using UnityEngine.Serialization;

public enum SkillEffect
{
    none,
    damagebuff,  // 피해량 버프 (duration 동안 지속)
    atkbuff,     // ATK 영구 증가
    deepbreath   // ST 회복
}

[CreateAssetMenu(menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName;

    [TextArea(2, 5)]
    public string description;

    public int coolTime = 0;

    [Header("효과 (Enemy 전용)")]
    public SkillEffect skillEffect;
    public int effectValue;
    public int duration;
}
