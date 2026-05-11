using UnityEngine;

public enum BuffType
{
    AtkUp,
    DefenseUp,
    SpeedUp,
    DamageUp,
    Protection,
    AtkDown,
    DefenseDown,
    SpeedDown,
    DamageDown,
    Weakness
}

[System.Serializable]
public class ActiveBuff
{
    public BuffType type;
    public float value;
    public int duration;

    public ActiveBuff(BuffType type, float value, int duration)
    {
        this.type = type;
        this.value = value;
        this.duration = duration;
    }
}
