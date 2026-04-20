using UnityEngine;

public class SelectionSceneSetup : MonoBehaviour
{
    public CharacterInfoUI infoUI;

    private void Start()
    {
        if (infoUI != null)
        {
            infoUI.ClearUI();
        }
    }
}