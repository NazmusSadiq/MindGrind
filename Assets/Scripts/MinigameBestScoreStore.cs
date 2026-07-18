using TMPro;
using UnityEngine;
using System.IO;

public class MinigameBestScoreStore : MonoBehaviour
{
    private const string BestScoreKeyPrefix = "BestScore_";
    private const string TotalScoreKeyPrefix = "TotalScore_";
    private const string PlayCountKeyPrefix = "PlayCount_";

    // PlayerPrefs Keys for Cognitive Averages
    public const string PrefAttention = "Cognitive_Attention";
    public const string PrefMemory = "Cognitive_Memory";
    public const string PrefReasoning = "Cognitive_Reasoning";
    public const string PrefReflex = "Cognitive_Reflex";
    public const string PrefPerception = "Cognitive_Perception";
    public const string PrefLearning = "Cognitive_Learning";

    [Header("ID Configuration")]
    [SerializeField] private int explicitGameId = -1;

    [Header("References")]
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private TMP_Text gameTitleText;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text averageScoreText;

    public static int GetBestScore(string minigameId)
    {
        return PlayerPrefs.GetInt(GetKey(minigameId), 0);
    }

    public static int GetAverageScore(string minigameId)
    {
        int totalScore = PlayerPrefs.GetInt($"{TotalScoreKeyPrefix}{minigameId}", 0);
        int playCount = PlayerPrefs.GetInt($"{PlayCountKeyPrefix}{minigameId}", 0);

        if (playCount == 0) return 0;
        return Mathf.RoundToInt((float)totalScore / playCount);
    }

    public static int UpdateBestScore(string minigameIdOrSceneName, int score)
    {
        string finalizedIdString = minigameIdOrSceneName;
        int resolvedNumericId = 0;

        if (!int.TryParse(minigameIdOrSceneName, out resolvedNumericId))
        {
            if (MinigameDataStore.TryGetGameDataBySceneName(minigameIdOrSceneName, out MinigameDataStore.GameData foundGame))
            {
                resolvedNumericId = foundGame.id;
                finalizedIdString = foundGame.id.ToString();
            }
            else
            {
                resolvedNumericId = 0;
                finalizedIdString = minigameIdOrSceneName;
            }
        }

        string totalScoreKey = $"{TotalScoreKeyPrefix}{finalizedIdString}";
        string playCountKey = $"{PlayCountKeyPrefix}{finalizedIdString}";

        int currentTotalScore = PlayerPrefs.GetInt(totalScoreKey, 0) + score;
        int currentPlayCount = PlayerPrefs.GetInt(playCountKey, 0) + 1;

        PlayerPrefs.SetInt(totalScoreKey, currentTotalScore);
        PlayerPrefs.SetInt(playCountKey, currentPlayCount);

        string key = GetKey(finalizedIdString);
        bool hasExistingScore = PlayerPrefs.HasKey(key);
        int bestScore = PlayerPrefs.GetInt(key, 0);
        bool shouldUpdate = false;

        bool isTimeBased = resolvedNumericId >= 30;

        if (!isTimeBased)
        {
            if (!hasExistingScore || score > bestScore)
            {
                shouldUpdate = true;
            }
        }
        else
        {
            if (!hasExistingScore || score < bestScore)
            {
                shouldUpdate = true;
            }
        }

        if (shouldUpdate)
        {
            bestScore = score;
            PlayerPrefs.SetInt(key, bestScore);
        }

        // Calculate cognitive averages and write to PlayerPrefs
        CalculateAndSaveCognitiveAverages();

        PlayerPrefs.Save();

        // Sync local analytics to Profile/JSON models
        SyncToPlayerProfileFile();

        return bestScore;
    }

