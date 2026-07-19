using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SequenceMatchMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text sequenceText;
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Tooltip("The serialized AudioSource used to play success and failure sound effects.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float sequencePreviewDuration = 5f;
    [SerializeField] private int startingTargetCount = 2;
    [SerializeField] private int maxTargetCount = 8;
    [SerializeField] private int extraSequenceCharacters = 4;
    [SerializeField] private string characterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly List<char> currentSequence = new List<char>();
    private readonly List<char> currentTargets = new List<char>();

    private string expectedAnswer;
    private int targetCount;
    private int score;
    private float timeRemaining;
    private bool isGameRunning;
    private bool isInputPhaseActive;
    private Coroutine roundRoutine;

    private void Awake()
    {
        if (answerInputField != null)
        {
            answerInputField.onSubmit.RemoveListener(SubmitAnswerFromInput);
            answerInputField.onSubmit.AddListener(SubmitAnswerFromInput);
        }

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
    }

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
        score = 0;
        targetCount = Mathf.Clamp(startingTargetCount, 1, GetMaxPossibleTargetCount());
        maxTargetCount = Mathf.Clamp(maxTargetCount, targetCount, GetMaxPossibleTargetCount());
        timeRemaining = gameDuration;
        isGameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();
        SetInputInteractable(false);
        roundRoutine = StartCoroutine(StartRound());
    }

    private void Update()
    {
        if (!isGameRunning || Time.timeScale == 0f)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    private void OnDestroy()
    {
        if (answerInputField != null)
        {
            answerInputField.onSubmit.RemoveListener(SubmitAnswerFromInput);
        }
    }

    private bool HasValidSetup()
    {
        bool hasReferences = sequenceText != null
            && targetText != null
            && scoreText != null
            && timeRemainingText != null
            && answerInputField != null
            && bestScoreStore != null;

        if (!hasReferences)
        {
            return false;
        }

        if (sequencePreviewDuration <= 0f || gameDuration <= 0f)
        {
            return false;
        }

        if (GetUniqueCharacterCount() == 0)
        {
            return false;
        }

        return true;
    }

    private IEnumerator StartRound()
    {
        isInputPhaseActive = false;
        SetInputInteractable(false);
        answerInputField.text = string.Empty;
        targetText.text = string.Empty;

        BuildRound();
        sequenceText.text = GetSpacedText(currentSequence);

        float elapsed = 0f;
        while (isGameRunning && elapsed < sequencePreviewDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!isGameRunning)
        {
            yield break;
        }

        sequenceText.text = string.Empty;
        targetText.text = $"Targets: {GetSpacedText(GetShuffledCopy(currentTargets))}";
        isInputPhaseActive = true;
        SetInputInteractable(true);
        answerInputField.ActivateInputField();
    }

    private void BuildRound()
    {
        currentSequence.Clear();
        currentTargets.Clear();

        List<char> availableCharacters = GetUniqueCharacters();
        Shuffle(availableCharacters);

        int sequenceLength = Mathf.Min(
            availableCharacters.Count,
            Mathf.Max(targetCount, targetCount + Mathf.Max(0, extraSequenceCharacters)));

        for (int i = 0; i < sequenceLength; i++)
        {
            currentSequence.Add(availableCharacters[i]);
        }

        List<int> targetIndexes = new List<int>(currentSequence.Count);
        for (int i = 0; i < currentSequence.Count; i++)
        {
            targetIndexes.Add(i);
        }

        Shuffle(targetIndexes);
        targetIndexes.RemoveRange(targetCount, targetIndexes.Count - targetCount);
        targetIndexes.Sort();

        StringBuilder answerBuilder = new StringBuilder(targetIndexes.Count);
        for (int i = 0; i < targetIndexes.Count; i++)
        {
            char targetCharacter = currentSequence[targetIndexes[i]];
            currentTargets.Add(targetCharacter);
            answerBuilder.Append(targetCharacter);
        }

        expectedAnswer = answerBuilder.ToString();
    }

    private void SubmitAnswer()
    {
        if (!isGameRunning || !isInputPhaseActive)
        {
            return;
        }

        string submittedAnswer = NormalizeAnswer(answerInputField.text);
        if (submittedAnswer == expectedAnswer)
        {
            score += 10;
            targetCount = Mathf.Min(maxTargetCount, targetCount + 1);
            PlayFeedbackSFX(successClip);
        }
        else
        {
            score -= 5;
            PlayFeedbackSFX(failureClip);
        }

        UpdateScoreUI();
        RestartRound();
    }

    private void SubmitAnswerFromInput(string _)
    {
        SubmitAnswer();
    }

    private void PlayFeedbackSFX(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    private void RestartRound()
    {
        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
        }

        if (!isGameRunning)
        {
            return;
        }

        roundRoutine = StartCoroutine(StartRound());
    }

    private string NormalizeAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return string.Empty;
        }

        StringBuilder normalizedAnswer = new StringBuilder(answer.Length);
        for (int i = 0; i < answer.Length; i++)
        {
            if (!char.IsWhiteSpace(answer[i]))
            {
                normalizedAnswer.Append(char.ToUpperInvariant(answer[i]));
            }
        }

        return normalizedAnswer.ToString();
    }

    private void SetInputInteractable(bool isInteractable)
    {
        if (answerInputField != null)
        {
            answerInputField.interactable = isInteractable;
        }
    }

    private void UpdateScoreUI()
    {
        scoreText.text = score.ToString();
    }

    private void UpdateTimerUI()
    {
        int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
        timeRemainingText.text = secondsLeft.ToString();
    }

    private void EndGame()
    {
        if (!isGameRunning)
        {
            return;
        }

        isGameRunning = false;
        isInputPhaseActive = false;
        timeRemaining = 0f;
        UpdateTimerUI();
        SetInputInteractable(false);

        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
    }

    private int GetMaxPossibleTargetCount()
    {
        return Mathf.Max(1, GetUniqueCharacterCount());
    }

    private int GetUniqueCharacterCount()
    {
        return GetUniqueCharacters().Count;
    }

    private List<char> GetUniqueCharacters()
    {
        List<char> uniqueCharacters = new List<char>();
        if (string.IsNullOrEmpty(characterPool))
        {
            return uniqueCharacters;
        }

        for (int i = 0; i < characterPool.Length; i++)
        {
            char character = char.ToUpperInvariant(characterPool[i]);
            if (!char.IsWhiteSpace(character) && !uniqueCharacters.Contains(character))
            {
                uniqueCharacters.Add(character);
            }
        }

        return uniqueCharacters;
    }

    private string GetSpacedText(IReadOnlyList<char> characters)
    {
        if (characters == null || characters.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder((characters.Count * 2) - 1);
        for (int i = 0; i < characters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(characters[i]);
        }

        return builder.ToString();
    }

    private List<char> GetShuffledCopy(List<char> characters)
    {
        List<char> shuffledCharacters = new List<char>(characters);
        Shuffle(shuffledCharacters);
        return shuffledCharacters;
    }

    private void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            T item = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = item;
        }
    }
}