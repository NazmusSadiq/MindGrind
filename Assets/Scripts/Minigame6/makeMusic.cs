using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

public class makeMusic : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button startMusicButton;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Music Buttons")]
    [SerializeField] private Button[] noteButtons = new Button[8];
    [SerializeField] private AudioSource[] noteAudioSources = new AudioSource[8];

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int startingSequenceLength = 3;
    [SerializeField] private int maxSequenceLength = 8;
    [SerializeField] private int pointsPerCorrectNote = 10;
    [SerializeField] private int penaltyPerWrongNote = 5;
    [SerializeField] private float firstPreviewHoldDuration = 3f;
    [SerializeField] private float previewHoldDuration = 1.5f;
    [SerializeField] private float resultHoldDuration = 1.5f;
    [SerializeField] private float noteGap = 0.01f;
    [SerializeField] private float fallbackNoteDuration = 2f;

    private readonly List<int> currentSequence = new List<int>();
    private readonly List<int> playerSequence = new List<int>();

    private int score;
    private int sequenceLength;
    private float timeRemaining;
    private bool isGameRunning;
    private bool isPreviewPlaying;
    private bool isAnswerPhaseActive;
    private bool isTimerPaused;
    private bool hasPlayedFirstPreview;
    private Coroutine roundRoutine;
    private Coroutine replayRoutine;
    private UnityAction[] noteButtonActions;

    private void Awake()
    {
        RegisterButtonListeners();
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
        sequenceLength = Mathf.Clamp(startingSequenceLength, 1, GetPlayableNoteCount());
        maxSequenceLength = Mathf.Clamp(maxSequenceLength, sequenceLength, GetPlayableNoteCount());
        timeRemaining = gameDuration;
        isGameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();
        SetAnswerPhase(false);
        SetPreviewState(false);

        roundRoutine = StartCoroutine(StartRound());
    }

    private void Update()
    {
        if (!isGameRunning)
        {
            return;
        }

        if (!isTimerPaused)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();

            if (timeRemaining <= 0f)
            {
                EndGame();
            }
        }
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();
    }

    private void RegisterButtonListeners()
    {
        if (noteButtons != null)
        {
            noteButtonActions = new UnityAction[noteButtons.Length];
            for (int i = 0; i < noteButtons.Length; i++)
            {
                int noteIndex = i;
                if (noteButtons[i] != null)
                {
                    noteButtonActions[i] = () => HandleNotePressed(noteIndex);
                    noteButtons[i].onClick.RemoveListener(noteButtonActions[i]);
                    noteButtons[i].onClick.AddListener(noteButtonActions[i]);
                }
            }
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(ReplayCurrentSequence);
            replayButton.onClick.AddListener(ReplayCurrentSequence);
        }

        if (startMusicButton != null)
        {
            startMusicButton.onClick.RemoveListener(StartAnswerPhase);
            startMusicButton.onClick.AddListener(StartAnswerPhase);
        }
    }

    private void UnregisterButtonListeners()
    {
        if (noteButtons != null)
        {
            for (int i = 0; i < noteButtons.Length; i++)
            {
                if (noteButtons[i] != null
                    && noteButtonActions != null
                    && i < noteButtonActions.Length
                    && noteButtonActions[i] != null)
                {
                    noteButtons[i].onClick.RemoveListener(noteButtonActions[i]);
                }
            }
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(ReplayCurrentSequence);
        }

        if (startMusicButton != null)
        {
            startMusicButton.onClick.RemoveListener(StartAnswerPhase);
        }
    }

    private bool HasValidSetup()
    {
        bool hasReferences = scoreText != null
            && timeRemainingText != null
            && previewPanel != null
            && replayButton != null
            && startMusicButton != null
            && bestScoreStore != null;

        if (!hasReferences)
        {
            Debug.LogError("makeMusic is missing required references.", this);
            return false;
        }

        if (gameDuration <= 0f)
        {
            Debug.LogError("makeMusic game duration must be greater than zero.", this);
            return false;
        }

        if (GetPlayableNoteCount() < 1)
        {
            Debug.LogError("makeMusic needs at least one playable note with a Button and an AudioSource that has an AudioClip.", this);
            return false;
        }

        if (noteButtons.Length != 8)
        {
            Debug.LogWarning("makeMusic is designed for 8 buttons. It will still use every playable button assigned.", this);
        }

        return true;
    }

    private IEnumerator StartRound()
    {
        SetAnswerPhase(false);
        BuildRandomSequence();
        yield return PlayCurrentSequenceWithPreview();

        if (!isGameRunning)
        {
            yield break;
        }

        SetPreviewState(false);
        SetTimerPaused(false);
        UpdateStatusText("Practice the notes, then press Start Music to answer.");
    }

    private void BuildRandomSequence()
    {
        currentSequence.Clear();
        playerSequence.Clear();

        List<int> playableIndexes = GetPlayableNoteIndexes();
        for (int i = 0; i < sequenceLength; i++)
        {
            currentSequence.Add(playableIndexes[Random.Range(0, playableIndexes.Count)]);
        }
    }

    private IEnumerator PlayCurrentSequenceWithPreview()
    {
        isPreviewPlaying = true;
        SetAnswerPhase(false);
        SetPreviewState(true);
        SetTimerPaused(true);
        UpdatePreviewText("Listen carefully !");
        SetControlButtonsInteractable(false);

        float holdDuration = hasPlayedFirstPreview ? previewHoldDuration : firstPreviewHoldDuration;
        hasPlayedFirstPreview = true;
        yield return WaitWhileGameRunning(holdDuration);

        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (!isGameRunning)
            {
                yield break;
            }

            int noteIndex = currentSequence[i];
            UpdatePreviewText($"Listen carefully !\n{i + 1}/{currentSequence.Count}");
            PlayNote(noteIndex);

            yield return WaitWhileGameRunning(GetNoteDuration(noteIndex) + noteGap);
        }

        isPreviewPlaying = false;
        SetControlButtonsInteractable(true);
    }

    public void ReplayCurrentSequence()
    {
        if (!isGameRunning || isPreviewPlaying || replayRoutine != null || currentSequence.Count == 0)
        {
            return;
        }

        replayRoutine = StartCoroutine(ReplayCurrentSequenceInMainScene());
    }

    private IEnumerator ReplayCurrentSequenceInMainScene()
    {
        SetControlButtonsInteractable(false);
        UpdateStatusText("Listening again...");

        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (!isGameRunning)
            {
                replayRoutine = null;
                yield break;
            }

            PlayNote(currentSequence[i]);
            yield return WaitWhileGameRunning(GetNoteDuration(currentSequence[i]) + noteGap);
        }

        if (!isGameRunning)
        {
            replayRoutine = null;
            yield break;
        }

        SetControlButtonsInteractable(true);
        UpdateStatusText(isAnswerPhaseActive ? "Repeat the sequence now." : "Practice the notes, then press Start Music to answer.");
        replayRoutine = null;
    }

    public void StartAnswerPhase()
    {
        if (!isGameRunning || isPreviewPlaying || replayRoutine != null || currentSequence.Count == 0)
        {
            return;
        }

        playerSequence.Clear();
        SetAnswerPhase(true);
        UpdateStatusText($"Repeat the sequence: 0/{currentSequence.Count}");
    }

    private void HandleNotePressed(int noteIndex)
    {
        if (!isGameRunning || isPreviewPlaying || replayRoutine != null || !IsPlayableNote(noteIndex))
        {
            return;
        }

        PlayNote(noteIndex);

        if (!isAnswerPhaseActive)
        {
            return;
        }

        playerSequence.Add(noteIndex);
        UpdateStatusText($"Repeat the sequence: {playerSequence.Count}/{currentSequence.Count}");

        if (playerSequence.Count >= currentSequence.Count)
        {
            ScoreCurrentAnswer();
            StartNextRound();
        }
    }

    private void ScoreCurrentAnswer()
    {
        int correctCount = 0;
        int wrongCount = 0;

        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (i < playerSequence.Count && playerSequence[i] == currentSequence[i])
            {
                correctCount++;
            }
            else
            {
                wrongCount++;
            }
        }

        int roundScore = (correctCount * pointsPerCorrectNote) - (wrongCount * penaltyPerWrongNote);
        score += roundScore;
        UpdateScoreUI();
        UpdateStatusText($"Correct: {correctCount}  Wrong: {wrongCount}  Round: {roundScore}");
    }

    private void StartNextRound()
    {
        SetAnswerPhase(false);
        sequenceLength = Mathf.Min(maxSequenceLength, sequenceLength + 1);

        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
        }

        if (isGameRunning)
        {
            roundRoutine = StartCoroutine(HoldResultThenStartRound());
        }
    }

    private IEnumerator HoldResultThenStartRound()
    {
        SetTimerPaused(true);
        SetControlButtonsInteractable(false);
        yield return WaitWhileGameRunning(resultHoldDuration);

        if (isGameRunning)
        {
            roundRoutine = StartCoroutine(StartRound());
        }
    }

    private void PlayNote(int noteIndex)
    {
        if (!IsPlayableNote(noteIndex))
        {
            return;
        }

        AudioSource noteAudioSource = GetNoteAudioSource(noteIndex);
        noteAudioSource.Stop();
        noteAudioSource.Play();
    }

    private float GetNoteDuration(int noteIndex)
    {
        if (!IsPlayableNote(noteIndex))
        {
            return fallbackNoteDuration;
        }

        AudioSource noteAudioSource = GetNoteAudioSource(noteIndex);
        if (noteAudioSource != null && noteAudioSource.clip != null)
        {
            return noteAudioSource.clip.length;
        }

        return fallbackNoteDuration;
    }

    private bool IsPlayableNote(int noteIndex)
    {
        AudioSource noteAudioSource = GetNoteAudioSource(noteIndex);

        return noteButtons != null
            && noteIndex >= 0
            && noteIndex < noteButtons.Length
            && noteButtons[noteIndex] != null
            && noteAudioSource != null
            && noteAudioSource.clip != null;
    }

    private AudioSource GetNoteAudioSource(int noteIndex)
    {
        if (noteButtons == null || noteIndex < 0 || noteIndex >= noteButtons.Length)
        {
            return null;
        }

        if (noteAudioSources != null
            && noteIndex < noteAudioSources.Length
            && noteAudioSources[noteIndex] != null)
        {
            return noteAudioSources[noteIndex];
        }

        if (noteButtons[noteIndex] == null)
        {
            return null;
        }

        return noteButtons[noteIndex].GetComponent<AudioSource>();
    }

    private int GetPlayableNoteCount()
    {
        return GetPlayableNoteIndexes().Count;
    }

    private List<int> GetPlayableNoteIndexes()
    {
        List<int> playableIndexes = new List<int>();
        if (noteButtons == null)
        {
            return playableIndexes;
        }

        for (int i = 0; i < noteButtons.Length; i++)
        {
            if (IsPlayableNote(i))
            {
                playableIndexes.Add(i);
            }
        }

        return playableIndexes;
    }

    private void SetAnswerPhase(bool isActive)
    {
        isAnswerPhaseActive = isActive;
        if (startMusicButton != null)
        {
            startMusicButton.interactable = isGameRunning && !isPreviewPlaying && !isActive;
        }
    }

    private void SetPreviewState(bool isActive)
    {
        if (previewPanel != null)
        {
            previewPanel.SetActive(isActive);
        }
    }

    private void SetTimerPaused(bool isPaused)
    {
        isTimerPaused = isPaused;
    }

    private IEnumerator WaitWhileGameRunning(float duration)
    {
        float elapsed = 0f;
        while (isGameRunning && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SetControlButtonsInteractable(bool isInteractable)
    {
        if (replayButton != null)
        {
            replayButton.interactable = isInteractable && isGameRunning;
        }

        if (startMusicButton != null)
        {
            startMusicButton.interactable = isInteractable && isGameRunning && !isAnswerPhaseActive;
        }

        if (noteButtons == null)
        {
            return;
        }

        for (int i = 0; i < noteButtons.Length; i++)
        {
            if (noteButtons[i] != null)
            {
                noteButtons[i].interactable = isInteractable && isGameRunning;
            }
        }
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
            int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timeRemainingText.text = $"Time : {secondsLeft.ToString()}";
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void UpdatePreviewText(string message)
    {
        if (previewText != null)
        {
            previewText.text = message;
        }
    }

    private void EndGame()
    {
        if (!isGameRunning)
        {
            return;
        }

        isGameRunning = false;
        isPreviewPlaying = false;
        isAnswerPhaseActive = false;
        isTimerPaused = false;
        timeRemaining = 0f;
        UpdateTimerUI();
        SetControlButtonsInteractable(false);
        SetPreviewState(false);

        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }

        if (replayRoutine != null)
        {
            StopCoroutine(replayRoutine);
            replayRoutine = null;
        }

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
        Debug.Log($"Minigame finished. Current score: {score}, Best score: {bestScore}");
    }
}
