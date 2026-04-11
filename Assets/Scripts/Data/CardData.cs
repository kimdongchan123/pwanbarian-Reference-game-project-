// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Data/CardData.cs 교체
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public PieceType pieceType;
    public DamageType damageType;
    public CardTargetType targetType = CardTargetType.Enemy;

    [FormerlySerializedAs("damage")]
    public int power = 0;

    public bool useEffect = false;
    public StatusEffectType effectType = StatusEffectType.None;
    public int effectAmount = 0;

    [TextArea(2, 6)]
    public string description;
}
