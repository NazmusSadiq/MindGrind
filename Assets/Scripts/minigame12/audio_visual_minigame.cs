using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class audio_visual_minigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private Button matchButton;
    [SerializeField] private Button noMatchButton;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;
    [SerializeField] private TextAsset wordDictionaryFile;
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Assign the Story Mode Details Panel here. Audio playback and user input will wait until this object is hidden/disabled.")]
    [SerializeField] private GameObject triggerObject;

    [Tooltip("The serialized AudioSource used to play success and failure sound effects.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

    [Header("Gameplay")]
    [SerializeField, Range(0f, 1f)] private float matchProbability = 0.5f;

    private readonly List<string> allWords = new List<string>();
    private readonly List<string> unusedWords = new List<string>();
    private readonly HashSet<string> shownWords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private string currentTextWord;
    private string soundWord;
    private bool isCurrentMatch;
    private int score;
    private int lives;
    private bool isGameRunning;
    private Coroutine ttsCoroutine;

    // Helper property to check if details panel is active on screen
    private bool IsDetailsPanelActive => triggerObject != null && triggerObject.activeInHierarchy;

    private void Awake()
    {
        // Dynamically find general UI references if not assigned in Inspector
        if (wordText == null) wordText = GameObject.Find("Sequence_Text")?.GetComponent<TMP_Text>();
        if (scoreText == null) scoreText = GameObject.Find("Score_Value")?.GetComponent<TMP_Text>();
        if (livesText == null) livesText = GameObject.Find("Reamining_Lives_Value")?.GetComponent<TMP_Text>();
        if (matchButton == null) matchButton = GameObject.Find("match-btn")?.GetComponent<Button>();
        if (noMatchButton == null) noMatchButton = GameObject.Find("no_match_btn")?.GetComponent<Button>();
        if (audioSource == null) audioSource = GameObject.Find("Audio Source")?.GetComponent<AudioSource>();
        if (bestScoreStore == null) bestScoreStore = FindFirstObjectByType<MinigameBestScoreStore>();

        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }

        if (sfxAudioSource != null)
        {
            sfxAudioSource.loop = false;
            sfxAudioSource.playOnAwake = false;
        }

        if (matchButton != null)
        {
            matchButton.onClick.RemoveListener(OnMatchButtonClicked);
            matchButton.onClick.AddListener(OnMatchButtonClicked);
        }
        if (noMatchButton != null)
        {
            noMatchButton.onClick.RemoveListener(OnNoMatchButtonClicked);
            noMatchButton.onClick.AddListener(OnNoMatchButtonClicked);
        }
    }

    private void Start()
    {
        LoadDictionary();

        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        score = 0;
        lives = 3;
        isGameRunning = true;

        shownWords.Clear();
        unusedWords.Clear();
        unusedWords.AddRange(allWords);

        UpdateScoreUI();
        UpdateLivesUI();

        ShowNextWordAndPlaySound();
    }

    private void Update()
    {
        // Block player keyboard inputs while game is paused, non-running, or while details panel is visible
        if (!isGameRunning || Time.timeScale == 0f || IsDetailsPanelActive)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.nKey.wasPressedThisFrame)
            {
                OnNoMatchButtonClicked();
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame || keyboard.yKey.wasPressedThisFrame)
            {
                OnMatchButtonClicked();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.N))
        {
            OnNoMatchButtonClicked();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Y))
        {
            OnMatchButtonClicked();
        }
