using UnityEngine;
// 유닛 정보 + 체스판 좌표를 하나로 묶은 상자
[System.Serializable]
public class PartyMemberInfo
{
    public UnitData unitData;
    public int rank;
    public File file;
}

public static class StageManager
{
    public static StageData SelectedStage;
    public static PartyMemberInfo[] SelectedPartyMembers; // 🌟 묶음 상자 배열로 변경!
}