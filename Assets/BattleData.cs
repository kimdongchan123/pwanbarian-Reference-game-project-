using UnityEngine;
using System.Collections.Generic;

// MonoBehaviour를 상속받지 않는 'static' 클래스입니다. 씬이 넘어가도 절대 파괴되지 않습니다!
public static class BattleData
{
    // 유닛의 종류(인덱스)와 위치를 묶어서 기억할 구조체
    public struct UnitInfo
    {
        public int unitIndex; // 프리팹 리스트의 몇 번째 유닛인지
        public Vector3 position; // 어느 위치(좌표)에 배치되었는지
    }

    // 배치된 유닛들의 정보를 담아둘 리스트 (바구니)
    public static List<UnitInfo> placedUnits = new List<UnitInfo>();
}