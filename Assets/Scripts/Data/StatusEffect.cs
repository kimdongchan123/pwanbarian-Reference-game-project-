using System;

public class StatusEffect
{
    public StatusEffectType type;
    public int amount;

    public StatusEffect(StatusEffectType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}
