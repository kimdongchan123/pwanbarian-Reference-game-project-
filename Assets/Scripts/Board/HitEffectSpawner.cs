using System;
using UnityEngine;

public static class HitEffectSpawner
{
    private const string BasePath = "HitEffects/";
    private const float DefaultScale = 0.38f;
    private const float DefaultFrameRate = 42f;
    private const int DefaultSortingOrder = 1000;

    public static void SpawnImpact(Vector3 position)
    {
        Spawn("ImpactGold", position, DefaultScale);
    }

    public static void SpawnForDamageType(DamageType damageType, Vector3 position)
    {
        string effectName = damageType switch
        {
            DamageType.Mental => "MagicPurple",
            DamageType.Special => "SlashBlue",
            DamageType.Sin => "MagicPurple",
            _ => "ImpactGold"
        };

        Spawn(effectName, position, DefaultScale);
    }

    public static void SpawnForCard(CardData card, Vector3 position)
    {
        if (card == null)
        {
            SpawnImpact(position);
            return;
        }

        string effectName = card.damageType switch
        {
            DamageType.Mental => "MagicPurple",
            DamageType.Special => "SlashBlue",
            DamageType.Sin => "MagicPurple",
            _ => IsSlashPiece(card.pieceType) ? "SlashRed" : "ImpactGold"
        };

        Spawn(effectName, position, DefaultScale);
    }

    public static void Spawn(string effectName, Vector3 position, float scale = DefaultScale)
    {
        Sprite[] frames = Resources.LoadAll<Sprite>(BasePath + effectName);
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"HitEffectSpawner: missing effect frames - {effectName}");
            return;
        }

        Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));

        Vector3 effectPosition = position;
        effectPosition.z = -0.5f;

        GameObject effectObject = new GameObject($"HitEffect_{effectName}");
        effectObject.transform.position = effectPosition;
        effectObject.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-8f, 8f));

        HitEffectPlayer player = effectObject.AddComponent<HitEffectPlayer>();
        player.Play(frames, DefaultFrameRate, scale, DefaultSortingOrder);
    }

    private static bool IsSlashPiece(PieceType pieceType)
    {
        return pieceType == PieceType.Knight
               || pieceType == PieceType.Bishop
               || pieceType == PieceType.Rook
               || pieceType == PieceType.Queen;
    }
}
