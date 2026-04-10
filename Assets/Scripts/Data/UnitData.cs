using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName;
    public PanicType panicType;

    [Header("기본 스탯")]
    public int maxHp;
    public int maxSt;
    public int minSp;
    public int maxSp;
    public int minAtk;
    public int maxAtk;
    public int def;

    [Header("공격 내성")]
    public float physicalResist = 1f;
    public float mentalResist = 1f;
    public float specialResist = 1f;
    public float sinResist = 1f;

    [Header("기물 키워드")]
    public string affiliation;
    public string unitTypeKeyword;
    public List<string> traitKeywords = new List<string>();

    [Header("연결 데이터")]
    public TraitData[] traits;
    public SkillData[] skills;
    public CardData[] deck;
}