using UnityEngine;

// 이동 패턴 종류 (폰 바리안 스타일)
public enum MovePattern { Pawn, Knight, Bishop, Rook, Queen, King }

[CreateAssetMenu(fileName = "New Test Card", menuName = "Battle System/Test Card Data")]
public class TestCardData : ScriptableObject
{
    public string cardName = "Test Card";
    public int cost = 1;
    public MovePattern pattern;
}