    /// <summary>
    /// Evaluates played games and updates running average attributes based on criteria configurations.
    /// </summary>
    public static void CalculateAndSaveCognitiveAverages()
    {
        float sumAttention = 0f, sumMemory = 0f, sumReasoning = 0f, sumReflex = 0f, sumPerception = 0f, sumLearning = 0f;
        int countAttention = 0, countMemory = 0, countReasoning = 0, countReflex = 0, countPerception = 0, countLearning = 0;

        for (int id = 0; id < 35; id++)
        {
            string idStr = id.ToString();
            int playCount = PlayerPrefs.GetInt($"{PlayCountKeyPrefix}{idStr}", 0);

            // Only process games with active play records
            if (playCount == 0) continue;

            if (MinigameDataStore.TryGetGameData(id, out MinigameDataStore.GameData gameData))
            {
                float avgScore = GetAverageScore(idStr);
                float normalizedScore = 0f;

                if (id >= 30) // Time-based game (id >= 30)
                {
                    float minTimeTarget = GetMinimumTimeTargetForGame(id);
                    if (avgScore > 0f)
                    {
                        // Calculate how many seconds past the minimum time target the player took
                        float secondsLate = Mathf.Max(0f, avgScore - minTimeTarget);

                        // True exponential decay with lambda = 0.005
                        normalizedScore = Mathf.Exp(-0.005f * secondsLate);
                    }
                }
                else // Score-based game (id < 30)
                {
                    float maxScoreLimit = GetMaxScoreLimitForGame(id);
                    normalizedScore = Mathf.Clamp01(avgScore / maxScoreLimit);
                }

                string skills = gameData.cognitiveSkills.ToLower();

                if (skills.Contains("attention"))
                {
                    sumAttention += normalizedScore;
                    countAttention++;
                }
                if (skills.Contains("memory"))
                {
                    sumMemory += normalizedScore;
                    countMemory++;
                }
                if (skills.Contains("reasoning"))
                {
                    sumReasoning += normalizedScore;
                    countReasoning++;
                }
                if (skills.Contains("reflex"))
                {
                    sumReflex += normalizedScore;
                    countReflex++;
                }
                if (skills.Contains("perception"))
                {
                    sumPerception += normalizedScore;
                    countPerception++;
                }
                if (skills.Contains("learning"))
                {
                    sumLearning += normalizedScore;
                    countLearning++;
                }
            }
        }

        // Apply averages, returning 0 if no games have been registered in a specific category
        float avgAttention = countAttention > 0 ? sumAttention / countAttention : 0f;
        float avgMemory = countMemory > 0 ? sumMemory / countMemory : 0f;
        float avgReasoning = countReasoning > 0 ? sumReasoning / countReasoning : 0f;
        float avgReflex = countReflex > 0 ? sumReflex / countReflex : 0f;
        float avgPerception = countPerception > 0 ? sumPerception / countPerception : 0f;
        float avgLearning = countLearning > 0 ? sumLearning / countLearning : 0f;

        PlayerPrefs.SetFloat(PrefAttention, avgAttention);
        PlayerPrefs.SetFloat(PrefMemory, avgMemory);
        PlayerPrefs.SetFloat(PrefReasoning, avgReasoning);
        PlayerPrefs.SetFloat(PrefReflex, avgReflex);
        PlayerPrefs.SetFloat(PrefPerception, avgPerception);
        PlayerPrefs.SetFloat(PrefLearning, avgLearning);
    }

    /// <summary>
    /// Configurable target limit considered a '1.0' for Point-Based games (ID < 30).
    /// </summary>
    private static float GetMaxScoreLimitForGame(int gameId)
    {
        switch (gameId)
        {
            case 0: return 250f;
            case 1: return 300f;
            case 2: return 200f;
            case 3: return 90f;
            case 4: return 200f;
            case 5: return 200f;
            case 6: return 70f;
            case 7: return 275f;
            case 8: return 170f;
            case 9: return 225f;
            case 10: return 100f;
            case 11: return 300f;
            case 12: return 200f;
            case 13: return 170f;
            case 14: return 200f;
            case 15: return 80f;
            case 16: return 400f;
            case 17: return 125f;
            case 18: return 800f;
            case 19: return 175f;
            default: return 100f; // Default 100 points
        }
    }

    /// <summary>
    /// Configurable minimum target time considered a '1.0' for Time-Based games (ID >= 30).
    /// </summary>
    private static float GetMinimumTimeTargetForGame(int gameId)
    {
        switch (gameId)
        {
            case 30: return 95f;
            case 31: return 100f;
            case 32: return 85f;
            case 33: return 120f;
            case 34: return 190f;
            default: return 20f; // Default 20 seconds minimum time target
        }
    }

    private static void SyncToPlayerProfileFile()
    {
        string saveFilePath = Path.Combine(Application.persistentDataPath, "player_analytics.json");
        PersonalInfoChecker infoChecker = Object.FindFirstObjectByType<PersonalInfoChecker>();
        if (infoChecker != null)
        {
            infoChecker.SyncAllExistingScores();
            infoChecker.SaveProfileToDisk();
        }
        else if (File.Exists(saveFilePath))
        {
            try
            {
                string rawJson = File.ReadAllText(saveFilePath);
                PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(rawJson);

                if (profile != null)
                {
                    if (profile.gameStats == null)
                    {
                        profile.gameStats = new System.Collections.Generic.List<GameStat>();
                    }

                    profile.gameStats.Clear();

                    for (int id = 0; id < 35; id++)
                    {
                        string idStr = id.ToString();
                        string checkKey = $"{PlayCountKeyPrefix}{idStr}";

                        if (PlayerPrefs.HasKey(checkKey))
                        {
                            GameStat stat = new GameStat
                            {
                                gameId = id,
                                bestScore = PlayerPrefs.GetInt($"{BestScoreKeyPrefix}{idStr}", 0),
                                playCount = PlayerPrefs.GetInt(checkKey, 0),
                                averageScore = GetAverageScore(idStr)
                            };
                            profile.gameStats.Add(stat);
                        }
                    }

                    string updatedJson = JsonUtility.ToJson(profile, true);
                    File.WriteAllText(saveFilePath, updatedJson);
                    SimpleDiskSyncManager.PushLocalJsonToServer();
                }
            }
            catch (System.Exception e)
            {
                //Debug.LogError($"Error syncing metrics to player_analytics layout: {e.Message}");
            }
        }
    }

