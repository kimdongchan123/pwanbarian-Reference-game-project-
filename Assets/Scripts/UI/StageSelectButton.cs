using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectButton : MonoBehaviour
{
    public StageData stageData;

    public void StartStage()
    {
        StageManager.SelectedStage = stageData;
        SceneManager.LoadScene("SettingPlaceScene");
    }
}
