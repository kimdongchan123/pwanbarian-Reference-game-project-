using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private bool existsGame = false;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private RectTransform newGameButton;
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private RectTransform quitButton;

    [SerializeField] private string gameSceneName = "SettingPlaceScene";

    
    void Awake()
    {
        if (existsGame)
        {
            continueButton.SetActive(true);
            newGameButton.anchoredPosition = new Vector3(0, -140, 0);
            settingsButton.anchoredPosition = new Vector3(0, -270, 0);
            quitButton.anchoredPosition = new Vector3(0, -400, 0);
        }
        else
        {
            continueButton.SetActive(false);
            newGameButton.anchoredPosition = new Vector3(0, -10, 0);
            settingsButton.anchoredPosition = new Vector3(0, -140, 0);
            quitButton.anchoredPosition = new Vector3(0, -270, 0);
        }
    }

    public void StartNewGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
