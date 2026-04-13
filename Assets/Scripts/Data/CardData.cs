// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Data/CardData.cs 교체
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public PieceType pieceType;
    public SinType sinType;
    public DamageType damageType;
    public CardTargetType targetType = CardTargetType.Enemy;

    [Header("원거리 공격")]
    public bool isRanged = false;      // 원거리 공격 여부
    public int attackRange = 1;        // 공격 사거리 (타일 수, isRanged=true일 때 유효)

    [FormerlySerializedAs("damage")]
    public int power = 0;

    public bool useEffect = false;
    public StatusEffectType effectType = StatusEffectType.None;
    public int effectAmount = 0;

    [TextArea(2, 6)]
    public string description;
}
