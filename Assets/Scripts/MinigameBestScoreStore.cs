using TMPro;
using UnityEngine;

public class MinigameBestScoreStore : MonoBehaviour
{
    private const string BestScoreKeyPrefix = "BestScore_";

    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private TMP_Text gameTitleText;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private string minigameId;

    public static int GetBestScore(string minigameId)
    {
        return PlayerPrefs.GetInt(GetKey(minigameId), 0);
    }

    public static int UpdateBestScore(string minigameIdOrSceneName, int score)
    {
        string finalizedIdString = minigameIdOrSceneName;
        int resolvedNumericId = 0;

        // 1. Check if the string passed is a scene name or a numerical ID string
        if (!int.TryParse(minigameIdOrSceneName, out resolvedNumericId))
        {
            // It's a text scene name (e.g., "Minigame1"). Let's find its true registered ID
            if (MinigameDataStore.TryGetGameDataBySceneName(minigameIdOrSceneName, out MinigameDataStore.GameData foundGame))
            {
                resolvedNumericId = foundGame.id;
                finalizedIdString = foundGame.id.ToString();
            }
            else
            {
                // Fallback: If not found in database registry, deduce type from scene name layout
                // E.g., If name has "Level" or number >= 30, treat as time; otherwise points
                resolvedNumericId = 0;
                finalizedIdString = minigameIdOrSceneName; // Keep scene name string as the PlayerPrefs key
            }
        }

        string key = GetKey(finalizedIdString);
        bool hasExistingScore = PlayerPrefs.HasKey(key);
        int bestScore = PlayerPrefs.GetInt(key, 0);
        bool shouldUpdate = false;

        // 2. Route score rules safely depending on the discovered numeric ID configuration map
        if (resolvedNumericId < 30)
        {
            // HIGHER score is better (Points based)
            if (!hasExistingScore || score > bestScore)
            {
                shouldUpdate = true;
            }
        }
        else
        {
            // LOWER score is better (Time based)
            if (!hasExistingScore || score < bestScore)
            {
                shouldUpdate = true;
            }
        }

        if (shouldUpdate)
        {
            bestScore = score;
            PlayerPrefs.SetInt(key, bestScore);
            PlayerPrefs.Save();
            Debug.Log($"[Backend Saved] Key: {key} updated successfully to score target: {bestScore}");
        }

        return bestScore;
    }

    public void DisplayUpdatedUI(int currentScoreValue, int idValue)
    {
        int currentBestValue = GetBestScore(idValue.ToString());
        bool isTimeBased = idValue >= 30;

        if (currentScoreText != null)
        {
            currentScoreText.text = isTimeBased ? $"Current Time: {currentScoreValue}s" : $"Current Score: {currentScoreValue}";
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = isTimeBased ? $"Best Time: {currentBestValue}s" : $"Best Score: {currentBestValue}";
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

        // Extract active ID type cleanly to format end screen components dynamically
        MinigameDataStore.GameData currentGame = MinigameDataStore.GetCurrentGame();

        // If current game state hasn't initialized from menus, attempt active scene search matching
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

        if (bestScoreText != null)
        {
            // Pull matching best score records using the correct key lookup configuration matrix
            string resolvedLookupKey = currentGame.id != 0 || !string.IsNullOrEmpty(currentGame.gameTitle) ?
                currentGame.id.ToString() : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            int liveBest = GetBestScore(resolvedLookupKey);
            bestScoreText.text = isTimeBased ? $"Best Time: {liveBest}s" : $"Best Score: {liveBest}";
        }
    }

    private static string GetKey(string minigameId)
    {
        return $"{BestScoreKeyPrefix}{minigameId}";
    }
}