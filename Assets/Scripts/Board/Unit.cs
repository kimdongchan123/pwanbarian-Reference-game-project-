using UnityEngine;

[RequireComponent(typeof(BattleUnit))] // 🌟 드디어 진짜 RPG 스탯이 들어옵니다!
public class Unit : MonoBehaviour
{
    [Header("기본 정보")]
    public string unitName = "테스트 유닛";
    public bool isAlly = true;

    [Header("핵심 시스템 모듈")]
    public UnitMovement movement;
    public BattleUnit battleUnit; // 🌟 가짜 UnitStats를 밀어내고 진짜가 왔습니다!

    // TurnManager가 체력을 물어볼 때 진짜 BattleUnit의 체력을 대답해줍니다.
    public int currentHp
    {
        get => battleUnit != null ? battleUnit.currentHp : 0;
        set { if (battleUnit != null) battleUnit.currentHp = value; }
    }
    public int maxHp => battleUnit != null && battleUnit.data != null ? battleUnit.data.maxHp : 1;

    void Awake()
    {
        movement = GetComponent<UnitMovement>();
        battleUnit = GetComponent<BattleUnit>();
    }

    // 외부에서 찌르면 진짜 BattleUnit에게 전달합니다.
    public void TakeDamage(int damage)
    {
        if (battleUnit != null) battleUnit.TakeDamage(damage, DamageType.Physical);
    }

    public void AddStatus(StatusEffectType type, int amount)
    {
        if (battleUnit != null) battleUnit.AddStatus(type, amount);
    }
}