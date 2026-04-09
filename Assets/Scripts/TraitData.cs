using UnityEngine;

public enum TraitEffect
{
    none,
    grogyEscape,    // 불허: 출격 중 그로기 1회 무시
    avoidance,      // 회피: 자신의 DEF이하의 공격을 받지 않고 넉백되지 않는다.
    clearyourmind,  // 정신 가다듬기: 턴 시작 시 St(stAmount)를 회복한다.
    struggling,     // 생존발악: 아군이 사망하면 [정신](5) 피해를 받고 <ATK 증가>(1)을 얻는다.
    heroOfTribe,    // 바다민족의 영웅: 턴시작 아군(affiliationTarget) ST 회복 + ATK 증가
    callOfTribe,    // 민족의 부름: 턴종료 바다민족 소환
    swiftness,      // 신속: 턴마다 1번 추가 행마
    boss,           // 보스: 아군 생존 시 타겟 불가, HP 유지
    seaDisaster,    // 바다의 재앙: 젖음 50% + 바다 포인트 ATK/재빠름/확산
    elite,          // 엘리트: maxHp×50%로 시작, HP 유지
}

[CreateAssetMenu(fileName = "NewTrait", menuName = "Trait/TraitData")]
public class TraitData : ScriptableObject
{
    [Header("특성 이름")]
    public string TraitName;


    [Header("특성 효과")]
    public TraitEffect traitEffect;

    [Header("특성 설명")]
    [TextArea(3, 5)]
    public string Description;

    [Header("ST 수치 (clearyourmind, heroOfTribe 용)")]
    public int stAmount;

    [Header("대상 종족 (heroOfTribe, callOfTribe 용)")]
    public string affiliationTarget;
}
