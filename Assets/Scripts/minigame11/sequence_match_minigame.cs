using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class sequence_match_minigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;
    [SerializeField] private AudioSource audioSource;

    [Header("Additional Audio")]
    [SerializeField] private AudioSource sfxAudioSource; // Serialized AudioSource for round results
    [SerializeField] private AudioClip roundSuccessSFX;
    [SerializeField] private AudioClip roundFailureSFX;

    [Header("Colors (Rich/Harmonious Slate/Cyan/Green/Red)")]
    private readonly Color normalColor = new Color(0.12f, 0.16f, 0.23f, 1f);      // Sleek Slate-800 (#1E293B)
    private readonly Color litColor = new Color(0.06f, 0.71f, 0.85f, 1f);         // Glow Cyan (#0EA5E9)
    private readonly Color correctColor = new Color(0.18f, 0.8f, 0.44f, 1f);       // Emerald Green (#2ECC71)
    private readonly Color incorrectColor = new Color(0.9f, 0.22f, 0.27f, 1f);     // Alizarin Red (#E74C3C)

    private readonly List<int> sequence = new List<int>();
    private readonly Image[] cellImages = new Image[16];
    private readonly Button[] cellButtons = new Button[16];
    private readonly AudioClip[] cellBeeps = new AudioClip[16];
    private AudioClip errorBeep;

    private int score;
    private int k; // Sequence length
    private int userStepIndex;
    private bool isGameRunning;
    private bool isInputEnabled;

    private void Awake()
    {
        // Dynamically find references if not explicitly assigned in Unity Editor
        if (scoreText == null) scoreText = GameObject.Find("Score_Value")?.GetComponent<TMP_Text>();
        if (gridContainer == null) gridContainer = GameObject.Find("Container")?.GetComponent<RectTransform>();
        if (bestScoreStore == null) bestScoreStore = FindFirstObjectByType<MinigameBestScoreStore>();
        if (audioSource == null) audioSource = GameObject.Find("Audio Source")?.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        score = 0;
        k = 1;
        isGameRunning = true;
        isInputEnabled = false;

        UpdateScoreUI();
        SetupGrid();
        GenerateAudioClips();

        StartNextRound();
    }

    private void SetupGrid()
    {
        if (gridContainer == null)
        {
            Debug.LogError("Grid container RectTransform is not assigned on sequence_match_minigame.", this);
            return;
        }

        // Reparent gridContainer to Canvas at runtime and place it above background (sibling index 1)
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            gridContainer.SetParent(canvas.transform, false);
            gridContainer.SetSiblingIndex(0);
        }
        gridContainer.gameObject.layer = 5; // UI layer

        // Remove any existing LayoutGroup to ensure we use our clean grid layout
        HorizontalLayoutGroup existingLayout = gridContainer.GetComponent<HorizontalLayoutGroup>();
        if (existingLayout != null)
        {
            DestroyImmediate(existingLayout);
        }

        // Add and configure GridLayoutGroup
        GridLayoutGroup gridLayout = gridContainer.gameObject.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.cellSize = new Vector2(100f, 100f);
        gridLayout.spacing = new Vector2(12f, 12f);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // Try to obtain a standard rounded button sprite from the scene UI to keep aesthetics consistent
        Sprite buttonSprite = null;
        GameObject sampleBtnObj = GameObject.Find("RestartButton");
        if (sampleBtnObj == null) sampleBtnObj = GameObject.Find("Play_Button");
        if (sampleBtnObj == null) sampleBtnObj = GameObject.Find("Resume_Button");
        if (sampleBtnObj != null)
        {
            Image sampleImage = sampleBtnObj.GetComponent<Image>();
            if (sampleImage != null)
            {
                buttonSprite = sampleImage.sprite;
            }
        }

        // Spawn the 4x4 grid (16 cells)
        for (int i = 0; i < 16; i++)
        {
            GameObject cellObj = new GameObject($"Cell_{i}", typeof(RectTransform));
            cellObj.transform.SetParent(gridContainer, false);
            cellObj.layer = 5; // UI layer

            Image cellImg = cellObj.AddComponent<Image>();
            cellImg.sprite = buttonSprite;
            cellImg.type = Image.Type.Sliced;
            cellImg.color = normalColor;
            cellImages[i] = cellImg;

            Button cellBtn = cellObj.AddComponent<Button>();
            // Set navigation to None to prevent keyboard selection highlights
            Navigation nav = new Navigation { mode = Navigation.Mode.None };
            cellBtn.navigation = nav;
            cellButtons[i] = cellBtn;

            int cellIndex = i;
            cellBtn.onClick.AddListener(() => OnCellClicked(cellIndex));
        }
    }

    private void GenerateAudioClips()
    {
        // 16 pitches mapping to a diatonic C Major scale starting from C4 (261.63Hz)
        float[] pitches = new float[]
        {
            261.63f, 293.66f, 329.63f, 349.23f, // C4, D4, E4, F4
            392.00f, 440.00f, 493.88f, 523.25f, // G4, A4, B4, C5
            587.33f, 659.25f, 698.46f, 783.99f, // D5, E5, F5, G5
            880.00f, 987.77f, 1046.50f, 1174.66f // A5, B5, C6, D6
        };

        for (int i = 0; i < 16; i++)
        {
            cellBeeps[i] = CreateBeepClip(pitches[i], 0.35f);
        }

        // Synthesize a low warning buzz for incorrect clicks (110Hz C2/A2-ish buzz)
        errorBeep = CreateBeepClip(110f, 0.5f);
    }

    private AudioClip CreateBeepClip(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Apply linear fade-out to prevent popping sounds at the end
            float fade = 1.0f - ((float)i / sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade * 0.35f;
        }

        AudioClip clip = AudioClip.Create($"Beep_{frequency}", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void StartNextRound()
    {
        if (!isGameRunning) return;

        // Generate a random sequence of length K
        sequence.Clear();
        for (int i = 0; i < k; i++)
        {
            sequence.Add(Random.Range(0, 16));
        }

        StartCoroutine(ShowSequenceCoroutine());
    }

    private IEnumerator ShowSequenceCoroutine()
    {
        isInputEnabled = false;

        yield return new WaitForSeconds(0.6f);

        for (int i = 0; i < sequence.Count; i++)
        {
            int cellIndex = sequence[i];

            // Play the corresponding synthesized beep
            if (audioSource != null && cellBeeps[cellIndex] != null)
            {
                audioSource.PlayOneShot(cellBeeps[cellIndex]);
            }

            // Flash the cell visual with cyan and micro-animation
            StartCoroutine(FlashCellCoroutine(cellIndex, litColor, 0.4f));

            yield return new WaitForSeconds(0.45f);
        }

        userStepIndex = 0;
        isInputEnabled = true;
    }

    private IEnumerator FlashCellCoroutine(int index, Color flashColor, float duration)
    {
        Image img = cellImages[index];
        if (img == null) yield break;

        Transform trans = img.transform;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.1f, 1.1f, 1.1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth sine curve for standard flashing animation
            float wave = Mathf.Sin(t * Mathf.PI);

            img.color = Color.Lerp(normalColor, flashColor, wave);
            trans.localScale = Vector3.Lerp(originalScale, targetScale, wave);

            yield return null;
        }

        img.color = normalColor;
        trans.localScale = originalScale;
    }

    private void OnCellClicked(int index)
    {
        if (!isGameRunning || !isInputEnabled) return;

        // Verify user choice
        if (index == sequence[userStepIndex])
        {
            // Correct click
            if (audioSource != null && cellBeeps[index] != null)
            {
                audioSource.PlayOneShot(cellBeeps[index]);
            }

            StartCoroutine(FlashCellCoroutine(index, correctColor, 0.25f));
            userStepIndex++;

            // Check if full sequence replicated
            if (userStepIndex == sequence.Count)
            {
                isInputEnabled = false;

                // Play custom success sound clip via the serialized audio source
                if (sfxAudioSource != null && roundSuccessSFX != null)
                {
                    sfxAudioSource.PlayOneShot(roundSuccessSFX);
                }

                score += 10;
                k++;
                UpdateScoreUI();

                // Advance to next level after brief delay
                Invoke(nameof(StartNextRound), 0.8f);
            }
        }
        else
        {
            // Incorrect click - Game Over!
            isInputEnabled = false;

            // Play custom failure sound clip via the serialized audio source
            if (sfxAudioSource != null && roundFailureSFX != null)
            {
                sfxAudioSource.PlayOneShot(roundFailureSFX);
            }
            else if (audioSource != null && errorBeep != null)
            {
                // Fallback to standard generated error buzz if serialized clip is missing
                audioSource.PlayOneShot(errorBeep);
            }

            // Flash wrong cell red, and correct one green to help the user learn
            StartCoroutine(FlashCellCoroutine(index, incorrectColor, 0.6f));
            int expectedIndex = sequence[userStepIndex];
            StartCoroutine(FlashCellCoroutine(expectedIndex, correctColor, 0.6f));

            Invoke(nameof(GameOver), 0.8f);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void GameOver()
    {
        if (!isGameRunning) return;

        isGameRunning = false;

        // Stop all active sequence showing or flashing coroutines
        StopAllCoroutines();

        // Destroy the dynamic grid cells
        for (int i = 0; i < 16; i++)
        {
            if (cellImages[i] != null)
            {
                Destroy(cellImages[i].gameObject);
                cellImages[i] = null;
                cellButtons[i] = null;
            }
        }

        string minigameSceneName = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameSceneName, score);

        if (bestScoreStore != null)
        {
            bestScoreStore.ShowStats(score, bestScore);
        }

        Debug.Log($"Sequence Grid Match minigame finished. Final Score: {score}, Best Score: {bestScore}");
    }
}