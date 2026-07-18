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
    [SerializeField] private Sprite cellSprite;

    [Header("Disruptor Object Interaction")]
    // Assign the object you want to monitor here in the Inspector
    [SerializeField] private GameObject triggerObject;

    [Header("Additional Audio")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip roundSuccessSFX;
    [SerializeField] private AudioClip roundFailureSFX;

    [Header("Colors (Rich/Harmonious Slate/Cyan/Green/Red)")]
    private readonly Color normalColor = new Color(0.12f, 0.16f, 0.23f, 1f);
    private readonly Color litColor = new Color(0.06f, 0.71f, 0.85f, 1f);
    private readonly Color correctColor = new Color(0.18f, 0.8f, 0.44f, 1f);
    private readonly Color incorrectColor = new Color(0.9f, 0.22f, 0.27f, 1f);

    private readonly List<int> sequence = new List<int>();
    private readonly Image[] cellImages = new Image[16];
    private readonly Button[] cellButtons = new Button[16];
    private readonly AudioClip[] cellBeeps = new AudioClip[16];
    private AudioClip errorBeep;

    private int score;
    private int k;
    private int userStepIndex;
    private bool isGameRunning;
    private bool isInputEnabled;
    private bool isGridHiddenByObject; // Tracks the visibility override state

    private void Awake()
    {
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
        score = 0;
        k = 1;
        isGameRunning = true;
        isInputEnabled = false;
        isGridHiddenByObject = false;

        UpdateScoreUI();
        SetupGrid();
        GenerateAudioClips();

        StartNextRound();
    }

    private void Update()
    {
        // Continuously check the target object's status while the minigame runs
        if (!isGameRunning || triggerObject == null || gridContainer == null) return;

        bool isObjectActive = triggerObject.activeInHierarchy;

        // If the object becomes active and the grid is still visible, hide it instantly
        if (isObjectActive && !isGridHiddenByObject)
        {
            isGridHiddenByObject = true;
            gridContainer.gameObject.SetActive(false);
        }
        // If the object becomes inactive and the grid is hidden, restore it instantly
        else if (!isObjectActive && isGridHiddenByObject)
        {
            isGridHiddenByObject = false;
            gridContainer.gameObject.SetActive(true);
        }
    }

    private void SetupGrid()
    {
        if (gridContainer == null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            gridContainer.SetParent(canvas.transform, false);
            gridContainer.SetAsFirstSibling();
        }
        gridContainer.gameObject.layer = 5;

        Canvas gridCanvas = gridContainer.gameObject.GetComponent<Canvas>();
        if (gridCanvas == null)
        {
            gridCanvas = gridContainer.gameObject.AddComponent<Canvas>();
        }
        gridCanvas.overrideSorting = true;
        gridCanvas.sortingOrder = -10;

        GraphicRaycaster raycaster = gridContainer.gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gridContainer.gameObject.AddComponent<GraphicRaycaster>();
        }

        HorizontalLayoutGroup existingLayout = gridContainer.GetComponent<HorizontalLayoutGroup>();
        if (existingLayout != null)
        {
            DestroyImmediate(existingLayout);
        }

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

        for (int i = 0; i < 16; i++)
        {
            GameObject cellObj = new GameObject($"Cell_{i}", typeof(RectTransform));
            cellObj.transform.SetParent(gridContainer, false);
            cellObj.layer = 5;

            Image cellImg = cellObj.AddComponent<Image>();
            cellImg.sprite = cellSprite;
            cellImg.type = Image.Type.Sliced;
            cellImg.color = normalColor;
            cellImages[i] = cellImg;

            Button cellBtn = cellObj.AddComponent<Button>();
            Navigation nav = new Navigation { mode = Navigation.Mode.None };
            cellBtn.navigation = nav;
            cellButtons[i] = cellBtn;

            int cellIndex = i;
            cellBtn.onClick.AddListener(() => OnCellClicked(cellIndex));
        }
    }

    private void GenerateAudioClips()
    {
        float[] pitches = new float[]
        {
            261.63f, 293.66f, 329.63f, 349.23f,
            392.00f, 440.00f, 493.88f, 523.25f,
            587.33f, 659.25f, 698.46f, 783.99f,
            880.00f, 987.77f, 1046.50f, 1174.66f
        };

        for (int i = 0; i < 16; i++)
        {
            cellBeeps[i] = CreateBeepClip(pitches[i], 0.35f);
        }

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
            // Smart Pause: If the overlay object is currently active, wait here 
            // so flashes don't play invisibly in the background.
            while (isGridHiddenByObject)
            {
                yield return null;
            }

            int cellIndex = sequence[i];

            if (audioSource != null && cellBeeps[cellIndex] != null)
            {
                audioSource.PlayOneShot(cellBeeps[cellIndex]);
            }

            StartCoroutine(FlashCellCoroutine(cellIndex, litColor, 0.4f));
            yield return new WaitForSeconds(0.45f);
        }

        // Final sanity check before handing control back to the player
        while (isGridHiddenByObject)
        {
            yield return null;
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
        // Added 'isGridHiddenByObject' check to prevent unexpected clicks if raycasts leak through
        if (!isGameRunning || !isInputEnabled || isGridHiddenByObject) return;

        if (index == sequence[userStepIndex])
        {
            if (audioSource != null && cellBeeps[index] != null)
            {
                audioSource.PlayOneShot(cellBeeps[index]);
            }

            StartCoroutine(FlashCellCoroutine(index, correctColor, 0.25f));
            userStepIndex++;

            if (userStepIndex == sequence.Count)
            {
                isInputEnabled = false;

                if (sfxAudioSource != null && roundSuccessSFX != null)
                {
                    sfxAudioSource.PlayOneShot(roundSuccessSFX);
                }

                score += 10;
                k++;
                UpdateScoreUI();

                Invoke(nameof(StartNextRound), 0.8f);
            }
        }
        else
        {
            isInputEnabled = false;

            if (sfxAudioSource != null && roundFailureSFX != null)
            {
                sfxAudioSource.PlayOneShot(roundFailureSFX);
            }
            else if (audioSource != null && errorBeep != null)
            {
                audioSource.PlayOneShot(errorBeep);
            }

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
        StopAllCoroutines();

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

    }
}