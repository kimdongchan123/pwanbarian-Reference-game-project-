using UnityEngine;

[CreateAssetMenu(menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public PieceType pieceType;
    public DamageType damageType;
    public CardTargetType targetType = CardTargetType.Enemy;

    public int power = 0;

    public bool useEffect = false;
    public StatusEffectType effectType = StatusEffectType.None;
    public int effectAmount = 0;

    [TextArea(2, 6)]
    public string description;
}