using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Level5_Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_Text puzzlesSolvedText; // NEW: UI text element showing progress (e.g., "0/5")

    [Header("Door Unlocking")]
    [SerializeField] private DoorSceneTrigger exitGate; // NEW: Explicitly handles the exit pathway state

    [Header("Puzzle Management")]
    [SerializeField] private List<PowerGridManager> allPuzzles = new List<PowerGridManager>(); // NEW: Tracks all grid managers in Level 5

    private float elapsedTime;
    private bool isLevelOver;
    private int totalPuzzlesCount = 0;
    private int currentSolvedCount = -1; // Default to trigger an initial UI text refresh safely

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

        // Lock the exit gateway cleanly at layout initialization
        if (exitGate != null)
        {
            exitGate.canOpen = false;
        }

        // Cache total puzzle count for display framing matches
        totalPuzzlesCount = allPuzzles.Count;

        elapsedTime = 0f;
        UpdateTimerUI();
        CheckPuzzleProgression(forceUpdateUI: true);

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
        if (isLevelOver || Time.timeScale == 0f)
            return;

        if (playerController.IsDead)
        {
            TriggerGameOver();
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();

        // Continuously evaluate the status tracking loops across grid states
        CheckPuzzleProgression(forceUpdateUI: false);
    }

    /// <summary>
    /// Checks all registered grid managers to update progression and unlock the door when satisfied.
    /// </summary>
    private void CheckPuzzleProgression(bool forceUpdateUI)
    {
        int solvedCounter = 0;

        foreach (PowerGridManager puzzle in allPuzzles)
        {
            // Assuming your PowerGridManager exposes an 'IsSolved' or 'isSolved' property/method
            if (puzzle != null && puzzle.IsSolved)
            {
                solvedCounter++;
            }
        }

        // Only update UI layout structures or check conditions if the solved count actually shifted
        if (solvedCounter != currentSolvedCount || forceUpdateUI)
        {
            currentSolvedCount = solvedCounter;
            UpdatePuzzlesUI();

            if (currentSolvedCount >= totalPuzzlesCount && totalPuzzlesCount > 0)
            {
                UnlockExitGate();
            }
        }
    }

    private void UnlockExitGate()
    {
        if (exitGate != null && !exitGate.canOpen)
        {
            exitGate.canOpen = true;
        }
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

    private void UpdatePuzzlesUI()
    {
        if (puzzlesSolvedText != null)
        {
            puzzlesSolvedText.text = $"{currentSolvedCount}/{totalPuzzlesCount}";
        }
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }
}