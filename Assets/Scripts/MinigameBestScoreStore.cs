using TMPro;
using UnityEngine;

public class MinigameBestScoreStore : MonoBehaviour
{
    private const string BestScoreKeyPrefix = "BestScore_";

    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private TMP_Text gameTitleText;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;

    public static int GetBestScore(string minigameId)
    {
        return PlayerPrefs.GetInt(GetKey(minigameId), 0);
    }

    public static int UpdateBestScore(string minigameId, int score)
    {
        string key = GetKey(minigameId);
        int bestScore = PlayerPrefs.GetInt(key, 0);

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(key, bestScore);
            PlayerPrefs.Save();
        }

        return bestScore;
    }

    public void ShowStats(int currentScore, int bestScore)
    {
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
        }

        Time.timeScale = 0f;

        if (gameTitleText != null)
        {
            gameTitleText.text = MinigameDataStore.GetCurrentGame().gameTitle;
        }

        if (currentScoreText != null)
        {
            currentScoreText.text = $"Current Score: {currentScore}";
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = $"Best Score: {bestScore}";
        }
    }

    private static string GetKey(string minigameId)
    {
        return $"{BestScoreKeyPrefix}{minigameId}";
    }
}
