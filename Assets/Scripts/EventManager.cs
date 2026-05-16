using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string MiniGamesButtonName = "MiniGames_Button";

    private static bool isStoryMode;

    public void QuitGame()
    {
        Debug.Log("Quit!");

        Application.Quit();
    }

    public void SetStoryMode(bool value)
    {
        isStoryMode = value;
    }

    public void LoadMiniGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            MinigameDataStore.Instance.GetCurrentGame().sceneName
        );
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void processNext()
    {
        Time.timeScale = 1f;

        if (isStoryMode)
        {
            Debug.Log("Process next from story mode.");
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MainMenuSceneName)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameObject miniGamesButtonObject = GameObject.Find(MiniGamesButtonName);
        if (miniGamesButtonObject == null)
        {
            Debug.LogWarning($"Could not find '{MiniGamesButtonName}' in scene '{MainMenuSceneName}'.");
            return;
        }

        Button miniGamesButton = miniGamesButtonObject.GetComponent<Button>();
        if (miniGamesButton == null)
        {
            Debug.LogWarning($"'{MiniGamesButtonName}' does not have a Button component.");
            return;
        }

        miniGamesButton.onClick.Invoke();
    }
}