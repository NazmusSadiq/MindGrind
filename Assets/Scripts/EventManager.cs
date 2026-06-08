using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string StartSceneName = "Level1";
    private const string MiniGamesButtonName = "MiniGames_Button";
    private const string StoryModeTitleObjectName = "Title";
    private const string StoryModeDescriptionObjectName = "Description";
    private const string StoryModeAttributesObjectName = "Attributes";
    private const string StoryModeBestScoreObjectName = "BestScore";

    [SerializeField] private string storyModeNextSceneName = "Level1";
    [SerializeField] private GameObject storyModeDetailsPanel;

    private TMP_Text storyModeTitleText;
    private TMP_Text storyModeDescriptionText;
    private TMP_Text storyModeAttributesText;
    private TMP_Text storyModeHighestScoreText;

    private static bool isStoryMode;

    private void Awake()
    {
        CacheStoryModeDetailsReferences();
    }

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

        MinigameDataStore.GameData currentGame = MinigameDataStore.GetCurrentGame();
        if (string.IsNullOrWhiteSpace(currentGame.sceneName))
        {
            Debug.LogWarning("Current minigame scene name is not set.", this);
            return;
        }

        SceneManager.LoadScene(
            currentGame.sceneName
        );
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(StartSceneName);
    }

    public void processNext()
    {
        if (isStoryMode)
        {
            if (string.IsNullOrWhiteSpace(storyModeNextSceneName))
            {
                Debug.LogWarning("Story mode next scene name is not set.", this);
                return;
            }

            SceneManager.LoadScene(storyModeNextSceneName);
            PauseGameAndShowDetailsPanel();
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadMainMenu();
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuSceneName);
        Time.timeScale = 1f;
    }


    public static void PauseGameAndShowDetailsPanel()
    {
        // REMOVE OR COMMENT OUT THIS LINE:
        // if (!isStoryMode) { return; } 

        SceneManager.sceneLoaded -= OnStoryModeSceneLoaded;
        SceneManager.sceneLoaded += OnStoryModeSceneLoaded;

        MainMenu mainMenuInstance = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenuInstance != null)
        {
            mainMenuInstance.ShowDetailsPanelAndPauseGame();
        }
    }

    public void HideDetailsPanelAndResumeGame()
    {
        if (storyModeDetailsPanel == null)
        {
            Debug.LogWarning("Story mode details panel is not assigned.", this);
            Time.timeScale = 1f;
            return;
        }

        storyModeDetailsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void ShowDetailsPanelAndPauseGame()
    {
        if (storyModeDetailsPanel == null)
        {
            Debug.LogWarning("Story mode details panel is not assigned.", this);
            Time.timeScale = 1f;
            return;
        }

        // 1. Force identify the current level layout
        int correctLevelId = GetCurrentLevelId();

        // 2. Explicitly update the active description profile using that ID
        UpdateDescription(correctLevelId);

        storyModeDetailsPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void UpdateDescription(int levelId)
    {
        MinigameDataStore.GameData gameData;

        if (!MinigameDataStore.TryGetGameData(levelId, out gameData)
            && !MinigameDataStore.TryGetGameDataBySceneName(SceneManager.GetActiveScene().name, out gameData))
        {
            Debug.LogWarning($"Could not find minigame details.", this);
            return;
        }

        MinigameDataStore.SetCurrentGame(gameData);
        CacheStoryModeDetailsReferences();

        if (storyModeTitleText != null) storyModeTitleText.text = gameData.gameTitle;
        if (storyModeDescriptionText != null) storyModeDescriptionText.text = gameData.description;
        if (storyModeAttributesText != null) storyModeAttributesText.text = gameData.cognitiveSkills;

        // Ensure this UI text component reads from the correct dynamic PlayerPrefs key string
        if (storyModeHighestScoreText != null)
        {
            int highestScore = MinigameBestScoreStore.GetBestScore(gameData.id.ToString());

            if (highestScore == 0)
            {
                storyModeHighestScoreText.text = "Highest Score: None";
            }
            else
            {
                // Format display text dynamically depending on whether it tracks score or time
                storyModeHighestScoreText.text = gameData.id < 30 ?
                    $"Highest Score: {highestScore}" :
                    $"Best Time: {highestScore}s";
            }
        }
    }

    private int GetCurrentLevelId()
    {
        MinigameDataStore.GameData currentGame = MinigameDataStore.GetCurrentGame();
        string activeSceneName = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrWhiteSpace(currentGame.sceneName) && currentGame.sceneName == activeSceneName)
        {
            return currentGame.id;
        }

        if (MinigameDataStore.TryGetGameDataBySceneName(activeSceneName, out MinigameDataStore.GameData sceneGame))
        {
            return sceneGame.id;
        }

        return currentGame.id;
    }

    private void CacheStoryModeDetailsReferences()
    {
        if (storyModeDetailsPanel == null)
        {
            return;
        }

        storyModeTitleText ??= GetStoryModeText(StoryModeTitleObjectName);
        storyModeDescriptionText ??= GetStoryModeText(StoryModeDescriptionObjectName);
        storyModeAttributesText ??= GetStoryModeText(StoryModeAttributesObjectName);
        storyModeHighestScoreText ??= GetStoryModeText(StoryModeBestScoreObjectName);
    }

    private TMP_Text GetStoryModeText(string childObjectName)
    {
        Transform child = storyModeDetailsPanel.transform.Find(childObjectName);
        if (child == null)
        {
            Debug.LogWarning($"Could not find '{childObjectName}' under '{storyModeDetailsPanel.name}'.", this);
            return null;
        }

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = child.GetComponentInChildren<TMP_Text>(true);
        }

        if (text == null)
        {
            Debug.LogWarning($"'{childObjectName}' does not have a TMP_Text component.", this);
        }

        return text;
    }

    private static void OnStoryModeSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnStoryModeSceneLoaded;

        MainMenu mainMenu = FindObjectOfType<MainMenu>();
        if (mainMenu == null)
        {
            Debug.LogWarning($"Could not find a {nameof(MainMenu)} in scene '{scene.name}'.");
            Time.timeScale = 1f;
            return;
        }

        mainMenu.ShowDetailsPanelAndPauseGame();
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