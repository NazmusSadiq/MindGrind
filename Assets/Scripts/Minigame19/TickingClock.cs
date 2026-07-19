using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TickingClock : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Clock Panels")]
    [SerializeField] private ClockController[] clocks = new ClockController[3];

    [SerializeField] private TMP_Text[] targetTimeTexts = new TMP_Text[3];

    [SerializeField] private Button[] stopButtons = new Button[3];

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float speedIncreasePerRound = 45f;

    private float currentRotationSpeed = 90f;

    private int roundNumber = 1;

    private float timeRemaining;

    private bool isGameRunning;

    private int score;

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        //Time.timeScale = 1f;

        score = 0;

        timeRemaining = gameDuration;

        isGameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();

        statusText.text = "Click STOP when the hand reaches the target.";

        currentRotationSpeed = 90f;
        roundNumber = 1;

        InitializeClocks();

        RegisterButtons();
    }

    private void Update()
    {
        if (!isGameRunning || Time.timeScale == 0f)
            return;

        timeRemaining -= Time.deltaTime;

        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    #region Initialization

    private bool HasValidSetup()
    {
        if (scoreText == null ||
            timeRemainingText == null ||
            statusText == null ||
            bestScoreStore == null)
        {
            return false;
        }

        if (clocks.Length != 3)
        {
            return false;
        }

        return true;
    }

    private string GenerateRandomTime()
    {
        int hour = Random.Range(1, 13);      // 1-12
        int minute = Random.Range(0, 60);    // 0-59

        return $"{hour}:{minute:00}";
    }

    private void InitializeClocks()
    {
        for (int i = 0; i < clocks.Length; i++)
        {
            string targetTime = GenerateRandomTime();

            targetTimeTexts[i].text = targetTime;

            stopButtons[i].interactable = true;

            clocks[i].ResetClock();

            clocks[i].SetRotationSpeed(currentRotationSpeed);

            clocks[i].SetTargetTime(targetTime);

            clocks[i].StartClock();
        }
    }

    private bool AreAllClocksStopped()
    {
        foreach (ClockController clock in clocks)
        {
            if (!clock.IsStopped)
                return false;
        }

        return true;
    }

    private void StartNextRound()
    {
        roundNumber++;

        currentRotationSpeed += speedIncreasePerRound;

        InitializeClocks();
    }

    private void RegisterButtons()
    {
        for (int i = 0; i < stopButtons.Length; i++)
        {
            int index = i;

            stopButtons[index].onClick.RemoveAllListeners();

            stopButtons[index].onClick.AddListener(() =>
            {
                StopClock(index);
            });
        }
    }

    #endregion

    #region Gameplay

    private void StopClock(int index)
    {
        // FIX: Reject click callbacks immediately if the game loop layout is paused
        if (!isGameRunning || Time.timeScale == 0f)
            return;

        if (index < 0 || index >= clocks.Length)
            return;

        if (clocks[index] == null)
            return;

        if (clocks[index].IsStopped)
            return;

        clocks[index].StopClock();

        stopButtons[index].interactable = false;

        float stoppedAngle = clocks[index].CurrentAngle;

        float targetAngle = clocks[index].TargetAngle;

        float angleDifference =
            Mathf.Abs(Mathf.DeltaAngle(stoppedAngle, targetAngle));

        int gainedScore = CalculateScore(angleDifference);

        score += gainedScore;

        UpdateScoreUI();

        float accuracy = Mathf.Clamp01(1f - angleDifference / 60f) * 100f;

        statusText.text =
            $"Clock {index + 1}\n" +
            $"Accuracy: {accuracy:0}%   Score: +{gainedScore}";

        if (AreAllClocksStopped())
        {
            StartNextRound();
        }
    }

    private int CalculateScore(float angleDifference)
    {
        angleDifference = Mathf.Clamp(angleDifference, 0f, 180f);

        if (angleDifference >= 90f)
            return 1;

        if (angleDifference >= 60f)
        {
            float t = (90f - angleDifference) / 30f;
            return Mathf.RoundToInt(Mathf.Lerp(2f, 5f, t));
        }

        if (angleDifference >= 30f)
        {
            float t = (60f - angleDifference) / 30f;
            return Mathf.RoundToInt(Mathf.Lerp(6f, 10f, t));
        }

        if (angleDifference >= 10f)
        {
            float t = (30f - angleDifference) / 20f;
            return Mathf.RoundToInt(Mathf.Lerp(11f, 20f, t));
        }

        float tt = (10f - angleDifference) / 10f;

        return Mathf.RoundToInt(Mathf.Lerp(21f, 30f, tt));
    }

    #endregion

    #region UI

    private void UpdateScoreUI()
    {
        scoreText.text = $"Score : {score}";
    }

    private void UpdateTimerUI()
    {
        int sec =
            Mathf.CeilToInt(
                Mathf.Max(0f, timeRemaining));

        timeRemainingText.text =
            $"Time : {sec}";
    }

    #endregion

    #region End Game

    private void EndGame()
    {
        if (!isGameRunning)
            return;

        isGameRunning = false;

        timeRemaining = 0f;

        UpdateTimerUI();

        foreach (Button b in stopButtons)
        {
            if (b != null)
                b.interactable = false;
        }

        foreach (ClockController clock in clocks)
        {
            if (clock != null)
                clock.StopClock();
        }

        string minigameId =
            SceneManager.GetActiveScene().name;

        int bestScore =
            MinigameBestScoreStore.UpdateBestScore(
                minigameId,
                score);

        bestScoreStore.ShowStats(
            score,
            bestScore);
    }

    #endregion
}