    private static string GetKey(string minigameId)
    {
        return $"{BestScoreKeyPrefix}{minigameId}";
    }

    public void SetupGameOverMenu(int currentScore)
    {
        ShowGameOverMenu();

        MinigameDataStore.GameData currentGame;

        // Core adjustment logic injection to prioritize explicit manual inspector settings
        if (explicitGameId >= 0 && MinigameDataStore.TryGetGameData(explicitGameId, out MinigameDataStore.GameData explicitGame))
        {
            currentGame = explicitGame;
        }
        else
        {
            currentGame = MinigameDataStore.GetCurrentGame();

            if (string.IsNullOrEmpty(currentGame.sceneName))
            {
                string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                MinigameDataStore.TryGetGameDataBySceneName(activeSceneName, out currentGame);
            }
        }

        if (gameTitleText != null && !string.IsNullOrEmpty(currentGame.gameTitle))
        {
            gameTitleText.text = currentGame.gameTitle;
        }

        bool isTimeBased = currentGame.id >= 30;

        if (currentScoreText != null)
        {
            currentScoreText.text = isTimeBased ? $"Current Time: {currentScore}s" : $"Current Score: {currentScore}";
        }

        string resolvedLookupKey = currentGame.id != 0 || !string.IsNullOrEmpty(currentGame.gameTitle) ?
            currentGame.id.ToString() : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (bestScoreText != null)
        {
            int liveBest = GetBestScore(resolvedLookupKey);
            bestScoreText.text = isTimeBased ? $"Best Time: {liveBest}s" : $"Best Score: {liveBest}";
        }

        if (averageScoreText != null)
        {
            int liveAvg = GetAverageScore(resolvedLookupKey);
            averageScoreText.text = isTimeBased ? $"Avg Time: {liveAvg}s" : $"Avg Score: {liveAvg}";
        }
    }

    public void DisplayUpdatedUI(int currentScoreValue, int idValue)
    {
        int currentBestValue = GetBestScore(idValue.ToString());
        int currentAvgValue = GetAverageScore(idValue.ToString());
        bool isTimeBased = idValue >= 30;

        if (currentScoreText != null)
        {
            currentScoreText.text = isTimeBased ? $"Current Time: {currentScoreValue}s" : $"Current Score: {currentScoreValue}";
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = isTimeBased ? $"Best Time: {currentBestValue}s" : $"Best Score: {currentBestValue}";
        }

        if (averageScoreText != null)
        {
            averageScoreText.text = isTimeBased ? $"Avg Time: {currentAvgValue}s" : $"Avg Score: {currentAvgValue}";
        }

        if (gameTitleText != null)
        {
            MinigameDataStore.GameData currentGame;

            if (explicitGameId >= 0 && MinigameDataStore.TryGetGameData(explicitGameId, out MinigameDataStore.GameData explicitGame))
            {
                currentGame = explicitGame;
            }
            else
            {
                currentGame = MinigameDataStore.GetCurrentGame();
            }

            if (!string.IsNullOrEmpty(currentGame.gameTitle))
            {
                gameTitleText.text = currentGame.gameTitle;
            }
        }
    }

    public void ShowStats(int currentScore, int bestScore)
    {
        ShowGameOverMenu();

        Time.timeScale = 0f;

        MinigameDataStore.GameData currentGame;

        // Core adjustment logic injection to prioritize explicit manual inspector settings
        if (explicitGameId >= 0 && MinigameDataStore.TryGetGameData(explicitGameId, out MinigameDataStore.GameData explicitGame))
        {
            currentGame = explicitGame;
        }
        else
        {
            currentGame = MinigameDataStore.GetCurrentGame();

            if (string.IsNullOrEmpty(currentGame.sceneName))
            {
                string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                MinigameDataStore.TryGetGameDataBySceneName(activeSceneName, out currentGame);
            }
        }

        if (gameTitleText != null && !string.IsNullOrEmpty(currentGame.gameTitle))
        {
            gameTitleText.text = currentGame.gameTitle;
        }

        bool isTimeBased = currentGame.id >= 30;

        if (currentScoreText != null)
        {
            currentScoreText.text = isTimeBased ? $"Current Time: {currentScore}s" : $"Current Score: {currentScore}";
        }

        string resolvedLookupKey = currentGame.id != 0 || !string.IsNullOrEmpty(currentGame.gameTitle) ?
            currentGame.id.ToString() : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (bestScoreText != null)
        {
            int liveBest = GetBestScore(resolvedLookupKey);
            bestScoreText.text = isTimeBased ? $"Best Time: {liveBest}s" : $"Best Score: {liveBest}";
        }

        if (averageScoreText != null)
        {
            int liveAvg = GetAverageScore(resolvedLookupKey);
            averageScoreText.text = isTimeBased ? $"Avg Time: {liveAvg}s" : $"Avg Score: {liveAvg}";
        }
    }

    private void ShowGameOverMenu()
    {
        if (gameOverMenu == null)
            return;

        gameOverMenu.SetActive(true);
        gameOverMenu.transform.SetAsLastSibling();

        CanvasGroup canvasGroup = gameOverMenu.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}