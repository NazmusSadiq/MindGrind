using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class number_memory_minigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successMusic;
    [SerializeField] private AudioClip failureMusic;

    [Header("Gameplay Settings")]
    [SerializeField] private float gameDuration = 60.0f;

    private int k = 1;
    private int score = 0;
    private float timeRemaining;
    private string currentNumber = string.Empty;
    private bool isGameRunning = false;
    private bool isShowingNumber = false;
    private Coroutine activeRoundRoutine;

    private void Awake()
    {
        if (answerInputField != null)
        {
            answerInputField.onSubmit.RemoveListener(SubmitAnswer);
            answerInputField.onSubmit.AddListener(SubmitAnswer);
        }
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
        timeRemaining = gameDuration;
        k = 1;
        isGameRunning = true;

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
        }

        UpdateScoreUI();
        UpdateTimerUI();

        StartNewRound();
    }

    private void Update()
    {
        if (!isGameRunning || Time.timeScale == 0f) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerUI();
            EndGame();
            return;
        }

        UpdateTimerUI();
    }

    private void OnDestroy()
    {
        if (answerInputField != null)
        {
            answerInputField.onSubmit.RemoveListener(SubmitAnswer);
        }
    }

    private bool HasValidSetup()
    {
        bool hasReferences = numberText != null
            && scoreText != null
            && timerText != null
            && instructionText != null
            && answerInputField != null
            && bestScoreStore != null;

        return hasReferences;
    }

    private void StartNewRound()
    {
        if (!isGameRunning) return;

        if (activeRoundRoutine != null)
        {
            StopCoroutine(activeRoundRoutine);
        }
        activeRoundRoutine = StartCoroutine(ShowNumberRoutine());
    }

    private float GetPreviewDuration(int length)
    {
        if (length <= 3) return 2.0f;
        if (length <= 7) return 3.0f;
        if (length <= 12) return 4.0f;
        return 5.0f;
    }

    private IEnumerator ShowNumberRoutine()
    {
        isShowingNumber = true;

        // Hide input field and clear text
        if (answerInputField != null)
        {
            answerInputField.text = string.Empty;
            answerInputField.gameObject.SetActive(false);
        }

        // Generate and show number
        currentNumber = GenerateRandomNumberString(k);
        if (numberText != null)
        {
            numberText.text = currentNumber;
            numberText.gameObject.SetActive(true);
        }

        // Timer countdown for preview (dynamic duration based on sequence length)
        float previewDuration = GetPreviewDuration(k);
        float elapsed = 0f;
        while (elapsed < previewDuration)
        {
            if (!isGameRunning) yield break;

            float remaining = previewDuration - elapsed;
            if (instructionText != null)
            {
                instructionText.text = $"Memorise the digits! ({Mathf.CeilToInt(remaining)}s)";
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        // Transition to input phase
        if (numberText != null)
        {
            numberText.text = string.Empty;
            numberText.gameObject.SetActive(false);
        }

        if (instructionText != null)
        {
            instructionText.text = "Enter the number you saw:";
        }

        if (answerInputField != null)
        {
            answerInputField.gameObject.SetActive(true);
            answerInputField.ActivateInputField();
        }

        isShowingNumber = false;
    }

    private string GenerateRandomNumberString(int length)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
        {
            if (i == 0)
            {
                sb.Append(Random.Range(1, 10)); // No leading zero
            }
            else
            {
                sb.Append(Random.Range(0, 10));
            }
        }
        return sb.ToString();
    }

    private void SubmitAnswer(string input)
    {
        if (!isGameRunning || isShowingNumber) return;

        string trimmedInput = input.Trim();
        bool isCorrect = trimmedInput.Equals(currentNumber);

        if (isCorrect)
        {
            k++;
            score += 10;
            PlaySound(successMusic);
        }
        else
        {
            // Subtract 5 points, clamped at 0 so score doesn't become negative
            score = Mathf.Max(0, score - 5);
            PlaySound(failureMusic);
        }

        UpdateScoreUI();
        StartNewRound();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";
        }
    }

    private void EndGame()
    {
        if (!isGameRunning) return;

        isGameRunning = false;

        if (activeRoundRoutine != null)
        {
            StopCoroutine(activeRoundRoutine);
            activeRoundRoutine = null;
        }

        if (answerInputField != null)
        {
            answerInputField.text = string.Empty;
            answerInputField.gameObject.SetActive(false);
        }

        if (numberText != null)
        {
            numberText.gameObject.SetActive(false);
        }

        if (instructionText != null)
        {
            instructionText.text = "Game Over!";
        }

        string minigameSceneName = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameSceneName, score);

        bestScoreStore.ShowStats(score, bestScore);
    }
}