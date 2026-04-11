using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private bool settingsOpen = false;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject volumeSettingsMenu;
    [SerializeField] private GameObject displaySettingsMenu;
    [SerializeField] private Button volumeSettingsButton;
    [SerializeField] private Button displaySettingsButton;

    public void OpenSettings()
    {
        settingsOpen = !settingsOpen;
        settingsMenu.SetActive(settingsOpen);
    }

    public void CloseSettings()
    {
        settingsOpen = false;
        settingsMenu.SetActive(false);
    }

    public void GotoVolumeSettings()
    {
        volumeSettingsMenu.SetActive(true);
        displaySettingsMenu.SetActive(false);
        volumeSettingsButton.interactable = false;
        displaySettingsButton.interactable = true;
    }

    public void GotoDisplaySettings()
    {
        volumeSettingsMenu.SetActive(false);
        displaySettingsMenu.SetActive(true);
        volumeSettingsButton.interactable = true;
        displaySettingsButton.interactable = false;
    }
}
