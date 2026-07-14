using TMPro;
using UnityEngine;

public class Level4_Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text timeRemainingText;

    private float elapsedTime;
    private bool isLevelOver;

    private void Start()
    {
        MainMenu mainMenuFallback = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenuFallback != null)
        {
            mainMenuFallback.SetStoryMode(true);
        }

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogError("Level4_Manager could not find a PlayerController.", this);
            enabled = false;
            return;
        }

        // Auto-initialize game startup execution to ensure inputs unlock properly
        //playerController.SetGameStarted(true);
        //playerController.EnableGameplayInput(true);

        elapsedTime = 0f;
        UpdateTimerUI();

        MainMenu.PauseGameAndShowDetailsPanel();
    }

    private void Update()
    {
        if (isLevelOver)
            return;

        if (playerController.IsDead)
        {
            TriggerGameOver();
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    public void TriggerLevelComplete()
    {
        if (isLevelOver)
            return;

        isLevelOver = true;
        Debug.Log($"Level 4 Completed in: {elapsedTime:F2} seconds!");
    }

    private void TriggerGameOver()
    {
        isLevelOver = true;

        if (playerController != null)
        {
            playerController.ShowGameOverMenu();
        }
    }

    private void UpdateTimerUI()
    {
        if (timeRemainingText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        if (minutes > 0)
        {
            timeRemainingText.text = $"{minutes}:{seconds:00}";
        }
        else
        {
            timeRemainingText.text = seconds.ToString();
        }
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }
}