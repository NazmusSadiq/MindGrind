using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class square_gen : MonoBehaviour
{
    private const int RevealedLetterCount = 3;
    private const int MaxWordLength = 10;

    [Header("References")]
    [SerializeField] private GameObject[] squares;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Tooltip("The serialized AudioSource used to play success and failure sound effects.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

    [Header("Dictionary")]
    [SerializeField] private TextAsset wordDictionaryFile;
    private string currentPrefix;

    private readonly Dictionary<string, List<string>> wordGroups =
        new Dictionary<string, List<string>>();

    private readonly List<string> availablePrefixes =
        new List<string>();

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private Color activeSquareColor = new Color(1f, 0.9f, 0.35f);

    private readonly List<string> remainingWords = new List<string>();

    private string target;
    private char[] enteredLetters;
    private TMP_Text[] squareTexts;
    private Image[] squareImages;
    private Color[] defaultSquareColors;
    private int activeIndex = -1;
    private int score;
    private float timeRemaining;
    private bool isGameRunning;
    private Coroutine messageRoutine;
    private Color defaultMessageColor = Color.white;
#if ENABLE_INPUT_SYSTEM
    private Keyboard subscribedKeyboard;
#endif

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        SubscribeKeyboard();
    }

    private void OnDisable()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        if (subscribedKeyboard != null)
        {
            subscribedKeyboard.onTextInput -= HandleTextInput;
            subscribedKeyboard = null;
        }
    }
#endif

    private void Start()
    {
#if !ENABLE_INPUT_SYSTEM
        enabled = false;
        return;
#endif
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

        LoadDictionary();

        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        remainingWords.Clear();
        remainingWords.AddRange(BuildUniqueWordList());

        score = 0;
        timeRemaining = gameDuration;
        isGameRunning = true;

        if (messageText != null)
        {
            defaultMessageColor = messageText.color;
        }

        UpdateScoreUI();
        UpdateTimerUI();
        SetMessage(string.Empty);

        if (!LoadNextWord())
        {
            EndGame();
        }
    }

    private void Update()
    {
        if (!isGameRunning)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        SubscribeKeyboard();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)
        {
            HandleBackspace();
        }

        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            ValidateAnswer();
        }
#endif

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void SubscribeKeyboard()
    {
        if (Keyboard.current == null || Keyboard.current == subscribedKeyboard)
        {
            return;
        }

        if (subscribedKeyboard != null)
        {
            subscribedKeyboard.onTextInput -= HandleTextInput;
        }

        subscribedKeyboard = Keyboard.current;
        subscribedKeyboard.onTextInput += HandleTextInput;
    }

    private void HandleTextInput(char typedCharacter)
    {
        if (!isGameRunning || activeIndex < 0 || typedCharacter == '\n' || typedCharacter == '\r' || !char.IsLetter(typedCharacter))
        {
            return;
        }

        EnterLetter(typedCharacter);
    }
