using UnityEngine;

public enum TraitTriggerType
{
    Passive,
    TurnStart,
    TurnEnd,
    OnAttack,
    OnHit,
    OnDeath
}

[CreateAssetMenu(menuName = "Game/Trait Data")]
public class TraitData : ScriptableObject
{
    public string traitName;
    public TraitTriggerType triggerType;

    [TextArea(2, 6)]
    public string description;
}