using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SearchPasswordFromBoxMinigame : MonoBehaviour
{
    private const float DefaultGameDuration = 60f;
    private static readonly string[] DefaultPasswordWords =
    {
        "backplane",
        "landscape",
        "rainstorm",
        "moonlight",
        "blueprint",
        "headphones",
        "notebook",
        "container",
        "pineapple",
        "firestone",
        "stargazer",
        "goldenrod",
        "storybook",
        "paintwork",
        "stronghold",
        "waterfall",
        "earthbound",
        "snowflake",
        "timetable",
        "sandstorm",
        "lighthouse",
        "starfield",
        "workbench",
        "breakfast",
        "congratulation",
        "infrastructure",
        "communication",
        "transportation",
        "configuration",
        "extraordinary",
        "visualization",
        "investigation",
        "neighborhoods",
        "administrator",
        "interpretation",
        "manufacturing",
        "crossroad",
        "tightrope",
        "whirlpool",
        "overdrive",
        "rainbowed",
        "northgate",
        "sentinel",
        "mountains",
        "transitway",
        "reference",
        "bluewater",
        "snowbound",
        "yardstick",
        "quicksand",
        "paperwork"
    };
    [System.Serializable]
    public class ColorEntry
    {
        public string id;
        public Color color = Color.white;
    }

    [Header("References")]
    [SerializeField] private FallingColorBox fallingBoxPrefab;
    [SerializeField] private Image targetColorImage;
    [SerializeField] private TMP_Text revealedLetterText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeLabelText;
    [SerializeField] private TMP_Text timeValueText;
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Button submitPasswordButton;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private Transform spawnLeftPoint;
    [SerializeField] private Transform spawnRightPoint;
    [SerializeField] private Transform missLine;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Colors")]
    [SerializeField] private ColorEntry[] colors;
    [SerializeField, Range(0f, 1f)] private float targetSpawnChance = 0.35f;

    [Header("Password")]
    [SerializeField] private string[] passwordWords = DefaultPasswordWords;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = DefaultGameDuration;
    [SerializeField] private float spawnInterval = 0.75f;
    [SerializeField] private Vector2 fallSpeedRange = new Vector2(3.5f, 5f);
    [SerializeField] private Vector2 spawnRotationRange = new Vector2(0f, 360f);
    [SerializeField] private float targetSwitchInterval = 7f;
    [SerializeField] private float revealedLetterDuration = 3f;

    private int currentTargetIndex;
    private int score;
    private float timeRemaining;
    private bool isGameRunning;
    private bool isAwaitingPassword;
    private Coroutine spawnRoutine;
    private Coroutine revealRoutine;
    private float targetSwitchTimer;
    private string currentPassword;
    private int currentLetterIndex;
    private string currentTargetLetter;
    private int attemptsRemaining = 3;
    private string[] validPasswordWords;

    private void Awake()
    {
        if (passwordPanel != null)
        {
            passwordPanel.SetActive(false);
        }

        isAwaitingPassword = false;
    }

    private void Start()
    {
        if (!HasValidSetup())
        {
            if (passwordPanel != null)
            {
                passwordPanel.SetActive(false);
            }

            enabled = false;
            return;
        }

        score = 0;
        gameDuration = DefaultGameDuration;
        timeRemaining = DefaultGameDuration;
        isGameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();
        InitializePassword();
        SetTargetForCurrentPhase();
        targetSwitchTimer = targetSwitchInterval;
        spawnRoutine = StartCoroutine(SpawnLoop());
        HideRevealedLetter();
        SetupPasswordPanel();
    }

    private void Update()
    {
        if (!isGameRunning && !isAwaitingPassword)
        {
            return;
        }

        if (isGameRunning)
        {
            HandleMouseInput();
        }

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (isGameRunning)
        {
            targetSwitchTimer -= Time.deltaTime;
            if (targetSwitchTimer <= 0f)
            {
                AdvanceTargetPhase();
                targetSwitchTimer = targetSwitchInterval;
            }
        }

        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        Physics2D.SyncTransforms();

        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(activeCamera.ScreenPointToRay(Mouse.current.position.ReadValue()));
        for (int i = 0; i < hits.Length; i++)
        {
            FallingColorBox fallingBox = hits[i].collider.GetComponent<FallingColorBox>();
            if (fallingBox == null)
            {
                continue;
            }

            fallingBox.HandleClick();
            return;
        }
    }

    private bool HasValidSetup()
    {
        bool hasValidColors = colors != null && colors.Length > 0;
        bool hasSpawnPoints = spawnLeftPoint != null && spawnRightPoint != null;
        bool hasReferences = fallingBoxPrefab != null && targetColorImage != null && missLine != null && bestScoreStore != null;
        bool hasTimeValue = timeValueText != null;
        if (passwordWords == null || passwordWords.Length == 0)
        {
            passwordWords = (string[])DefaultPasswordWords.Clone();
        }

        bool hasPasswords = passwordWords != null && passwordWords.Length > 0;
        bool hasPasswordUi = passwordPanel != null && passwordInputField != null && submitPasswordButton != null;

        if (!hasValidColors || !hasSpawnPoints || !hasReferences || !hasPasswords || !hasTimeValue || !hasPasswordUi)
        {
            Debug.LogError("SearchPasswordFromBoxMinigame is missing required references.", this);
            return false;
        }

        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] == null)
            {
                Debug.LogError("Every color entry needs to be assigned.", this);
                return false;
            }
        }

        int invalidCount = 0;
        System.Collections.Generic.List<string> validWords = new System.Collections.Generic.List<string>(passwordWords.Length);
        for (int i = 0; i < passwordWords.Length; i++)
        {
            string word = passwordWords[i];
            if (string.IsNullOrWhiteSpace(word))
            {
                invalidCount++;
                continue;
            }

            string trimmed = word.Trim().ToLowerInvariant();
            if (trimmed.Length < 8 || trimmed.Length > 15)
            {
                invalidCount++;
                continue;
            }

            validWords.Add(trimmed);
        }

        if (validWords.Count == 0)
        {
            Debug.LogError("Password list has no valid words (8 to 15 letters).", this);
            return false;
        }

        if (invalidCount > 0)
        {
            Debug.LogWarning($"Removed {invalidCount} invalid password words. Use 8 to 15 letters.", this);
        }

        validPasswordWords = validWords.ToArray();

        return true;
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(spawnInterval);

        while (isGameRunning)
        {
            SpawnFallingBox();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnFallingBox()
    {
        int colorIndex = GetRandomSpawnColorIndex();
        float spawnX = Random.Range(spawnLeftPoint.position.x, spawnRightPoint.position.x);
        Vector3 spawnPosition = new Vector3(spawnX, spawnLeftPoint.position.y, 0f);
        float spawnRotationZ = Random.Range(spawnRotationRange.x, spawnRotationRange.y);
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, spawnRotationZ);

        FallingColorBox fallingBox = Instantiate(fallingBoxPrefab, spawnPosition, spawnRotation);
        float fallSpeed = Random.Range(fallSpeedRange.x, fallSpeedRange.y);

        string hiddenLetter = GetSpawnLetter(colorIndex);
        fallingBox.Initialize(this, colorIndex, colors[colorIndex].color, hiddenLetter, fallSpeed, missLine.position.y);
    }

    private int GetRandomSpawnColorIndex()
    {
        if (colors.Length == 1)
        {
            return 0;
        }

        if (Random.value <= targetSpawnChance)
        {
            return currentTargetIndex;
        }

        return Random.Range(0, colors.Length);
    }

    private void SetTargetForCurrentPhase()
    {
        currentTargetIndex = Random.Range(0, colors.Length);
        targetColorImage.color = colors[currentTargetIndex].color;
        currentTargetLetter = currentPassword.Substring(currentLetterIndex, 1);
    }

    private void InitializePassword()
    {
        if (validPasswordWords == null || validPasswordWords.Length == 0)
        {
            currentPassword = string.Empty;
            return;
        }

        int wordIndex = Random.Range(0, validPasswordWords.Length);
        currentPassword = validPasswordWords[wordIndex];
        currentLetterIndex = 0;
    }

    private void AdvanceTargetPhase()
    {
        if (currentLetterIndex < currentPassword.Length - 1)
        {
            currentLetterIndex++;
            SetTargetForCurrentPhase();
        }
    }

    private string GetSpawnLetter(int colorIndex)
    {
        if (colorIndex == currentTargetIndex)
        {
            return currentTargetLetter;
        }

        return string.Empty;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateTimerUI()
    {
        if (timeLabelText != null)
        {
            timeLabelText.text = "TIME :";
        }

        if (timeValueText != null)
        {
            int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timeValueText.text = secondsLeft.ToString();
        }
    }

    public void HandleBoxClicked(FallingColorBox fallingBox)
    {
        if (!isGameRunning || fallingBox == null)
        {
            return;
        }

        if (fallingBox.ColorIndex == currentTargetIndex)
        {
            score += 10;
            ShowRevealedLetter(fallingBox.HiddenLetter);
            if (currentLetterIndex >= currentPassword.Length - 1)
            {
                BeginPasswordEntry();
            }
            else
            {
                currentLetterIndex++;
                SetTargetForCurrentPhase();
                targetSwitchTimer = targetSwitchInterval;
            }
        }
        else
        {
            score -= 5;
        }

        UpdateScoreUI();
        fallingBox.Remove();
    }

    public void HandleBoxMissed(FallingColorBox fallingBox)
    {
        if (!isGameRunning || fallingBox == null)
        {
            return;
        }

        if (fallingBox.ColorIndex == currentTargetIndex)
        {
            score -= 5;
            UpdateScoreUI();
        }

        fallingBox.Remove();
    }

    private void EndGame()
    {
        isAwaitingPassword = false;
        isGameRunning = false;
        timeRemaining = 0f;
        UpdateTimerUI();

        if (passwordPanel != null)
        {
            passwordPanel.SetActive(false);
        }

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
        }

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        FallingColorBox[] remainingBoxes = FindObjectsOfType<FallingColorBox>();
        for (int i = 0; i < remainingBoxes.Length; i++)
        {
            remainingBoxes[i].Remove();
        }

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);

        Debug.Log($"Minigame finished. Current score: {score}, Best score: {bestScore}");
    }

    private void BeginPasswordEntry()
    {
        if (!isGameRunning)
        {
            return;
        }

        isGameRunning = false;
        isAwaitingPassword = true;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        FallingColorBox[] remainingBoxes = FindObjectsOfType<FallingColorBox>();
        for (int i = 0; i < remainingBoxes.Length; i++)
        {
            remainingBoxes[i].Remove();
        }

        if (passwordPanel != null)
        {
            passwordPanel.SetActive(true);
        }

        if (passwordInputField != null)
        {
            passwordInputField.text = string.Empty;
            passwordInputField.ActivateInputField();
        }

        UpdateAttemptsUI();
    }

    private void SetupPasswordPanel()
    {
        if (passwordPanel != null)
        {
            passwordPanel.SetActive(false);
        }

        if (submitPasswordButton != null)
        {
            submitPasswordButton.onClick.RemoveAllListeners();
            submitPasswordButton.onClick.AddListener(SubmitPassword);
        }

        UpdateAttemptsUI();
    }

    private void SubmitPassword()
    {
        if (!isAwaitingPassword || passwordInputField == null)
        {
            return;
        }

        string guess = passwordInputField.text.Trim().ToLowerInvariant();
        if (guess == currentPassword)
        {
            EndGame();
            return;
        }

        attemptsRemaining--;
        UpdateAttemptsUI();

        if (attemptsRemaining <= 0)
        {
            EndGame();
            return;
        }

        passwordInputField.text = string.Empty;
        passwordInputField.ActivateInputField();
    }

    private void UpdateAttemptsUI()
    {
        if (attemptsText == null)
        {
            return;
        }

        attemptsText.text = $"Attempts left: {attemptsRemaining}";
    }

    private void ShowRevealedLetter(string letter)
    {
        if (revealedLetterText == null || string.IsNullOrEmpty(letter))
        {
            return;
        }

        revealedLetterText.text = letter;
        revealedLetterText.enabled = true;

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
        }

        revealRoutine = StartCoroutine(HideRevealedLetterAfterDelay());
    }

    private void HideRevealedLetter()
    {
        if (revealedLetterText == null)
        {
            return;
        }

        revealedLetterText.text = string.Empty;
        revealedLetterText.enabled = false;
    }

    private IEnumerator HideRevealedLetterAfterDelay()
    {
        yield return new WaitForSeconds(revealedLetterDuration);
        HideRevealedLetter();
    }
}
