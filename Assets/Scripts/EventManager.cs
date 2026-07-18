using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string StartSceneName = "Level1";
    private const string MiniGamesButtonName = "MiniGames_Button";
    private const string StoryModeTitleObjectName = "Title";
    private const string StoryModeDescriptionObjectName = "Description";
    private const string StoryModeAttributesObjectName = "Attributes";
    private const string StoryModeBestScoreObjectName = "BestScore";
    private const string StoryModeAverageScoreObjectName = "AverageScore";

    [Header("Menu Stats Layout")]
    [SerializeField] private TMP_Text overallText;
    [SerializeField] private TMP_Text attentionText;
    [SerializeField] private TMP_Text memoryText;
    [SerializeField] private TMP_Text reflexText;
    [SerializeField] private TMP_Text perceptionText;
    [SerializeField] private TMP_Text learningText;
    [SerializeField] private TMP_Text reasoningText;

    [Header("Game Progression")]
    [SerializeField] private string storyModeNextSceneName = "Level1";
    [SerializeField] private GameObject storyModeDetailsPanel;
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Thumbnail Target Settings")]
    [Tooltip("Drag the UI Image component here that you want to be updated by the input image/sprite.")]
    [SerializeField] private Image targetThumbnailField;

    private TMP_Text storyModeTitleText;
    private TMP_Text storyModeDescriptionText;
    private TMP_Text storyModeAttributesText;
    private TMP_Text storyModeHighestScoreText;
    private TMP_Text storyModeAverageScoreText;

    private static bool isStoryMode;
    private bool isGamePaused;

    private void Awake()
    {
        CacheStoryModeDetailsReferences();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == MainMenuSceneName) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else if (Time.timeScale > 0f)
            {
                PauseGame();
            }
        }
    }

    public void QuitGame()
    {
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
            return;
        }

        SceneManager.LoadScene(currentGame.sceneName);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;

        if (MinigameDataStore.TryGetGameData(30, out MinigameDataStore.GameData storyStartGame))
        {
            MinigameDataStore.SetCurrentGame(storyStartGame);
        }

        SceneManager.LoadScene(StartSceneName);
    }

    public void processNext()
    {
        if (isStoryMode)
        {
            if (string.IsNullOrWhiteSpace(storyModeNextSceneName))
            {
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
        if (storyModeDetailsPanel == null) return;

        storyModeDetailsPanel.SetActive(false);
        Time.timeScale = 1f;

        StartCoroutine(EnableInputAfterDelay());
    }

    private IEnumerator EnableInputAfterDelay()
    {
        yield return new WaitForSeconds(0.25f);

        Level2_Manager level2 = Object.FindFirstObjectByType<Level2_Manager>();
        if (level2 != null)
        {
            level2.StartCinematicReveal();
            //yield break;
        }

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetGameStarted(true);
            player.EnableGameplayInput(true);
        }
    }

    public void ShowDetailsPanelAndPauseGame()
    {
        if (storyModeDetailsPanel != null)
        {
            storyModeDetailsPanel.SetActive(true);
        }

        isGamePaused = true;
        Time.timeScale = 0f;

        // Unlock and display the cursor when showing the story mode details assignment UI overlay
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string activeSceneName = SceneManager.GetActiveScene().name;
        string targetLookupName = (activeSceneName == "MainMenu") ? storyModeNextSceneName : activeSceneName;

        string displayTitle = targetLookupName;
        string displayDescription = "";
        string displaySkills = "";

        string resolvedPrefsKey = targetLookupName;
        bool isTimeBased = false;

        if (MinigameDataStore.TryGetGameDataBySceneName(targetLookupName, out MinigameDataStore.GameData gameData))
        {
            displayTitle = gameData.gameTitle;
            displayDescription = gameData.description;
            displaySkills = gameData.cognitiveSkills;

            resolvedPrefsKey = gameData.id.ToString();
            isTimeBased = gameData.id >= 30;
        }

        if (storyModeTitleText != null) storyModeTitleText.text = displayTitle;
        if (storyModeDescriptionText != null) storyModeDescriptionText.text = displayDescription;
        if (storyModeAttributesText != null) storyModeAttributesText.text = displaySkills;

        int playCount = PlayerPrefs.GetInt($"PlayCount_{resolvedPrefsKey}", 0);

        if (storyModeHighestScoreText != null)
        {
            if (playCount == 0)
            {
                storyModeHighestScoreText.text = isTimeBased ? "Best Time: None" : "Best Score: None";
            }
            else
            {
                int bestScore = MinigameBestScoreStore.GetBestScore(resolvedPrefsKey);
                storyModeHighestScoreText.text = isTimeBased ? $"Best Time: {bestScore}s" : $"Best Score: {bestScore}";
            }
        }

        if (storyModeAverageScoreText != null)
        {
            if (playCount == 0)
            {
                storyModeAverageScoreText.text = isTimeBased ? "Average Time: None" : "Average Score: None";
            }
            else
            {
                int averageScore = MinigameBestScoreStore.GetAverageScore(resolvedPrefsKey);
                storyModeAverageScoreText.text = isTimeBased ? $"Average Time: {averageScore}s" : $"Average Score: {averageScore}";
            }
        }
    }

    public void PauseGame()
    {
        if (pauseMenuPanel == null) return;

        isGamePaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetGameStarted(false);
            player.EnableGameplayInput(false);
        }
    }

    public void UpdateStatsForMenu()
    {
        float attention = PlayerPrefs.GetFloat(MinigameBestScoreStore.PrefAttention, 0f);
        float memory = PlayerPrefs.GetFloat(MinigameBestScoreStore.PrefMemory, 0f);
        float reasoning = PlayerPrefs.GetFloat(MinigameBestScoreStore.PrefReasoning, 0f);
        float reflex = PlayerPrefs.GetFloat(MinigameBestScoreStore.PrefReflex, 0f);
        float perception = PlayerPrefs.GetFloat(MinigameBestScoreStore.PrefPerception, 0f);
        float learning = PlayerPrefs.GetFloat(MinigameBestScoreStore.PrefLearning, 0f);

        float sumOfPlayedScores = 0f;
        int activeCategoriesCount = 0;

        if (attention > 0f) { sumOfPlayedScores += attention; activeCategoriesCount++; }
        if (memory > 0f) { sumOfPlayedScores += memory; activeCategoriesCount++; }
        if (reasoning > 0f) { sumOfPlayedScores += reasoning; activeCategoriesCount++; }
        if (reflex > 0f) { sumOfPlayedScores += reflex; activeCategoriesCount++; }
        if (perception > 0f) { sumOfPlayedScores += perception; activeCategoriesCount++; }
        if (learning > 0f) { sumOfPlayedScores += learning; activeCategoriesCount++; }

        float overall = activeCategoriesCount > 0 ? (sumOfPlayedScores / activeCategoriesCount) : 0f;

        if (overallText != null) overallText.text = overall.ToString("F2");
        if (attentionText != null) attentionText.text = attention.ToString("F2");
        if (memoryText != null) memoryText.text = memory.ToString("F2");
        if (reflexText != null) reflexText.text = reflex.ToString("F2");
        if (perceptionText != null) perceptionText.text = perception.ToString("F2");
        if (learningText != null) learningText.text = learning.ToString("F2");
        if (reasoningText != null) reasoningText.text = reasoning.ToString("F2");
    }

    public void ResumeGame()
    {
        if (pauseMenuPanel == null) return;

        isGamePaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            player.SetGameStarted(true);
            player.EnableGameplayInput(true);
        }
    }

    public void UpdateDescription(int levelId)
    {
        MinigameDataStore.GameData gameData;

        if (!MinigameDataStore.TryGetGameData(levelId, out gameData)
            && !MinigameDataStore.TryGetGameDataBySceneName(SceneManager.GetActiveScene().name, out gameData))
        {
            return;
        }

        MinigameDataStore.SetCurrentGame(gameData);
        CacheStoryModeDetailsReferences();

        if (storyModeTitleText != null) storyModeTitleText.text = gameData.gameTitle;
        if (storyModeDescriptionText != null) storyModeDescriptionText.text = gameData.description;
        if (storyModeAttributesText != null) storyModeAttributesText.text = gameData.cognitiveSkills;

        string stringId = gameData.id.ToString();
        if (gameData.id >= 30 && !string.IsNullOrEmpty(gameData.sceneName))
        {
            stringId = gameData.sceneName;
        }

        if (storyModeHighestScoreText != null)
        {
            int highestScore = MinigameBestScoreStore.GetBestScore(stringId);

            if (highestScore == 0 && !PlayerPrefs.HasKey($"BestScore_{stringId}"))
            {
                storyModeHighestScoreText.text = gameData.id < 30 ? "Highest Score: None" : "Best Time: None";
            }
            else
            {
                storyModeHighestScoreText.text = gameData.id < 30 ?
                    $"Highest Score: {highestScore}" :
                    $"Best Time: {highestScore}s";
            }
        }

        if (storyModeAverageScoreText != null)
        {
            int averageScore = MinigameBestScoreStore.GetAverageScore(stringId);
            int playCount = PlayerPrefs.GetInt($"PlayCount_{stringId}", 0);

            if (playCount == 0)
            {
                storyModeAverageScoreText.text = gameData.id < 30 ? "Average Score: None" : "Average Time: None";
            }
            else
            {
                storyModeAverageScoreText.text = gameData.id < 30 ?
                    $"Average Score: {averageScore}" :
                    $"Average Time: {averageScore}s";
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
        if (storyModeDetailsPanel == null) return;

        storyModeTitleText ??= GetStoryModeText(StoryModeTitleObjectName);
        storyModeDescriptionText ??= GetStoryModeText(StoryModeDescriptionObjectName);
        storyModeAttributesText ??= GetStoryModeText(StoryModeAttributesObjectName);
        storyModeHighestScoreText ??= GetStoryModeText(StoryModeBestScoreObjectName);
        storyModeAverageScoreText ??= GetStoryModeText(StoryModeAverageScoreObjectName);
    }

    private TMP_Text GetStoryModeText(string childObjectName)
    {
        Transform child = storyModeDetailsPanel.transform.Find(childObjectName);
        if (child == null)
        {
            return null;
        }

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = child.GetComponentInChildren<TMP_Text>(true);
        }

        return text;
    }

    private static void OnStoryModeSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnStoryModeSceneLoaded;

        MainMenu mainMenu = FindObjectOfType<MainMenu>();
        if (mainMenu == null)
        {
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
            return;
        }

        Button miniGamesButton = miniGamesButtonObject.GetComponent<Button>();
        if (miniGamesButton == null)
        {
            return;
        }

        miniGamesButton.onClick.Invoke();
    }

    public void UpdateThumbnail(Sprite newSprite)
    {
        if (targetThumbnailField == null)
        {
            return;
        }

        if (newSprite == null)
        {
            return;
        }

        targetThumbnailField.sprite = newSprite;
    }

    public void UpdateThumbnailFromImage(Image sourceImage)
    {
        if (targetThumbnailField == null)
        {
            return;
        }

        if (sourceImage == null)
        {
            return;
        }

        targetThumbnailField.sprite = sourceImage.sprite;
    }
}