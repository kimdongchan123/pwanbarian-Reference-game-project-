using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StageSelectButton : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI stageNameText;

    private StageData stageData;

    public void StartStage()
    {
        StageManager.SelectedStage = stageData;
        SceneManager.LoadScene("PlayerSelectScene");
    }

    public void SetStageData(StageData data)
    {
        stageData = data;
        stageNameText.text = data.stageName;
    }
}
