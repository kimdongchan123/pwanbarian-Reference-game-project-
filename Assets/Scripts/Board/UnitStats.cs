using UnityEngine;

public class UnitStats : MonoBehaviour
{
    [Header("기본 능력치")]
    public int baseAttack = 10;
    public int minSpeed = 1;
    public int maxSpeed = 5;

    [HideInInspector] public int currentTurnSpeed;

    private BuffHandler buffHandler;

    private void Awake()
    {
        buffHandler = GetComponent<BuffHandler>();
        if (buffHandler == null) buffHandler = gameObject.AddComponent<BuffHandler>();
    }

    public int CurrentAttack
    {
        get
        {
            // 🌟 버프 매니저에게 AtkUp 버프 수치를 물어봅니다.
            float modifier = buffHandler.GetTotalModifier(BuffType.AtkUp);
            return Mathf.CeilToInt(baseAttack * (1.0f + modifier));
        }
    }

    public void TakeDamage(int damage) { }
    public bool UseMana(int amount) => true;
}