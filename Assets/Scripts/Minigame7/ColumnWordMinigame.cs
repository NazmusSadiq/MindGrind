using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ColumnWordMinigame : MonoBehaviour
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    [Header("References")]
    [SerializeField] private GameObject[] columnBoxes;
    [SerializeField] private GameObject[] answerBoxes;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Dictionary")]
    [SerializeField] private TextAsset wordDictionaryFile;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int startingColumnCount = 4;
    [SerializeField] private int maxColumnCount = 10;
    [SerializeField] private int lettersPerColumn = 3;
    [SerializeField] private Color activeSquareColor = new Color(1f, 0.9f, 0.35f);

    private readonly Dictionary<int, List<string>> wordsByLength = new Dictionary<int, List<string>>();
    private readonly HashSet<string> allDictionaryWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> currentValidWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly List<char[]> currentColumnOptions = new List<char[]>();

    private TMP_Text[] columnTexts;
    private Image[] columnImages;
    private Color[] defaultColumnColors;
    private TMP_Text[] answerTexts;
    private Image[] answerImages;
    private Color[] defaultAnswerColors;
    private char[] enteredLetters;
    private int runtimeStartingColumnCount;
    private int runtimeMaxColumnCount;
    private int currentColumnCount;
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
        Debug.LogError("ColumnWordMinigame requires the Input System package.", this);
        enabled = false;
        return;
#endif

        LoadDictionary();

        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
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

        currentColumnCount = FindNextPlayableColumnCount(runtimeStartingColumnCount);
        if (currentColumnCount < 0 || !BuildRound(currentColumnCount))
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
            SubmitGuess();
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
        if (!isGameRunning || activeIndex < 0 || enteredLetters == null || !char.IsLetter(typedCharacter))
        {
            return;
        }

        if (activeIndex >= currentColumnOptions.Count)
        {
            return;
        }

        char letter = char.ToLowerInvariant(typedCharacter);
        if (!ColumnContainsLetter(activeIndex, letter))
        {
            ShowTemporaryMessage("Wrong letter", Color.red);
            return;
        }

        EnterLetter(letter);
    }
