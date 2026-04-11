// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Enums/BattleEnums.cs 교체
using UnityEngine;

public enum DamageType
{
    Physical,
    Mental,
    Special,
    Sin
}

public enum PanicType
{
    None,
    Weakness,
    BerserkerSoul
}

// Zebra, Camel, Giraffe 추가
public enum PieceType
{
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King,
    Giraffe,
    Zebra,
    Camel
}

public enum StatusEffectType
{
    None,
    Quick, Breath, Agitation, Charge, Shield,
    HuntHumanType, Rupture, Hallucination, Frenzy,
    GoldenTime, GoldenGuarantee, AtkUp, DefUp, DamageUp,
    Slow, Mark, Corrosion, Poison, Fear, Truth, Logic, Prey
}

public enum CardTargetType
{
    Enemy,
    Self
}
