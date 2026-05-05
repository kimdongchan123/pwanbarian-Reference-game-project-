using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("기본 정보")]
    public string unitName = "테스트 유닛";
    public bool isAlly = true;
    public int formationIndex = 0;

    [Header("전투 스탯")]
    public int maxHp = 20;
    [HideInInspector] public int currentHp;

    [Header("핵심 시스템 모듈 (자동 연결됨)")]
    public UnitMovement movement;
    public UnitStats stats;
    private BuffHandler buffHandler;

    void Awake()
    {
        movement = GetComponent<UnitMovement>();
        stats = GetComponent<UnitStats>();
        buffHandler = GetComponent<BuffHandler>();

        if (movement == null) Debug.LogWarning($" {unitName} : UnitMovement 없음");
        if (stats == null) Debug.LogWarning($" {unitName} : UnitStats 없음");

        // 버프 핸들러가 없다면 자동으로 추가합니다.
        if (buffHandler == null) buffHandler = gameObject.AddComponent<BuffHandler>();
    }

    void Start()
    {
        currentHp = maxHp;
    }

    // 🌟 카드를 써서 공격할 때 호출되는 진짜 공격력 (버프 포함)
    public int GetAttackPower()
    {
        if (stats != null)
        {
            return stats.CurrentAttack;
        }
        return 5; // 예외 상황 시 기본값
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        Debug.Log($"{name}이(가) {damage} 피해를 받음. 남은 체력: {currentHp}");

        if (currentHp <= 0)
        {
            Debug.Log($"{name} 사망");
            // 사망 처리 로직 필요 시 추가
        }
    }

    // 🌟 카드 시스템에서 넘겨준 "AtkUp" 데이터를 버프로 바꿔줍니다!
    public void AddStatus(StatusEffectType type, int amount)
    {
        if (type == StatusEffectType.None) return;

        // "Atk" 단어가 포함되어 있으면 무조건 공격력 버프로 처리!
        if (type.ToString().Contains("Atk"))
        {
            // Amount 1당 10%(0.1f) 증가로 계산
            float buffValue = amount * 0.1f;

            // 1턴 동안 공격력 증가 부여
            buffHandler.AddBuff(BuffType.AtkUp, buffValue, 1);

            Debug.Log($"🃏 카드 효과 발동! {name}의 공격력이 {buffValue * 100}% 증가합니다! (1턴 지속)");
        }
        else
        {
            Debug.Log($"🃏 {type} 효과가 {amount}만큼 {name}에게 적용되었습니다. (추가 구현 필요)");
        }
    }

    // 🌟 턴이 끝날 때 버프 지속 시간을 1턴씩 깎습니다.
    public void OnTurnEnd()
    {
        if (buffHandler != null)
        {
            buffHandler.TickBuffs();
        }
    }
}