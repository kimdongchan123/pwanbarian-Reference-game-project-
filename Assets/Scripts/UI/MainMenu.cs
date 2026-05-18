using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private bool existsGame = false;
    [SerializeField] private RectTransform startGameButton;
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private RectTransform quitButton;

    [SerializeField] private string gameSceneName = "StageSelectScene";

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