#endif
    }

    private void OnDestroy()
    {
        if (matchButton != null)
        {
            matchButton.onClick.RemoveListener(OnMatchButtonClicked);
        }
        if (noMatchButton != null)
        {
            noMatchButton.onClick.RemoveListener(OnNoMatchButtonClicked);
        }
        if (ttsCoroutine != null)
        {
            StopCoroutine(ttsCoroutine);
        }
    }

    private bool HasValidSetup()
    {
        bool hasReferences = wordText != null
            && scoreText != null
            && livesText != null
            && matchButton != null
            && noMatchButton != null
            && bestScoreStore != null
            && wordDictionaryFile != null
            && audioSource != null
            && sfxAudioSource != null;

        if (!hasReferences)
        {
            return false;
        }

        if (allWords.Count == 0)
        {
            return false;
        }

        return true;
    }

    private void LoadDictionary()
    {
        allWords.Clear();
        if (wordDictionaryFile == null)
        {
            return;
        }

        string[] lines = wordDictionaryFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string word = lines[i].Trim().ToLower();
            if (!string.IsNullOrEmpty(word))
            {
                allWords.Add(word);
            }
        }
    }

    private void ShowNextWordAndPlaySound()
    {
        if (!isGameRunning)
        {
            return;
        }

        // 1. Choose the text word to display on screen
        if (unusedWords.Count == 0)
        {
            unusedWords.AddRange(allWords);
        }

        List<string> textCandidates = new List<string>(unusedWords);
        if (textCandidates.Count > 1 && !string.IsNullOrEmpty(currentTextWord))
        {
            textCandidates.Remove(currentTextWord);
        }

        string nextTextWord = textCandidates[Random.Range(0, textCandidates.Count)];
        unusedWords.Remove(nextTextWord);
        currentTextWord = nextTextWord;

        // Display on screen
        if (wordText != null)
        {
            wordText.text = currentTextWord;
        }

        // 2. Choose the sound word
        List<string> matchCandidates = new List<string>();
        foreach (string w in shownWords)
        {
            if (!w.Equals(currentTextWord, System.StringComparison.OrdinalIgnoreCase))
            {
                matchCandidates.Add(w);
            }
        }

        bool playMatch = false;
        if (matchCandidates.Count > 0)
        {
            playMatch = (Random.value < matchProbability);
        }

        if (playMatch)
        {
            soundWord = matchCandidates[Random.Range(0, matchCandidates.Count)];
            isCurrentMatch = true;
        }
        else
        {
            List<string> noMatchCandidates = new List<string>();
            foreach (string w in allWords)
            {
                if (!shownWords.Contains(w) && !w.Equals(currentTextWord, System.StringComparison.OrdinalIgnoreCase))
                {
                    noMatchCandidates.Add(w);
                }
            }

            if (noMatchCandidates.Count == 0)
            {
                noMatchCandidates.AddRange(allWords);
                noMatchCandidates.Remove(currentTextWord);
            }

            soundWord = noMatchCandidates[Random.Range(0, noMatchCandidates.Count)];
            isCurrentMatch = false;
        }

        // Add currentTextWord to history of shown words
        shownWords.Add(currentTextWord);

        // Stop any running TTS request and play the new sound
        if (ttsCoroutine != null)
        {
            StopCoroutine(ttsCoroutine);
        }
        ttsCoroutine = StartCoroutine(PlayWordSoundCoroutine(soundWord));
    }

    private IEnumerator PlayWordSoundCoroutine(string word)
    {
        // 1. Wait here if the details panel is active or if Time.timeScale is 0
        while (IsDetailsPanelActive || Time.timeScale == 0f)
        {
            yield return null;
        }

        string url = "https://translate.google.com/translate_tts?ie=UTF-8&q=" + UnityWebRequest.EscapeURL(word) + "&tl=en&client=tw-ob";
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            // 2. Double-check before playing clip in case details panel re-opened during the network request
            while (IsDetailsPanelActive || Time.timeScale == 0f)
            {
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (audioSource != null && clip != null && isGameRunning)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }
        }
    }

    private void OnMatchButtonClicked()
    {
        HandleAnswer(true);
    }

    private void OnNoMatchButtonClicked()
    {
        HandleAnswer(false);
    }

    private void HandleAnswer(bool answeredMatch)
    {
        // Prevent action if game is stopped or details panel is showing
        if (!isGameRunning || IsDetailsPanelActive || Time.timeScale == 0f)
        {
            return;
        }

        bool isCorrect = (answeredMatch == isCurrentMatch);

        if (isCorrect)
        {
            score += 10;
            UpdateScoreUI();
            PlayFeedbackSFX(successClip);
        }
        else
        {
            score = Mathf.Max(0, score - 5);
            lives--;
            UpdateScoreUI();
            UpdateLivesUI();
            PlayFeedbackSFX(failureClip);

            if (lives <= 0)
            {
                EndGame();
                return;
            }
        }

        ShowNextWordAndPlaySound();
    }

    private void PlayFeedbackSFX(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {lives}";
        }
    }

    private void EndGame()
    {
        if (!isGameRunning)
        {
            return;
        }

        isGameRunning = false;

        if (matchButton != null) matchButton.interactable = false;
        if (noMatchButton != null) noMatchButton.interactable = false;

        if (ttsCoroutine != null)
        {
            StopCoroutine(ttsCoroutine);
        }
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        string minigameSceneName = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameSceneName, score);

        if (bestScoreStore != null)
        {
            bestScoreStore.ShowStats(score, bestScore);
        }
    }
}