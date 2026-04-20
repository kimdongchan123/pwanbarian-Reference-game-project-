using UnityEngine;
using UnityEngine.SceneManagement;

public class StartBattleButton : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    public string battleSceneName = "BattleScene";

    public void OnClickStartBattle()
    {
        if (PlayerSelectionData.Instance == null)
        {
            Debug.LogWarning("PlayerSelectionData가 없음");
            return;
        }

        if (!PlayerSelectionData.Instance.HasSelectedUnit())
        {
            Debug.LogWarning("캐릭터를 먼저 선택해야 함");
            return;
        }

        Debug.Log($"전투씬 이동: {PlayerSelectionData.Instance.selectedUnit.unitName}");
        SceneManager.LoadScene(battleSceneName);
    }
}
