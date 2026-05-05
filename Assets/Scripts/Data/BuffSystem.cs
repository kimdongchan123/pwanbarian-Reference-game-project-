using UnityEngine;

// 🌟 기획자님 요청에 따라 AtkUp으로 통일
public enum BuffType
{
    AtkUp,       // 공격력 증가
    DefenseUp,   // 방어력 증가
    SpeedUp      // 이동 속도 증가
}

[System.Serializable]
public class ActiveBuff
{
    public BuffType type;
    public float value;
    public int duration;

    public ActiveBuff(BuffType type, float value, int duration)
    {
        this.type = type;
        this.value = value;
        this.duration = duration;
    }
}