#endif

    private void LoadDictionary()
    {
        wordsByLength.Clear();
        allDictionaryWords.Clear();

        if (wordDictionaryFile == null)
        {
            Debug.LogError("ColumnWordMinigame is missing the dictionary file.", this);
            return;
        }

        string[] lines = wordDictionaryFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string word = NormalizeWord(lines[i]);
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            if (!allDictionaryWords.Add(word))
            {
                continue;
            }

            if (!wordsByLength.TryGetValue(word.Length, out List<string> wordList))
            {
                wordList = new List<string>();
                wordsByLength[word.Length] = wordList;
            }

            wordList.Add(word);
        }
    }

    private bool HasValidSetup()
    {
        bool hasReferences = columnBoxes != null
            && answerBoxes != null
            && scoreText != null
            && timeRemainingText != null
            && bestScoreStore != null
            && wordDictionaryFile != null;

        if (!hasReferences)
        {
            Debug.LogError("ColumnWordMinigame is missing required references.", this);
            return false;
        }

        if (gameDuration <= 0f)
        {
            Debug.LogError("ColumnWordMinigame game duration must be greater than zero.", this);
            return false;
        }

        if (startingColumnCount < 1 || maxColumnCount < 1)
        {
            Debug.LogError("ColumnWordMinigame needs positive column counts.", this);
            return false;
        }

        if (lettersPerColumn < 3)
        {
            Debug.LogError("ColumnWordMinigame needs at least three letters per column.", this);
            return false;
        }

        int availableBoxCount = Mathf.Min(columnBoxes.Length, answerBoxes.Length);
        if (availableBoxCount < 1)
        {
            Debug.LogError("ColumnWordMinigame needs at least one column box and one answer box.", this);
            return false;
        }

        runtimeMaxColumnCount = Mathf.Min(maxColumnCount, availableBoxCount);
        runtimeStartingColumnCount = Mathf.Clamp(startingColumnCount, 1, runtimeMaxColumnCount);

        if (wordsByLength.Count == 0)
        {
            Debug.LogError("ColumnWordMinigame dictionary is empty after loading.", this);
            return false;
        }

        columnTexts = new TMP_Text[columnBoxes.Length];
        columnImages = new Image[columnBoxes.Length];
        defaultColumnColors = new Color[columnBoxes.Length];
        answerTexts = new TMP_Text[answerBoxes.Length];
        answerImages = new Image[answerBoxes.Length];
        defaultAnswerColors = new Color[answerBoxes.Length];

        for (int i = 0; i < columnBoxes.Length; i++)
        {
            if (columnBoxes[i] == null)
            {
                Debug.LogError("Every column box must be assigned in ColumnWordMinigame.", this);
                return false;
            }

            columnTexts[i] = columnBoxes[i].GetComponentInChildren<TMP_Text>(true);
            columnImages[i] = columnBoxes[i].GetComponent<Image>();

            if (columnImages[i] == null)
            {
                columnImages[i] = columnBoxes[i].GetComponentInChildren<Image>(true);
            }

            if (columnTexts[i] == null || columnImages[i] == null)
            {
                Debug.LogError("Each column box needs an Image and a child TMP_Text.", columnBoxes[i]);
                return false;
            }

            defaultColumnColors[i] = columnImages[i].color;
            columnBoxes[i].SetActive(false);
        }

        for (int i = 0; i < answerBoxes.Length; i++)
        {
            if (answerBoxes[i] == null)
            {
                Debug.LogError("Every answer box must be assigned in ColumnWordMinigame.", this);
                return false;
            }

            answerTexts[i] = answerBoxes[i].GetComponentInChildren<TMP_Text>(true);
            answerImages[i] = answerBoxes[i].GetComponent<Image>();

            if (answerImages[i] == null)
            {
                answerImages[i] = answerBoxes[i].GetComponentInChildren<Image>(true);
            }

            if (answerTexts[i] == null || answerImages[i] == null)
            {
                Debug.LogError("Each answer box needs an Image and a child TMP_Text.", answerBoxes[i]);
                return false;
            }

            defaultAnswerColors[i] = answerImages[i].color;
            answerBoxes[i].SetActive(false);
        }

        return true;
    }

    private bool BuildRound(int columnCount)
    {
        if (!wordsByLength.TryGetValue(columnCount, out List<string> possibleWords) || possibleWords.Count == 0)
        {
            return false;
        }

        currentColumnCount = columnCount;
        string targetWord = possibleWords[Random.Range(0, possibleWords.Count)];

        currentColumnOptions.Clear();
        currentValidWords.Clear();

        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            List<char> options = new List<char>(lettersPerColumn)
            {
                targetWord[columnIndex]
            };

            while (options.Count < lettersPerColumn)
            {
                char candidate = GetRandomLetter();
                if (!ContainsLetter(options, candidate))
                {
                    options.Add(candidate);
                }
            }

            Shuffle(options);
            currentColumnOptions.Add(options.ToArray());
        }

        for (int i = 0; i < possibleWords.Count; i++)
        {
            string word = possibleWords[i];
            if (WordFitsColumns(word))
            {
                currentValidWords.Add(word);
            }
        }

        if (currentValidWords.Count == 0)
        {
            currentValidWords.Add(targetWord);
        }

        enteredLetters = new char[columnCount];
        RefreshRoundVisuals();
        SetActiveColumn(0);
        return true;
    }

    private void RefreshRoundVisuals()
    {
        for (int i = 0; i < columnBoxes.Length; i++)
        {
            bool shouldShow = i < currentColumnCount;
            columnBoxes[i].SetActive(shouldShow);

            if (!shouldShow)
            {
                continue;
            }

            columnTexts[i].text = BuildColumnLabel(currentColumnOptions[i]);
            columnImages[i].color = defaultColumnColors[i];
        }

        for (int i = 0; i < answerBoxes.Length; i++)
        {
            bool shouldShow = i < currentColumnCount;
            answerBoxes[i].SetActive(shouldShow);

            if (!shouldShow)
            {
                continue;
            }

            enteredLetters[i] = '\0';
            answerTexts[i].text = string.Empty;
            answerImages[i].color = defaultAnswerColors[i];
        }
    }

    private void EnterLetter(char letter)
    {
        if (enteredLetters == null || activeIndex < 0 || activeIndex >= enteredLetters.Length)
        {
            return;
        }

        enteredLetters[activeIndex] = letter;
        answerTexts[activeIndex].text = letter.ToString();

        int nextIndex = activeIndex + 1;
        if (nextIndex >= currentColumnCount)
        {
            SetActiveColumn(-1);
            SubmitGuess();
            return;
        }

        SetActiveColumn(nextIndex);
    }

    private void HandleBackspace()
    {
        if (!isGameRunning || enteredLetters == null)
        {
            return;
        }

        for (int i = Mathf.Min(currentColumnCount, enteredLetters.Length) - 1; i >= 0; i--)
        {
            if (enteredLetters[i] == '\0')
            {
                continue;
            }

            enteredLetters[i] = '\0';
            answerTexts[i].text = string.Empty;
            SetActiveColumn(i);
            return;
        }

        SetActiveColumn(0);
    }

    private void SubmitGuess()
    {
        if (!isGameRunning || enteredLetters == null)
        {
            return;
        }

        if (!IsGuessComplete())
        {
            ShowTemporaryMessage("Complete the word", Color.yellow);
            SetActiveColumn(FindNextEmptyIndex());
            return;
        }

        string guessedWord = BuildEnteredWord();
        if (!currentValidWords.Contains(guessedWord))
        {
            score -= 5;
            UpdateScoreUI();
            ShowTemporaryMessage("Try again", Color.red);
            ResetCurrentGuess();
            return;
        }

        score += 10;
        UpdateScoreUI();
        ShowTemporaryMessage("Success", Color.green);

        int nextColumnCount = FindNextPlayableColumnCount(currentColumnCount + 1);
        if (nextColumnCount < 0 || !BuildRound(nextColumnCount))
        {
            EndGame();
            return;
        }
    }

    private void ResetCurrentGuess()
    {
        if (enteredLetters == null)
        {
            return;
        }

        for (int i = 0; i < enteredLetters.Length; i++)
        {
            enteredLetters[i] = '\0';
            answerTexts[i].text = string.Empty;
        }

        SetActiveColumn(0);
    }

    private bool IsGuessComplete()
    {
        if (enteredLetters == null)
        {
            return false;
        }

        for (int i = 0; i < enteredLetters.Length; i++)
        {
            if (enteredLetters[i] == '\0')
            {
                return false;
            }
        }

        return true;
    }

    private int FindNextEmptyIndex()
    {
        if (enteredLetters == null)
        {
            return 0;
        }

        for (int i = 0; i < enteredLetters.Length; i++)
        {
            if (enteredLetters[i] == '\0')
            {
                return i;
            }
        }

        return Mathf.Clamp(enteredLetters.Length - 1, 0, enteredLetters.Length - 1);
    }

    private string BuildEnteredWord()
    {
        StringBuilder builder = new StringBuilder(enteredLetters.Length);
        for (int i = 0; i < enteredLetters.Length; i++)
        {
            builder.Append(enteredLetters[i]);
        }

        return builder.ToString();
    }

    private void SetActiveColumn(int index)
    {
        activeIndex = enteredLetters != null && index >= 0 && index < enteredLetters.Length ? index : -1;

        for (int i = 0; i < answerImages.Length; i++)
        {
            if (answerImages[i] == null)
            {
                continue;
            }

            answerImages[i].color = i == activeIndex ? activeSquareColor : defaultAnswerColors[i];
        }

        for (int i = 0; i < columnImages.Length; i++)
        {
            if (columnImages[i] == null)
            {
                continue;
            }

            columnImages[i].color = i == activeIndex ? activeSquareColor : defaultColumnColors[i];
        }
    }

    private bool WordFitsColumns(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length != currentColumnOptions.Count)
        {
            return false;
        }

        for (int i = 0; i < word.Length; i++)
        {
            if (!ColumnContainsLetter(i, word[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool ColumnContainsLetter(int columnIndex, char letter)
    {
        if (columnIndex < 0 || columnIndex >= currentColumnOptions.Count)
        {
            return false;
        }

        char[] options = currentColumnOptions[columnIndex];
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] == letter)
            {
                return true;
            }
        }

        return false;
    }

    private int FindNextPlayableColumnCount(int minimumColumnCount)
    {
        int startCount = Mathf.Max(1, minimumColumnCount);
        for (int count = startCount; count <= runtimeMaxColumnCount; count++)
        {
            if (wordsByLength.TryGetValue(count, out List<string> words) && words.Count > 0)
            {
                return count;
            }
        }

        return -1;
    }

    private string BuildColumnLabel(char[] letters)
    {
        if (letters == null || letters.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(letters.Length * 2);
        for (int i = 0; i < letters.Length; i++)
        {
            builder.Append(char.ToUpperInvariant(letters[i]));
            if (i < letters.Length - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
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
        yield return new WaitForSeconds(1.5f);
        SetMessage(string.Empty);
        messageRoutine = null;
    }

    private string NormalizeWord(string rawWord)
    {
        if (string.IsNullOrWhiteSpace(rawWord))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(rawWord.Length);
        for (int i = 0; i < rawWord.Length; i++)
        {
            if (char.IsLetter(rawWord[i]))
            {
                builder.Append(char.ToLowerInvariant(rawWord[i]));
            }
        }

        return builder.ToString();
    }

    private char GetRandomLetter()
    {
        return char.ToLowerInvariant(Alphabet[Random.Range(0, Alphabet.Length)]);
    }

    private bool ContainsLetter(IList<char> letters, char letter)
    {
        for (int i = 0; i < letters.Count; i++)
        {
            if (letters[i] == letter)
            {
                return true;
            }
        }

        return false;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private void EndGame()
    {
        if (!isGameRunning)
        {
            return;
        }

        if (columnBoxes != null)
        {
            for (int i = 0; i < columnBoxes.Length; i++)
            {
                if (columnBoxes[i] != null)
                {
                    columnBoxes[i].SetActive(false);
                }
            }
        }

        if (answerBoxes != null)
        {
            for (int i = 0; i < answerBoxes.Length; i++)
            {
                if (answerBoxes[i] != null)
                {
                    answerBoxes[i].SetActive(false);
                }
            }
        }

        isGameRunning = false;
        timeRemaining = 0f;
        UpdateTimerUI();
        SetActiveColumn(-1);

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
        Debug.Log($"Minigame finished. Current score: {score}, Best score: {bestScore}");
    }
}