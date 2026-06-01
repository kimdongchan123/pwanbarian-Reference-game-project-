using UnityEngine;

public class StageSelectManager : MonoBehaviour
{
    [SerializeField]
    private GameObject stageSelectPanel;
    [SerializeField]
    private GameObject stageSelectButtonPrefab;
    [SerializeField]
    private StageData[] stageDataArray;

    void Awake()
    {
        foreach (var stageData in stageDataArray)
        {
            var button = Instantiate(stageSelectButtonPrefab, stageSelectPanel.transform);
            var buttonComponent = button.GetComponent<StageSelectButton>();
            buttonComponent.SetStageData(stageData);
        }
    }
}
