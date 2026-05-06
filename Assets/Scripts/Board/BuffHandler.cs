using UnityEngine;
using System.Collections.Generic;

public class BuffHandler : MonoBehaviour
{
    // 🌟 [SerializeField]를 붙이면 유니티 인스펙터 창에서 이 리스트를 실시간으로 볼 수 있습니다!
    [SerializeField] private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    public void AddBuff(BuffType type, float value, int duration)
    {
        activeBuffs.Add(new ActiveBuff(type, value, duration));
        Debug.Log($"{gameObject.name}: {type} 버프 적용!");
    }

    public float GetTotalModifier(BuffType type)
    {
        float total = 0;
        foreach (var buff in activeBuffs)
        {
            if (buff.type == type) total += buff.value;
        }
        return total;
    }

    public void TickBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].duration--;
            if (activeBuffs[i].duration <= 0) activeBuffs.RemoveAt(i);
        }
    }
}