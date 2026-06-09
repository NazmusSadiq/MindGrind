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
    [SerializeField] private GameObject[] topBoxes;
    [SerializeField] private GameObject[] middleBoxes;
    [SerializeField] private GameObject[] bottomBoxes;
    [SerializeField] private GameObject[] answerBoxes;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Dictionary")]
    [SerializeField] private TextAsset selectionDictionaryFile;
    [SerializeField] private TextAsset wordDictionaryFile;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int startingColumnCount = 4;
    [SerializeField] private int maxColumnCount = 30;
    [SerializeField] private int lettersPerColumn = 3;
    [SerializeField] private int wordsPerPuzzle = 3;
    [SerializeField] private Color activeSquareColor = new Color(1f, 0.9f, 0.35f);

    private readonly Dictionary<int, List<string>> wordsByLength = new Dictionary<int, List<string>>();
    private readonly Dictionary<int, List<string>> selectionWordsByLength = new Dictionary<int, List<string>>();
    private readonly HashSet<string> allDictionaryWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> currentValidWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly List<char[]> currentColumnOptions = new List<char[]>();

    private TMP_Text[] topTexts;
    private Image[] topImages;
    private Color[] defaultTopColors;
    private TMP_Text[] middleTexts;
    private Image[] middleImages;
    private Color[] defaultMiddleColors;
    private TMP_Text[] bottomTexts;
    private Image[] bottomImages;
    private Color[] defaultBottomColors;
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

        SetupSkipButton();

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

    private void SetupSkipButton()
    {
        if (skipButton == null)
        {
            Debug.LogWarning("ColumnWordMinigame has no skip button assigned. The skip feature will be unavailable.", this);
            return;
        }

        skipButton.onClick.RemoveListener(SkipFormation);
        skipButton.onClick.AddListener(SkipFormation);
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
        selectionWordsByLength.Clear();
        allDictionaryWords.Clear();

        if (selectionDictionaryFile == null)
        {
            Debug.LogError("ColumnWordMinigame is missing the selection dictionary file.", this);
            return;
        }

        if (wordDictionaryFile == null)
        {
            Debug.LogError("ColumnWordMinigame is missing the dictionary file.", this);
            return;
        }

        LoadWordsIntoDictionary(selectionDictionaryFile, selectionWordsByLength, null);
        LoadWordsIntoDictionary(wordDictionaryFile, wordsByLength, allDictionaryWords);
    }

    private bool HasValidSetup()
    {
        bool hasReferences = topBoxes != null
            && middleBoxes != null
            && bottomBoxes != null
            && answerBoxes != null
            && scoreText != null
            && timeRemainingText != null
            && bestScoreStore != null
            && selectionDictionaryFile != null
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

        int availableBoxCount = Mathf.Min(
            Mathf.Min(topBoxes.Length, middleBoxes.Length),
            Mathf.Min(bottomBoxes.Length, answerBoxes.Length));
        if (availableBoxCount < 1)
        {
            Debug.LogError("ColumnWordMinigame needs top, middle, bottom, and answer boxes assigned.", this);
            return false;
        }

        runtimeMaxColumnCount = Mathf.Min(maxColumnCount, availableBoxCount);
        runtimeStartingColumnCount = Mathf.Clamp(startingColumnCount, 1, runtimeMaxColumnCount);

        if (wordsByLength.Count == 0)
        {
            Debug.LogError("ColumnWordMinigame dictionary is empty after loading.", this);
            return false;
        }

        if (selectionWordsByLength.Count == 0)
        {
            Debug.LogError("ColumnWordMinigame selection dictionary is empty after loading.", this);
            return false;
        }

        topTexts = new TMP_Text[topBoxes.Length];
        topImages = new Image[topBoxes.Length];
        defaultTopColors = new Color[topBoxes.Length];
        middleTexts = new TMP_Text[middleBoxes.Length];
        middleImages = new Image[middleBoxes.Length];
        defaultMiddleColors = new Color[middleBoxes.Length];
        bottomTexts = new TMP_Text[bottomBoxes.Length];
        bottomImages = new Image[bottomBoxes.Length];
        defaultBottomColors = new Color[bottomBoxes.Length];
        answerTexts = new TMP_Text[answerBoxes.Length];
        answerImages = new Image[answerBoxes.Length];
        defaultAnswerColors = new Color[answerBoxes.Length];

        if (!CacheRow(topBoxes, topTexts, topImages, defaultTopColors, "top"))
        {
            return false;
        }

        if (!CacheRow(middleBoxes, middleTexts, middleImages, defaultMiddleColors, "middle"))
        {
            return false;
        }

        if (!CacheRow(bottomBoxes, bottomTexts, bottomImages, defaultBottomColors, "bottom"))
        {
            return false;
        }

        if (!CacheRow(answerBoxes, answerTexts, answerImages, defaultAnswerColors, "answer"))
        {
            return false;
        }

        return true;
    }

    private bool BuildRound(int columnCount)
    {
        if (!selectionWordsByLength.TryGetValue(columnCount, out List<string> possibleWords))
        {
            return false;
        }

        if (possibleWords.Count < wordsPerPuzzle)
        {
            return false;
        }

        currentColumnCount = columnCount;

        currentColumnOptions.Clear();
        currentValidWords.Clear();

        List<string> selectedWords = new List<string>();

        List<string> candidates = new List<string>(possibleWords);

        Shuffle(candidates);

        for (int i = 0; i < wordsPerPuzzle && i < candidates.Count; i++)
        {
            selectedWords.Add(candidates[i]);
            currentValidWords.Add(candidates[i]);
        }

        for (int column = 0; column < columnCount; column++)
        {
            List<char> columnLetters = new List<char>();

            for (int wordIndex = 0; wordIndex < selectedWords.Count; wordIndex++)
            {
                columnLetters.Add(selectedWords[wordIndex][column]);
            }

            while (columnLetters.Count < lettersPerColumn)
            {
                char randomLetter = GetRandomLetter();

                if (!ContainsLetter(columnLetters, randomLetter))
                {
                    columnLetters.Add(randomLetter);
                }
            }

            Shuffle(columnLetters);

            currentColumnOptions.Add(columnLetters.ToArray());
        }

        enteredLetters = new char[columnCount];

        RefreshRoundVisuals();
        SetActiveColumn(0);

        Debug.Log("Puzzle words:");

        foreach (string word in selectedWords)
        {
            Debug.Log(word);
        }

        return true;
    }

    private void RefreshRoundVisuals()
    {
        for (int i = 0; i < topBoxes.Length; i++)
        {
            bool shouldShow = i < currentColumnCount;
            topBoxes[i].SetActive(shouldShow);
            middleBoxes[i].SetActive(shouldShow);
            bottomBoxes[i].SetActive(shouldShow);

            if (!shouldShow)
            {
                continue;
            }

            topTexts[i].text = currentColumnOptions[i][0].ToString();
            middleTexts[i].text = currentColumnOptions[i][1].ToString();
            bottomTexts[i].text = currentColumnOptions[i][2].ToString();
            topImages[i].color = defaultTopColors[i];
            middleImages[i].color = defaultMiddleColors[i];
            bottomImages[i].color = defaultBottomColors[i];
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
        if (!allDictionaryWords.Contains(guessedWord))
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

    public void SkipFormation()
    {
        if (!isGameRunning)
        {
            return;
        }

        ShowTemporaryMessage("Skipped", Color.yellow);

        if (!BuildRound(currentColumnCount))
        {
            EndGame();
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

        for (int i = 0; i < topImages.Length; i++)
        {
            if (topImages[i] == null || middleImages[i] == null || bottomImages[i] == null)
            {
                continue;
            }

            Color color = i == activeIndex ? activeSquareColor : defaultTopColors[i];
            topImages[i].color = color;
            middleImages[i].color = i == activeIndex ? activeSquareColor : defaultMiddleColors[i];
            bottomImages[i].color = i == activeIndex ? activeSquareColor : defaultBottomColors[i];
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
            if (selectionWordsByLength.TryGetValue(count, out List<string> words) && words.Count > 0)
            {
                return count;
            }
        }

        return -1;
    }

    private void UpdateScoreUI()
    {
        scoreText.text = $"{score}";
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
        return char.ToLowerInvariant(Alphabet[UnityEngine.Random.Range(0, Alphabet.Length)]);
    }

    private void LoadWordsIntoDictionary(TextAsset sourceFile, Dictionary<int, List<string>> targetDictionary, HashSet<string> uniqueWords)
    {
        string[] lines = sourceFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string word = NormalizeWord(lines[i]);
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            if (uniqueWords != null && !uniqueWords.Add(word))
            {
                continue;
            }

            if (!targetDictionary.TryGetValue(word.Length, out List<string> wordList))
            {
                wordList = new List<string>();
                targetDictionary[word.Length] = wordList;
            }

            wordList.Add(word);
        }
    }

    private bool CacheRow(GameObject[] boxes, TMP_Text[] texts, Image[] images, Color[] defaultColors, string rowName)
    {
        if (boxes == null)
        {
            Debug.LogError($"ColumnWordMinigame is missing the {rowName} row.", this);
            return false;
        }

        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] == null)
            {
                Debug.LogError($"Every {rowName} box must be assigned in ColumnWordMinigame.", this);
                return false;
            }

            texts[i] = boxes[i].GetComponentInChildren<TMP_Text>(true);
            images[i] = boxes[i].GetComponent<Image>();

            if (images[i] == null)
            {
                images[i] = boxes[i].GetComponentInChildren<Image>(true);
            }

            if (texts[i] == null || images[i] == null)
            {
                Debug.LogError($"Each {rowName} box needs an Image and a child TMP_Text.", boxes[i]);
                return false;
            }

            defaultColors[i] = images[i].color;
            boxes[i].SetActive(false);
        }

        return true;
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
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
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

        if (topBoxes != null)
        {
            for (int i = 0; i < topBoxes.Length; i++)
            {
                if (topBoxes[i] != null)
                {
                    topBoxes[i].SetActive(false);
                }
            }
        }

        if (middleBoxes != null)
        {
            for (int i = 0; i < middleBoxes.Length; i++)
            {
                if (middleBoxes[i] != null)
                {
                    middleBoxes[i].SetActive(false);
                }
            }
        }

        if (bottomBoxes != null)
        {
            for (int i = 0; i < bottomBoxes.Length; i++)
            {
                if (bottomBoxes[i] != null)
                {
                    bottomBoxes[i].SetActive(false);
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

    private void OnDestroy()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipFormation);
        }
    }
}