#endif

    private void LoadDictionary()
    {
        wordGroups.Clear();
        availablePrefixes.Clear();

        if (wordDictionaryFile == null)
        {
            return;
        }

        string[] lines = wordDictionaryFile.text.Split('\n');

        string currentPrefix = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim().ToLower();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Length == 3)
            {
                currentPrefix = line;

                if (!wordGroups.ContainsKey(currentPrefix))
                {
                    wordGroups[currentPrefix] = new List<string>();
                    availablePrefixes.Add(currentPrefix);
                }
            }
            else
            {
                if (currentPrefix == null)
                {
                    continue;
                }

                if (line.Length >= 5 && line.Length <= 10)
                {
                    wordGroups[currentPrefix].Add(line);
                }
            }
        }
    }

    private bool HasValidSetup()
    {
        bool hasSquares = squares != null && squares.Length > 0;
        bool hasReferences = scoreText != null && timeRemainingText != null && bestScoreStore != null;

        if (!hasSquares || !hasReferences)
        {
            return false;
        }

        List<string> uniqueWords = BuildUniqueWordList();
        if (uniqueWords.Count == 0)
        {
            return false;
        }

        int longestWordLength = 0;
        for (int i = 0; i < uniqueWords.Count; i++)
        {
            if (uniqueWords[i].Length > longestWordLength)
            {
                longestWordLength = uniqueWords[i].Length;
            }
        }

        if (longestWordLength > squares.Length)
        {
            return false;
        }

        squareTexts = new TMP_Text[squares.Length];
        squareImages = new Image[squares.Length];
        defaultSquareColors = new Color[squares.Length];

        for (int i = 0; i < squares.Length; i++)
        {
            if (squares[i] == null)
            {
                return false;
            }

            squareTexts[i] = squares[i].GetComponentInChildren<TMP_Text>(true);
            squareImages[i] = squares[i].GetComponent<Image>();
            if (squareImages[i] == null)
            {
                squareImages[i] = squares[i].GetComponentInChildren<Image>(true);
            }

            if (squareTexts[i] == null || squareImages[i] == null)
            {
                return false;
            }

            defaultSquareColors[i] = squareImages[i].color;
            squares[i].SetActive(false);
        }

        return true;
    }

    private List<string> BuildUniqueWordList()
    {
        List<string> result = new List<string>();

        if (availablePrefixes.Count == 0)
        {
            return result;
        }

        currentPrefix =
            availablePrefixes[Random.Range(0, availablePrefixes.Count)];

        HashSet<string> uniqueWords =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (string word in wordGroups[currentPrefix])
        {
            if (uniqueWords.Add(word))
            {
                result.Add(word);
            }
        }

        return result;
    }

    private bool LoadNextWord()
    {
        remainingWords.Clear();
        remainingWords.AddRange(BuildUniqueWordList());

        if (remainingWords.Count == 0)
        {
            return false;
        }

        int displayLength = MaxWordLength;

        enteredLetters = new char[displayLength];

        for (int i = 0; i < squares.Length; i++)
        {
            bool shouldShowSquare = i < displayLength;
            squares[i].SetActive(shouldShowSquare);

            if (!shouldShowSquare)
            {
                continue;
            }

            bool isRevealedLetter = i < RevealedLetterCount;

            if (isRevealedLetter)
            {
                enteredLetters[i] = currentPrefix[i];
                squareTexts[i].text = currentPrefix[i].ToString();
            }
            else
            {
                enteredLetters[i] = '\0';
                squareTexts[i].text = string.Empty;
            }

            squareImages[i].color = defaultSquareColors[i];
        }

        SetActiveSquare(RevealedLetterCount);

        return true;
    }

    private void SetActiveSquare(int index)
    {
        activeIndex = enteredLetters != null && index >= RevealedLetterCount && index < enteredLetters.Length && index < MaxWordLength ? index : -1;

        for (int i = 0; i < squareImages.Length; i++)
        {
            if (squareImages[i] == null)
            {
                continue;
            }

            squareImages[i].color = i == activeIndex ? activeSquareColor : defaultSquareColors[i];
        }
    }

    private void EnterLetter(char letter)
    {
        if (activeIndex < 0 || enteredLetters == null || activeIndex >= enteredLetters.Length || activeIndex >= MaxWordLength)
        {
            return;
        }

        enteredLetters[activeIndex] = letter;
        squareTexts[activeIndex].text = letter.ToString();

        int nextIndex = activeIndex + 1;
        SetActiveSquare(nextIndex < enteredLetters.Length ? nextIndex : -1);
    }

    private void HandleBackspace()
    {
        if (!isGameRunning || enteredLetters == null)
        {
            return;
        }

        for (int i = Mathf.Min(enteredLetters.Length, MaxWordLength) - 1; i >= RevealedLetterCount; i--)
        {
            if (enteredLetters[i] == '\0')
            {
                continue;
            }

            enteredLetters[i] = '\0';
            squareTexts[i].text = string.Empty;
            SetActiveSquare(i);
            return;
        }
    }

    private void ResetPlayerInput()
    {
        if (enteredLetters == null)
        {
            return;
        }

        for (int i = RevealedLetterCount; i < enteredLetters.Length; i++)
        {
            enteredLetters[i] = '\0';
            squareTexts[i].text = string.Empty;
        }

        SetActiveSquare(RevealedLetterCount);
    }

    private void ValidateAnswer()
    {
        if (!isGameRunning || enteredLetters == null)
        {
            return;
        }

        string enteredWord =
            new string(enteredLetters)
            .Replace("\0", "")
            .ToLower();

        bool isValid =
            remainingWords.Contains(enteredWord);

        if (!isValid)
        {
            ResetPlayerInput();
            ShowTemporaryMessage("Try again", Color.red);
            PlayFeedbackSFX(failureClip);
            return;
        }

        score += enteredWord.Length;

        UpdateScoreUI();

        ShowTemporaryMessage("Success", Color.green);
        PlayFeedbackSFX(successClip);

        if (!LoadNextWord())
        {
            EndGame();
        }
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
        scoreText.text = $"Score: {score}";
    }

    private void UpdateTimerUI()
    {
        int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
        timeRemainingText.text = secondsLeft.ToString();
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.color = defaultMessageColor;
            messageText.text = message;
        }
    }

    private void ShowTemporaryMessage(string message, Color color)
    {
        if (messageText == null)
        {
            return;
        }

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(ClearMessageAfterDelay(message, color));
    }

    private IEnumerator ClearMessageAfterDelay(string message, Color color)
    {
        messageText.color = color;
        messageText.text = message;
        yield return new WaitForSeconds(2f);
        SetMessage(string.Empty);
        messageRoutine = null;
    }

    public void SkipWord()
    {
        if (!isGameRunning || enteredLetters == null)
        {
            return;
        }

        score = Mathf.Max(0, score - 3);
        UpdateScoreUI();

        ShowTemporaryMessage("Skipped", Color.yellow);

        ResetPlayerInput();

        if (!LoadNextWord())
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        if (!isGameRunning)
        {
            return;
        }

        if (squares != null)
        {
            for (int i = 0; i < squares.Length; i++)
            {
                if (squares[i] != null)
                {
                    squares[i].SetActive(false);
                }
            }
        }

        isGameRunning = false;
        timeRemaining = 0f;
        UpdateTimerUI();
        SetActiveSquare(-1);

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
    }
}