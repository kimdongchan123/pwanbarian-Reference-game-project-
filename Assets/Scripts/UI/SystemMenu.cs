using UnityEngine;
using UnityEngine.SceneManagement;

public class SystemMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private GameObject systemMenu;
    [SerializeField] private GameObject settingsMenu;

    public void ToggleSystemMenu()
    {
        systemMenu.SetActive(!systemMenu.activeSelf);
        Time.timeScale = systemMenu.activeSelf ? 0f : 1f; // Pause or resume the game
    }

    public void ResumeGame()
    {
        systemMenu.SetActive(false);
        Time.timeScale = 1f; // Resume the game
    }

    public void OpenSettings()
    {
        settingsMenu.SetActive(true);
    }

    public void QuitToMainMenu()
    {
        // Ensure pause state does not carry into the next scene.
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("Main menu scene name is empty.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
