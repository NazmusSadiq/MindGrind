using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class DoorSceneTrigger : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject successMenu;
    [SerializeField] private TMP_Text successCurrentScoreText;
    [SerializeField] private TMP_Text successBestScoreText;
    [SerializeField] public bool canOpen;

    [Header("Score Settings (For IDs < 30)")]
    [SerializeField] private int currentScore;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null || !canOpen) return;
        //if (EnemyController.IsAnyEnemyAware()) return;

        MinigameDataStore.GameData currentGame = MinigameDataStore.GetCurrentGame();

        if (string.IsNullOrEmpty(currentGame.sceneName))
        {
            string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (MinigameDataStore.TryGetGameDataBySceneName(activeSceneName, out MinigameDataStore.GameData sceneGame))
            {
                currentGame = sceneGame;
                MinigameDataStore.SetCurrentGame(currentGame);
            }
        }

        if (!string.IsNullOrEmpty(currentGame.gameTitle))
        {
            string activeIdString = currentGame.id.ToString();
            int finalValueToSave = 0;

            if (currentGame.id < 30)
            {
                finalValueToSave = currentScore;
            }
            else
            {
                finalValueToSave = Mathf.RoundToInt(Time.timeSinceLevelLoad + MinigamePenaltyTracker.PenaltySeconds);
            }

            int updatedBestScore = MinigameBestScoreStore.UpdateBestScore(activeIdString, finalValueToSave);
            int updatedAverageScore = MinigameBestScoreStore.GetAverageScore(activeIdString);

            if (successCurrentScoreText != null)
            {
                successCurrentScoreText.text = currentGame.id < 30 ? $"Score: {finalValueToSave}" : $"Time: {finalValueToSave}s";
            }
            if (successBestScoreText != null)
            {
                successBestScoreText.text = currentGame.id < 30 ? $"Best Score: {updatedBestScore}" : $"Best Time: {updatedBestScore}s";
            }

            MinigameBestScoreStore uiStore = Object.FindFirstObjectByType<MinigameBestScoreStore>();
            if (uiStore != null)
            {
                uiStore.DisplayUpdatedUI(finalValueToSave, currentGame.id);
            }
            else
            {
                Debug.LogWarning("DoorSceneTrigger: Found no MinigameBestScoreStore component instance active in this scene scene context.", this);
            }
        }
        else
        {
            Debug.LogError($"DoorSceneTrigger: Could not match the active scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' to any entry configured inside MinigameDataStore!", this);
        }

        Time.timeScale = 0f;
        if (successMenu != null) successMenu.SetActive(true);

        Level1_Manager levelManager = Object.FindFirstObjectByType<Level1_Manager>();
        if (levelManager != null) levelManager.TriggerLevelComplete();
    }
}