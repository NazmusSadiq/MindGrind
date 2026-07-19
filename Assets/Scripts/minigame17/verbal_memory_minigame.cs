using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class verbal_memory_minigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private Button newButton;
    [SerializeField] private Button seenButton;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;
    [SerializeField] private TextAsset wordDictionaryFile;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successMusic;
    [SerializeField] private AudioClip failureMusic;

    [Header("Gameplay")]
    [SerializeField, Range(0f, 1f)] private float seenProbability = 0.45f;

    private readonly List<string> allWords = new List<string>();
    private readonly List<string> unusedWords = new List<string>();
    private readonly HashSet<string> seenWords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private string currentWord;
    private bool isCurrentWordSeen;
    private int score;
    private int lives;
    private bool isGameRunning;

    private void Awake()
    {
        if (newButton != null)
        {
            newButton.onClick.RemoveListener(OnNewButtonClicked);
            newButton.onClick.AddListener(OnNewButtonClicked);
        }
        if (seenButton != null)
        {
            seenButton.onClick.RemoveListener(OnSeenButtonClicked);
            seenButton.onClick.AddListener(OnSeenButtonClicked);
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

        //Time.timeScale = 1f;
        score = 0;
        lives = 3;
        isGameRunning = true;

        seenWords.Clear();
        unusedWords.Clear();
        unusedWords.AddRange(allWords);

        UpdateScoreUI();
        UpdateLivesUI();

        ShowNextWord();
    }

    private void Update()
    {
        if (!isGameRunning || Time.timeScale == 0f)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.nKey.wasPressedThisFrame)
            {
                OnNewButtonClicked();
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            {
                OnSeenButtonClicked();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.N))
        {
            OnNewButtonClicked();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.S))
        {
            OnSeenButtonClicked();
        }
#endif
    }

    private void OnDestroy()
    {
        if (newButton != null)
        {
            newButton.onClick.RemoveListener(OnNewButtonClicked);
        }
        if (seenButton != null)
        {
            seenButton.onClick.RemoveListener(OnSeenButtonClicked);
        }
    }

    private bool HasValidSetup()
    {
        bool hasReferences = wordText != null
            && scoreText != null
            && livesText != null
            && newButton != null
            && seenButton != null
            && bestScoreStore != null
            && wordDictionaryFile != null;

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

    private void ShowNextWord()
    {
        if (!isGameRunning)
        {
            return;
        }

        // Gather seen candidates that are not the current word
        List<string> seenCandidates = new List<string>();
        foreach (string w in seenWords)
        {
            if (!w.Equals(currentWord, System.StringComparison.OrdinalIgnoreCase))
            {
                seenCandidates.Add(w);
            }
        }

        bool pickSeen = false;
        if (seenCandidates.Count > 0 && allWords.Count > 0)
        {
            pickSeen = Random.value < seenProbability;
        }

        if (pickSeen)
        {
            currentWord = seenCandidates[Random.Range(0, seenCandidates.Count)];
            isCurrentWordSeen = true;
        }
        else
        {
            if (unusedWords.Count == 0)
            {
                unusedWords.AddRange(allWords);
                seenWords.Clear();
            }

            // Remove currentWord from candidates to prevent consecutive repetition
            List<string> newCandidates = new List<string>(unusedWords);
            newCandidates.RemoveAll(w => w.Equals(currentWord, System.StringComparison.OrdinalIgnoreCase));

            if (newCandidates.Count == 0)
            {
                // Fallback in case of tiny dictionary or extreme circumstances
                newCandidates.AddRange(allWords);
                newCandidates.RemoveAll(w => w.Equals(currentWord, System.StringComparison.OrdinalIgnoreCase));
            }

            string selectedWord = newCandidates[Random.Range(0, newCandidates.Count)];
            unusedWords.RemoveAll(w => w.Equals(selectedWord, System.StringComparison.OrdinalIgnoreCase));
            currentWord = selectedWord;
            isCurrentWordSeen = false;
        }

        if (wordText != null)
        {
            wordText.text = currentWord;
        }
    }

    private void OnNewButtonClicked()
    {
        HandleAnswer(false);
    }

    private void OnSeenButtonClicked()
    {
        HandleAnswer(true);
    }

    private void HandleAnswer(bool answeredSeen)
    {
        if (!isGameRunning)
        {
            return;
        }

        bool isCorrect = (answeredSeen == isCurrentWordSeen);

        if (isCorrect)
        {
            score += 10;
            UpdateScoreUI();
            PlaySound(successMusic);
        }
        else
        {
            lives--;
            UpdateLivesUI();
            PlaySound(failureMusic);
            if (lives <= 0)
            {
                EndGame();
                return;
            }
        }

        seenWords.Add(currentWord);
        ShowNextWord();
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

        if (newButton != null) newButton.interactable = false;
        if (seenButton != null) seenButton.interactable = false;

        string minigameSceneName = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameSceneName, score);

        bestScoreStore.ShowStats(score, bestScore);
    }
}