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
        bool storyModeActive = false;

        // Retrieve the static 'isStoryMode' field via reflection from MainMenu
        System.Reflection.FieldInfo storyModeField = typeof(MainMenu).GetField("isStoryMode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (storyModeField != null)
        {
            storyModeActive = (bool)storyModeField.GetValue(null);
        }

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            enabled = false;
            return;
        }

        // Auto-initialize game startup execution to ensure inputs unlock properly
        //playerController.SetGameStarted(true);
        //playerController.EnableGameplayInput(true);

        elapsedTime = 0f;
        UpdateTimerUI();

        // Only pause and show details if story mode is already active
        if (storyModeActive)
        {
            MainMenu.PauseGameAndShowDetailsPanel();
        }
        else
        {
            playerController.SetGameStarted(true);
            playerController.EnableGameplayInput(true);
        }
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