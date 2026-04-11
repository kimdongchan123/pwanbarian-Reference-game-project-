using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("기본 정보")]
    public string unitName = "테스트 유닛"; // 기물 이름 (예: 나이트, 오크 등)
    public bool isAlly = true;              // true면 아군, false면 적군
    public int formationIndex = 0;          // 아군 편성 순서 (속도가 겹칠 때 먼저 움직일 순서)

    [Header("핵심 시스템 모듈 (자동 연결됨)")]
    public UnitMovement movement; // 이동 담당
    public UnitStats stats;       // 스탯 담당 (속도, 체력 등)

    void Awake()
    {
        // 게임이 시작될 때, 내 게임 오브젝트에 붙어있는 부품(스크립트)들을 자동으로 찾아옵니다.
        movement = GetComponent<UnitMovement>();
        stats = GetComponent<UnitStats>();

        // 만약 필수 부품을 빼먹고 안 붙였다면 콘솔에 경고를 띄워줍니다.
        if (movement == null) Debug.LogWarning($" {unitName} 오브젝트에 UnitMovement 스크립트가 안 붙어있습니다!");
        if (stats == null) Debug.LogWarning($" {unitName} 오브젝트에 UnitStats 스크립트가 안 붙어있습니다!");
    }
}