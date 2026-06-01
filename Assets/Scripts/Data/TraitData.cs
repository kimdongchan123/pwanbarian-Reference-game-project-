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
    seaDisaster,           // 바다의 재앙: 젖음 50% + 바다 포인트 ATK/재빠름/확산
    elite,                 // 엘리트: maxHp×50%로 시작, HP 유지
    absorption,            // 섭취: 상대 HP 20%이하 시 즉사 후 이동, Hp+40 St+30
    javaSpawn,             // 자바무너: 출격 시 다리 8개 소환
    machineSpirit,         // 기계정신: St/패닉 없음, 피스 가치 0
    stealthTentacle,       // 스텔스 무너: 연타+상태이상
    flight,                // 비행: 넘어 이동/공격, 포인트 효과 X
    aerialAcrobatics,      // 공중곡예: 회피 취급, Sp 4이하 공격 면역
    wyvernBreath,          // 와이번 브레스: 원거리 적중 시 화상 소모 발동
    youngDragon,           // 어린 용: 공격 적중 시 화상 부여
    dragonScale,           // 용의 비늘: 턴 시작 보호막 40
    dragonBreath,          // 드래곤 브레스: 원거리 적중 시 화상 전부 소모 발동
    ragingFlame,           // 거센 불길: 전투 시작 마나 소모 후 확산 연소
    antArmy,               // 개미군세: 아군 개미왕국 수만큼 Sp 증가
    giantKing,             // 거인왕: 아군 거인 ATK 증가, 생존 거인×3 ATK
    floodingSeaDisaster,   // 범람하는 바다의 재앙: 젖음 면역, 바다 포인트 확장
    parry,                 // 패마: 자신보다 ATK+DEF 합이 낮은 공격 무효화, 차이만큼 ST 반격
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
