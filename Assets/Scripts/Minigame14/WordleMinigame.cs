using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WordleMinigame : MonoBehaviour
{
    [System.Serializable]
    public struct WordleRow
    {
        public TMP_Text[] letterTexts;       // 5 text elements for this row
        public Image[] letterBackgrounds;   // 5 background images for this row
    }

    [Header("References")]
    [SerializeField] private TextAsset wordListFile; // Drag your .txt file here
    [SerializeField] private WordleRow[] rows = new WordleRow[6]; // 6 rows of 5 letters each
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;
    [SerializeField] private AudioSource audioSource;

    [Tooltip("The serialized AudioSource used to play letter-reveal success and failure sound effects.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

    [Header("Colors")]
    private readonly Color emptyColor = Color.white;                              // Default White background
    private readonly Color wrongColor = new Color(0.9f, 0.22f, 0.27f, 1f);        // Red (Letter not in word)
    private readonly Color yellowColor = new Color(0.98f, 0.75f, 0.14f, 1f);      // Yellow/Gold (Wrong spot)
    private readonly Color greenColor = new Color(0.18f, 0.8f, 0.44f, 1f);        // Green (Right spot)

    private List<string> wordPool = new List<string>();
    private string targetWord = "";
    private string currentGuess = "";
    private int currentAttempt = 0; // 0 to 5
    private int score = 0;
    private bool isGameActive = false;

    // Track historical point-yielding states for each of the 5 positions
    // 0 = Unmatched, 1 = Yellow found previously, 2 = Green found previously
    private int[] positionScoreStates = new int[5];

    private AudioClip winBeep;
    private AudioClip loseBeep;

    private void Awake()
    {
        if (scoreText == null) scoreText = GameObject.Find("Score_Text")?.GetComponent<TMP_Text>();
        if (instructionText == null) instructionText = GameObject.Find("Instruction_Text")?.GetComponent<TMP_Text>();
        if (bestScoreStore == null) bestScoreStore = FindFirstObjectByType<MinigameBestScoreStore>();
        if (audioSource == null) audioSource = GameObject.Find("Audio Source")?.GetComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = GameObject.Find("SFX Audio Source")?.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = false;
        audioSource.playOnAwake = false;

        // Fallback SFX AudioSource to primary AudioSource if a dedicated one isn't assigned
        if (sfxAudioSource == null)
        {
            sfxAudioSource = audioSource;
        }
        else
        {
            sfxAudioSource.loop = false;
            sfxAudioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        //Time.timeScale = 1f;
        GenerateAudioClips();
        LoadWordPool();
        StartNewGame();

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnTextInputReceived;
        }
    }

    private void OnDestroy()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInputReceived;
        }
    }

    private void Update()
    {
        // FIX: Verify Time.timeScale is not 0 before resolving key queries
        if (!isGameActive || Keyboard.current == null || Time.timeScale == 0f) return;

        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            if (currentGuess.Length > 0)
            {
                currentGuess = currentGuess.Substring(0, currentGuess.Length - 1);
                UpdateRowVisuals();
            }
        }
    }

    private void OnTextInputReceived(char character)
    {
        // FIX: Ignore incoming input buffer values instantly if game layout execution is paused
        if (!isGameActive || Time.timeScale == 0f) return;
        if (character == '\n' || character == '\r' || character == '\b') return;

        if (char.IsLetter(character) && currentGuess.Length < 5)
        {
            currentGuess += char.ToUpper(character);
            UpdateRowVisuals();

            if (currentGuess.Length == 5)
            {
                SubmitGuess();
            }
        }
    }

    private void LoadWordPool()
    {
        if (wordListFile == null)
        {
            wordPool.Add("UNITY");
            return;
        }

        string[] lines = wordListFile.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string trimmed = line.Trim().ToUpper();
            if (trimmed.Length == 5)
            {
                wordPool.Add(trimmed);
            }
        }

        if (wordPool.Count == 0)
        {
            wordPool.Add("UNITY");
        }
    }

    private void StartNewGame()
    {
        score = 0;
        currentAttempt = 0;
        currentGuess = "";
        isGameActive = true;

        // Reset tracking states back to empty/unmatched
        for (int i = 0; i < 5; i++)
        {
            positionScoreStates[i] = 0;
        }

        targetWord = wordPool[Random.Range(0, wordPool.Count)];

        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                if (rows[r].letterTexts[c] != null) rows[r].letterTexts[c].text = "";
                if (rows[r].letterBackgrounds[c] != null) rows[r].letterBackgrounds[c].color = emptyColor;
            }
        }

        UpdateScoreUI();
        UpdateInstructionUI("Type a 5-letter word!");
    }

    private void UpdateRowVisuals()
    {
        if (currentAttempt >= rows.Length) return;

        WordleRow row = rows[currentAttempt];
        for (int i = 0; i < 5; i++)
        {
            if (row.letterTexts[i] != null)
            {
                row.letterTexts[i].text = i < currentGuess.Length ? currentGuess[i].ToString() : "";
            }
        }
    }

    private void SubmitGuess()
    {
        StartCoroutine(RevealRowColors(currentAttempt, currentGuess));
        currentAttempt++;
        currentGuess = "";
    }

    private IEnumerator RevealRowColors(int rowIndex, string guess)
    {
        isGameActive = false;
        WordleRow row = rows[rowIndex];

        bool[] targetMatched = new bool[5];
        Color[] results = new Color[5];

        // Pass 1: Check Greens
        for (int i = 0; i < 5; i++)
        {
            if (guess[i] == targetWord[i])
            {
                results[i] = greenColor;
                targetMatched[i] = true;
            }
            else
            {
                results[i] = wrongColor;
            }
        }

        // Pass 2: Check Yellows
        for (int i = 0; i < 5; i++)
        {
            if (results[i] == greenColor) continue;

            for (int j = 0; j < 5; j++)
            {
                if (!targetMatched[j] && guess[i] == targetWord[j])
                {
                    results[i] = yellowColor;
                    targetMatched[j] = true;
                    break;
                }
            }
        }

        // Apply visuals and calculate career capped point additions
        for (int i = 0; i < 5; i++)
        {
            if (row.letterBackgrounds[i] != null)
            {
                row.letterBackgrounds[i].color = results[i];
            }

            // Play corresponding sound effect based on match result
            if (results[i] == greenColor || results[i] == yellowColor)
            {
                if (sfxAudioSource != null && successClip != null)
                {
                    sfxAudioSource.PlayOneShot(successClip);
                }
            }
            else if (results[i] == wrongColor)
            {
                if (sfxAudioSource != null && failureClip != null)
                {
                    sfxAudioSource.PlayOneShot(failureClip);
                }
            }

            // Strictly capped scoring evaluations:
            if (results[i] == greenColor)
            {
                if (positionScoreStates[i] == 0) // Brand new match discovered directly
                {
                    score += 10;
                    positionScoreStates[i] = 2; // Marked as full career capacity met
                }
                else if (positionScoreStates[i] == 1) // Was yellow previously, now green upgrade
                {
                    score += 5; // Total career contribution becomes exactly 10
                    positionScoreStates[i] = 2;
                }
            }
            else if (results[i] == yellowColor)
            {
                if (positionScoreStates[i] == 0) // Brand new yellow match discovery
                {
                    score += 5;
                    positionScoreStates[i] = 1; // Marked as partial career capacity met
                }
            }

            UpdateScoreUI();

            // FIX: Replaced standard WaitForSeconds with pause-compliant check loop
            float revealElapsed = 0f;
            while (revealElapsed < 0.12f)
            {
                if (Time.timeScale > 0f) revealElapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Evaluate state
        if (guess == targetWord)
        {
            int tryBonus = 140 - ((rowIndex + 1) * 20);
            score += tryBonus;

            UpdateScoreUI();
            UpdateInstructionUI($"Success! Word was: {targetWord}");
            if (audioSource != null) audioSource.PlayOneShot(winBeep);

            // FIX: Replaced standard WaitForSeconds with pause-compliant check loop
            float winElapsed = 0f;
            while (winElapsed < 0.5f)
            {
                if (Time.timeScale > 0f) winElapsed += Time.deltaTime;
                yield return null;
            }
            EndGame();
        }
        else if (currentAttempt >= 6)
        {
            UpdateInstructionUI($"Out of turns! Word was: {targetWord}");
            if (audioSource != null) audioSource.PlayOneShot(loseBeep);

            // FIX: Replaced standard WaitForSeconds with pause-compliant check loop
            float loseElapsed = 0f;
            while (loseElapsed < 0.5f)
            {
                if (Time.timeScale > 0f) loseElapsed += Time.deltaTime;
                yield return null;
            }
            EndGame();
        }
        else
        {
            isGameActive = true;
            UpdateInstructionUI($"Row {currentAttempt + 1} / 6");
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateInstructionUI(string message)
    {
        if (instructionText != null)
        {
            instructionText.text = message;
        }
    }

    private void EndGame()
    {
        isGameActive = false;

        string sceneName = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(sceneName, score);

        if (bestScoreStore != null)
        {
            bestScoreStore.ShowStats(score, bestScore);
        }
    }

    #region Audio Processing
    private void GenerateAudioClips()
    {
        winBeep = CreateWinClip();
        loseBeep = CreateBeepClip(130f, 0.4f, 0.25f);
    }

    private AudioClip CreateWinClip()
    {
        int sampleRate = 44100;
        float duration = 0.35f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float fade = 1.0f - (t / duration);
            float freq = t < (duration * 0.4f) ? 523.25f : 783.99f;
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * fade * 0.3f;
        }

        AudioClip clip = AudioClip.Create("WinBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateBeepClip(float frequency, float duration, float volume)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float fade = 1.0f - (t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade * volume;
        }

        AudioClip clip = AudioClip.Create($"Beep_{frequency}", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}