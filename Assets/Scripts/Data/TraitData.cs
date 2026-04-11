// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Data/TraitData.cs 교체
using UnityEngine;
using UnityEngine.Serialization;

public enum TraitTriggerType
{
    Passive,
    TurnStart,
    TurnEnd,
    OnAttack,
    OnHit,
    OnDeath
}

public enum TraitEffect
{
    none,
    grogyEscape,   // 불허: 출격 중 그로기 1회 무시
    avoidance,     // 회피
    clearyourmind, // 정신 가다듬기: 턴 시작 시 ST 회복
    struggling,    // 생존발악
    heroOfTribe,   // 바다민족의 영웅: 턴시작 아군 ST 회복 + ATK 증가
    callOfTribe,   // 민족의 부름: 턴종료 바다민족 소환
    swiftness,     // 신속: 턴마다 1번 추가 행마
    boss,          // 보스: 아군 생존 시 타겟 불가, HP 유지
    seaDisaster,   // 바다의 재앙: 젖음 50% + 바다 포인트 ATK/재빠름/확산
    elite,         // 엘리트: maxHp×50%로 시작, HP 유지
}

[CreateAssetMenu(menuName = "Game/Trait Data")]
public class TraitData : ScriptableObject
{
    public string traitName;
    public TraitTriggerType triggerType;
    public TraitEffect traitEffect;

    [TextArea(2, 6)]
    public string description;

    [Header("ST 수치 (clearyourmind, heroOfTribe 용)")]
    public int stAmount;

    [Header("대상 종족 (heroOfTribe, callOfTribe 용)")]
    public string affiliationTarget;
}
