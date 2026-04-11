using UnityEngine;

//  임시 스크립트
public class UnitStats : MonoBehaviour
{
    [Header("임시 속도 데이터 (턴 매니저용)")]
    public int minSpeed = 1;
    public int maxSpeed = 5;

    [HideInInspector]
    public int currentTurnSpeed;

    // 다른 스크립트에서 실수로 호출하더라도 에러가 나지 않도록 빈 공간만 만들어 둡니다.
    public void TakeDamage(int damage)
    {
        Debug.Log("임시: 데미지 처리 함수 호출됨");
    }

    public bool UseMana(int amount)
    {
        return true; // 임시: 마나는 항상 무한이라고 가정
    }
}