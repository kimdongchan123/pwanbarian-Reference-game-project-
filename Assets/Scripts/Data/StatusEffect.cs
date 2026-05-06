using System;

public class StatusEffect
{
    public StatusEffectType type;
    public int amount;
    public int count;

    public StatusEffect(StatusEffectType type, int amount)
        : this(type, amount, 0)
    {
    }

    public StatusEffect(StatusEffectType type, int amount, int count)
    {
        this.type = type;
        this.amount = amount;
        this.count = count;
    }
}
