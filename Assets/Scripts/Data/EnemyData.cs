// ▶ 레퍼런스 프로젝트 경로: Assets/Scripts/Data/EnemyData.cs 신규 추가
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum DefensiveCharacteristic
{
    none,           //없음
    avoidance,      //회피
    getaway,        //도주
    parry,          //패링
    guard,          //가드
    offset,         //상쇄
    counter,        //반격
    endure          //감내
}

public enum UniversalCharacteristics
{
    none,           //없음
    pollution,      //오염
    swiftness,      //신속
    indomitable,    //불굴
    combo,          //연격
    combon,         //연격 n번
    elite,          //엘리트
    boss,           //보스
    strongmind,     //다잡은마음
    machinespirit,  //기계정신
    flight,         //비행
    rangedattack    //원거리공격
}

public enum PieceCharacteristic
{
    none,                       //없음
    horizontalspread,           //확산-가로
    verticalspread,             //확산-세로
    diagonalspread_forward,     //확산-빗금
    diagonalspread_backward,    //확산-사선
    piercing,                   //관통
    leap,                       //도약
    multistrike,                //연타
    singleuse,                  //일회성
    rangedonly                  //원거리 공격 전용
}

public enum Panic
{
    none,                   //없음
    delirium,               //착란
    panic,                  //공황
    instinct,               //본능
    aggression,             //공격성표출
    fanaticism,             //광신
    denial,                 //부정
    resignation,            //체념
    vengeance,              //복수
    loss,                   //상실
    composure,              //침착
    flashback,              //주마등
    nobility,               //고결함
    split,                  //분열
    ecstasy,                //뜨거운 환락
    obsession,              //과몰입
    heartbeat,              //두근두근
    fury,                   //열불
    weakness,               //나약
    grit,                   //근성
    stubbornness,           //오기
    despair,                //망념
    resolve,                //각오
    fear,                   //공포
    nausea,                 //구역감
    stun,                   //기절
    seizure,                //발작
    self_harm,              //자해
    suicide,                //자살
    self_destruct,          //자폭
    self_defense,           //자기방어
    berserkers_soul,        //광전사의혼
    lavaeruption,           //용암분출
    just_kidding,           //넝담ㅋ
    distortedtime,          //엇나간시간
    momentum_suppression,   //기세&위축
    demonking_a,            //마왕
    confusion,              //혼란
    frozen,                 //얼어붙음
    cower,                  //움츠림
    hohoho,                 //호호호
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName;

    [Header("기본 스탯")]
    public int maxHp;
    public int maxSt;
    public int minSp;
    public int maxSp;
    public int atk;
    public int def;

    [Header("패닉")]
    public Panic[] panic;

    [Header("기물 키워드")]
    public string affiliation;
    public string unitTypeKeyword;
    public List<string> traitKeywords = new List<string>();

    [Header("공격 내성")]
    public float physicalResist = 1f;
    public float mentalResist = 1f;
    public float specialResist = 1f;
    public float sinResist = 1f;

    [Header("방어 특성")]
    public DefensiveCharacteristic[] defensiveCharacteristic;
    [Header("범용 특성")]
    public UniversalCharacteristics[] universalCharacteristic;
    [Header("피스 특성")]
    public PieceCharacteristic[] pieceCharacteristic;

    [Header("연결 데이터")]
    public SkillData[] skills;
    public TraitData[] traits;
    public CardData[] deck;
}
