using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TheFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Tooltip("The serialized AudioSource used to play success and failure sound effects.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

    [Header("Button Rows")]
    [SerializeField] private Button[] topRow = new Button[13];
    [SerializeField] private Button[] secondRow = new Button[13];
    [SerializeField] private Button[] thirdRow = new Button[13];
    [SerializeField] private Button[] fourthRow = new Button[13];
    [SerializeField] private Button[] fifthRow = new Button[13];

    [Header("Button Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color litColor = Color.yellow;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float firstLightDelay = 0.5f;
    [SerializeField] private Vector2 nextLightDelayRange = new Vector2(0.35f, 0.9f);

    [SerializeField] private int maxPointsPerHit = 10;
    [SerializeField] private int minPointsPerHit = 1;

    [SerializeField] private float perfectClickTime = 0.15f;
    [SerializeField] private float slowClickTime = 2f;

    [SerializeField] private int wrongClickPenalty = 5;

    private Button[][] buttonRows;
    private UnityAction[][] buttonActions;

    private Button activeButton;
    private Image activeButtonImage;

    private Coroutine lightRoutine;

    private int activeRow = -1;
    private int activeColumn = -1;

    private int score;
    private int hitCount;

    private float timeRemaining;
    private float activeLitTime; // Now stores elapsedGameTime at the moment of lighting up

    private float elapsedGameTime;

    private float currentLightInterval = 3f;

    private bool activeButtonAlreadyScored;

    private bool isGameRunning;
    private bool isWaitingForNextLight;

    private readonly Queue<string> statusHistory = new Queue<string>();
    [SerializeField] private int maxStatusLines = 3;

    private void Awake()
    {
        buttonRows = new Button[][]
        {
            topRow,
            secondRow,
            thirdRow,
            fourthRow,
            fifthRow
        };

        // Fallback to internal AudioSource component if one isn't explicitly assigned in the Inspector
        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
        }

        if (sfxAudioSource != null)
        {
            sfxAudioSource.loop = false;
            sfxAudioSource.playOnAwake = false;
        }

        RegisterButtonListeners();
    }

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        //Time.timeScale = 1f;

        score = 0;
        hitCount = 0;

        timeRemaining = gameDuration;
        elapsedGameTime = 0f;
        currentLightInterval = 3f;
        activeButtonAlreadyScored = false;
        isGameRunning = true;

        SetAllButtonsInteractable(true);

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateStatusText("Get Ready...");

        lightRoutine = StartCoroutine(LightLoop(firstLightDelay));
    }

    private void Update()
    {
        if (!isGameRunning || Time.timeScale == 0f)
            return;

        timeRemaining -= Time.deltaTime;
        elapsedGameTime += Time.deltaTime;

        UpdateTimerUI();

        currentLightInterval = GetCurrentInterval();

        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    private void OnDestroy()
    {
        if (buttonActions == null)
            return;

        for (int r = 0; r < buttonRows.Length; r++)
        {
            for (int c = 0; c < buttonRows[r].Length; c++)
            {
                if (buttonRows[r][c] != null &&
                    buttonActions[r][c] != null)
                {
                    buttonRows[r][c].onClick.RemoveListener(buttonActions[r][c]);
                }
            }
        }
    }

    private bool HasValidSetup()
    {
        if (scoreText == null ||
            timeRemainingText == null ||
            bestScoreStore == null)
        {
            return false;
        }

        int count = 0;

        foreach (Button[] row in buttonRows)
        {
            foreach (Button b in row)
            {
                if (b != null)
                    count++;
            }
        }

        if (count == 0)
        {
            return false;
        }

        return true;
    }

    private void RegisterButtonListeners()
    {
        buttonActions = new UnityAction[buttonRows.Length][];

        for (int r = 0; r < buttonRows.Length; r++)
        {
            buttonActions[r] = new UnityAction[buttonRows[r].Length];

            for (int c = 0; c < buttonRows[r].Length; c++)
            {
                if (buttonRows[r][c] == null)
                    continue;

                int rr = r;
                int cc = c;

                buttonActions[r][c] = () =>
                {
                    HandleButtonClicked(rr, cc);
                };

                buttonRows[r][c].onClick.AddListener(buttonActions[r][c]);

                Image img = buttonRows[r][c].GetComponent<Image>();

                if (img != null)
                    img.color = normalColor;
            }
        }
    }

    private void SetAllButtonsInteractable(bool value)
    {
        foreach (Button[] row in buttonRows)
        {
            foreach (Button b in row)
            {
                if (b != null)
                    b.interactable = value;
            }
        }
    }

    private Image GetButtonImage(int row, int column)
    {
        if (buttonRows[row][column] == null)
            return null;

        return buttonRows[row][column].GetComponent<Image>();
    }

    private void RestoreCurrentButton()
    {
        if (activeButtonImage != null)
        {
            activeButtonImage.color = normalColor;
        }

        activeButton = null;
        activeButtonImage = null;
        activeRow = -1;
        activeColumn = -1;

        activeButtonAlreadyScored = false;
    }


    private float GetCurrentInterval()
    {
        if (elapsedGameTime < 15f)
            return 3f;

        if (elapsedGameTime < 30f)
            return 2f;

        if (elapsedGameTime < 45f)
            return 1f;

        return 0.75f;
    }

    private int GetCurrentMaxScore()
    {
        if (elapsedGameTime < 15f)
            return 5;

        if (elapsedGameTime < 30f)
            return 10;

        if (elapsedGameTime < 45f)
            return 20;

        return 50;
    }

    private int GetCurrentMinScore()
    {
        if (elapsedGameTime >= 45f)
            return 50;

        return 1;
    }


    private IEnumerator LightLoop(float delay)
    {
        // FIX: Replaced standard WaitForSeconds with pause-compliant loop tracking
        float delayElapsed = 0f;
        while (delayElapsed < delay)
        {
            if (Time.timeScale > 0f) delayElapsed += Time.deltaTime;
            yield return null;
        }

        while (isGameRunning)
        {
            RestoreCurrentButton();

            LightRandomButton();

            // FIX: Replaced standard WaitForSeconds with pause-compliant loop tracking
            float intervalElapsed = 0f;
            float targetInterval = GetCurrentInterval();
            while (intervalElapsed < targetInterval)
            {
                if (Time.timeScale > 0f) intervalElapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void LightRandomButton()
    {
        int totalButtons = 0;

        foreach (Button[] row in buttonRows)
        {
            foreach (Button b in row)
            {
                if (b != null)
                    totalButtons++;
            }
        }

        if (totalButtons == 0)
            return;

        int randomIndex = Random.Range(0, totalButtons);

        int current = 0;

        for (int r = 0; r < buttonRows.Length; r++)
        {
            for (int c = 0; c < buttonRows[r].Length; c++)
            {
                if (buttonRows[r][c] == null)
                    continue;

                if (current == randomIndex)
                {
                    activeButton = buttonRows[r][c];
                    activeButtonImage = GetButtonImage(r, c);

                    activeRow = r;
                    activeColumn = c;

                    // FIX: Track using our pause-safe elapsedGameTime instead of real-time Time.time
                    activeLitTime = elapsedGameTime;

                    if (activeButtonImage != null)
                        activeButtonImage.color = litColor;

                    return;
                }

                current++;
            }
        }
    }

    private void HandleButtonClicked(int row, int column)
    {
        // FIX: Ignore click metrics completely if the game is paused (Time.timeScale == 0)
        if (!isGameRunning || Time.timeScale == 0f)
            return;

        if (statusHistory.Count > 0)
        {
            statusHistory.Clear();
        }

        if (activeButton == null)
            return;

        if (activeButtonAlreadyScored)
            return;


        if (row != activeRow || column != activeColumn)
        {
            score -= wrongClickPenalty;

            UpdateScoreUI();

            UpdateStatusText($"Wrong Button!          -{wrongClickPenalty}");

            PlayFeedbackSFX(failureClip);

            return;
        }

        // FIX: Calculate using pause-safe elapsed game time metrics
        float reactionTime = elapsedGameTime - activeLitTime;

        int points = CalculateReactionScore(reactionTime);

        score += points;
        activeButtonAlreadyScored = true;
        hitCount++;

        UpdateScoreUI();

        UpdateStatusText(
            $"Correct :   {reactionTime:0.00}s      +{points}");

        PlayFeedbackSFX(successClip);

        RestoreCurrentButton();
    }

    private void PlayFeedbackSFX(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    private int CalculateReactionScore(float reactionTime)
    {
        if (elapsedGameTime >= 50f)
            return 50;

        float interval = GetCurrentInterval();

        reactionTime = Mathf.Clamp(reactionTime, 0f, interval);

        float percent = reactionTime / interval;

        if (elapsedGameTime < 15f)
        {
            if (percent <= 0.20f) return 5;
            if (percent <= 0.40f) return 4;
            if (percent <= 0.60f) return 3;
            if (percent <= 0.80f) return 2;
            return 1;
        }
        else if (elapsedGameTime < 35f)
        {
            if (percent <= 0.10f) return 10;
            if (percent <= 0.20f) return 9;
            if (percent <= 0.30f) return 8;
            if (percent <= 0.40f) return 7;
            if (percent <= 0.50f) return 6;
            if (percent <= 0.60f) return 5;
            if (percent <= 0.70f) return 4;
            if (percent <= 0.80f) return 3;
            if (percent <= 0.90f) return 2;
            return 1;
        }
        else if (elapsedGameTime < 50f)
        {
            if (percent <= 0.10f) return 20;
            if (percent <= 0.20f) return 18;
            if (percent <= 0.30f) return 16;
            if (percent <= 0.40f) return 14;
            if (percent <= 0.50f) return 12;
            if (percent <= 0.60f) return 10;
            if (percent <= 0.70f) return 8;
            if (percent <= 0.80f) return 6;
            if (percent <= 0.90f) return 4;
            return 2;
        }

        return 50;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score : {score}";
        }
    }

    private void UpdateTimerUI()
    {
        if (timeRemainingText != null)
        {
            int sec = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timeRemainingText.text = $"Time : {sec}";
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText == null)
            return;

        statusHistory.Enqueue(message);

        while (statusHistory.Count > maxStatusLines)
            statusHistory.Dequeue();

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        foreach (string msg in statusHistory)
        {
            builder.AppendLine(msg);
        }

        statusText.text = builder.ToString();
    }

    private void EndGame()
    {
        if (!isGameRunning)
            return;

        isGameRunning = false;
        timeRemaining = 0f;

        UpdateTimerUI();
        SetAllButtonsInteractable(false);

        if (lightRoutine != null)
        {
            StopCoroutine(lightRoutine);
            lightRoutine = null;
        }

        RestoreCurrentButton();

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
    }
}