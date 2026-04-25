using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("기본 정보")]
    public string unitName = "테스트 유닛"; // 기물 이름 (예: 나이트, 오크 등)
    public bool isAlly = true;              // true면 아군, false면 적군
    public int formationIndex = 0;          // 아군 편성 순서 (속도가 겹칠 때 먼저 움직일 순서)

    [Header("전투 스탯")]
    public int atk = 5;
    public int maxHp = 20;
    [HideInInspector] public int currentHp;

    [Header("핵심 시스템 모듈 (자동 연결됨)")]
    public UnitMovement movement; // 이동 담당
    public UnitStats stats;       // 스탯 담당 (속도, 체력 등)

    void Awake()
    {
        movement = GetComponent<UnitMovement>();
        stats = GetComponent<UnitStats>();
        if (movement == null) Debug.LogWarning($" {unitName} 오브젝트에 UnitMovement 스크립트가 안 붙어있습니다!");
        if (stats == null) Debug.LogWarning($" {unitName} 오브젝트에 UnitStats 스크립트가 안 붙어있습니다!");
    }

    void Start()
    {
        currentHp = maxHp;
    }
}