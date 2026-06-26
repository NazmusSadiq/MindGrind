using TMPro;
using UnityEngine;
using System.IO;

public class MinigameBestScoreStore : MonoBehaviour
{
    private const string BestScoreKeyPrefix = "BestScore_";
    private const string TotalScoreKeyPrefix = "TotalScore_";
    private const string PlayCountKeyPrefix = "PlayCount_";

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

        if (resolvedNumericId < 30)
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

        PlayerPrefs.Save();

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

                    // Rebuild stats list directly from PlayerPrefs using your explicit PlayerDataModel structure
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
                    Debug.Log("[JSON Update Success] Successfully synced data model statistics to file.");

                    SimpleDiskSyncManager.PushLocalJsonToServer();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error syncing metrics to file layout path: {e.Message}");
            }
        }
        // --------------------------------------------------

        return bestScore;
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
            MinigameDataStore.GameData currentGame = MinigameDataStore.GetCurrentGame();
            if (!string.IsNullOrEmpty(currentGame.gameTitle))
            {
                gameTitleText.text = currentGame.gameTitle;
            }
        }
    }

    public void ShowStats(int currentScore, int bestScore)
    {
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
        }

        Time.timeScale = 0f;

        MinigameDataStore.GameData currentGame = MinigameDataStore.GetCurrentGame();

        if (string.IsNullOrEmpty(currentGame.sceneName))
        {
            string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            MinigameDataStore.TryGetGameDataBySceneName(activeSceneName, out currentGame);
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

    private static string GetKey(string minigameId)
    {
        return $"{BestScoreKeyPrefix}{minigameId}";
    }
}