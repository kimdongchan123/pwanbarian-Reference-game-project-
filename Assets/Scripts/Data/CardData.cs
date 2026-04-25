using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public string cardName;

    public PieceType pieceType;
    public SinType sinType;
    public DamageType damageType;
    public CardTargetType targetType = CardTargetType.Enemy;

    [Header("원거리 공격")]
    public bool isRanged = false;
    public int attackRange = 1;

    [Header("공격력")]
    [FormerlySerializedAs("damage")]
    public int power = 0;

    [Header("카드 효과")]
    public bool useEffect = false;
    public StatusEffectType effectType = StatusEffectType.None;
    public int effectAmount = 0;

    [Tooltip("체크하면 자기 자신에게 효과 적용, 체크 해제하면 대상에게 효과 적용")]
    public bool effectToSelf = true;

    [TextArea(2, 6)]
    public string description;
}