using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LuckyButtonMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button coin1Button;
    [SerializeField] private Button coin2Button;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;
    [SerializeField] private AudioSource audioSource;

    [Header("Colors (Premium Slate/Gold/Green/Red)")]
    private readonly Color normalColor = new Color(0.09f, 0.09f, 0.15f, 1f);      // Deep Midnight Slate (#171726)
    private readonly Color correctColor = new Color(0.18f, 0.8f, 0.44f, 1f);       // Emerald Green (#2ECC71)
    private readonly Color incorrectColor = new Color(0.9f, 0.22f, 0.27f, 1f);     // Alizarin Red (#E74C3C)
    private readonly Color goldColor = new Color(0.98f, 0.75f, 0.14f, 1f);        // Premium Gold (#FBBF24)

    private int score;
    private int currentRound;
    private const int maxRounds = 30;
    private bool isGameActive;
    private bool isCoin1Lucky;
    private bool isInputEnabled;

    private AudioClip winBeep;
    private AudioClip loseBeep;

    private void Awake()
    {
        // Dynamically find references if not explicitly assigned
        if (coin1Button == null) coin1Button = GameObject.Find("Coin1")?.GetComponent<Button>();
        if (coin2Button == null) coin2Button = GameObject.Find("Coin2")?.GetComponent<Button>();
        if (scoreText == null) scoreText = GameObject.Find("Score_Text")?.GetComponent<TMP_Text>();
        if (instructionText == null) instructionText = GameObject.Find("Instruction_Text")?.GetComponent<TMP_Text>();
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
        currentRound = 1;
        isGameActive = true;
        isInputEnabled = true;

        // Choose starting lucky coin
        isCoin1Lucky = Random.value > 0.5f;

        // Initialize UI components
        UpdateScoreUI();
        UpdateInstructionUI();
        GenerateAudioClips();
        SetupButtons();
    }

    private void Update()
    {
        if (!isGameActive || !isInputEnabled) return;

        // Keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            OnCoinClicked(true);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            OnCoinClicked(false);
        }
    }

    private void SetupButtons()
    {
        // Setup Button 1 (Left / Coin1)
        if (coin1Button != null)
        {
            coin1Button.onClick.RemoveAllListeners();
            coin1Button.onClick.AddListener(() => OnCoinClicked(true));
            
            // Set sleek slate-indigo background
            Image img = coin1Button.GetComponent<Image>();
            if (img != null) img.color = normalColor;

            // Set beautiful gold symbol
            TMP_Text txt = coin1Button.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = "<size=64>✹</size>\n<size=18>Symbol A</size>";
                txt.color = goldColor;
            }
        }

        // Setup Button 2 (Right / Coin2)
        if (coin2Button != null)
        {
            coin2Button.onClick.RemoveAllListeners();
            coin2Button.onClick.AddListener(() => OnCoinClicked(false));

            Image img = coin2Button.GetComponent<Image>();
            if (img != null) img.color = normalColor;

            TMP_Text txt = coin2Button.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = "<size=64>✦</size>\n<size=18>Symbol B</size>";
                txt.color = goldColor;
            }
        }
    }

    private void GenerateAudioClips()
    {
        // Dynamic sound synthesis: C5 (523.25Hz) -> E5 (659.25Hz) double beep for win
        winBeep = CreateWinClip();

        // 130Hz buzzing for lose
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

            // Arpeggio sound: first half C5, second half G5
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

    private void OnCoinClicked(bool isCoin1)
    {
        if (!isGameActive || !isInputEnabled) return;

        isInputEnabled = false;

        // Determine if choice is lucky or unlucky
        bool chosenLucky = (isCoin1 == isCoin1Lucky);

        // Outcomes:
        // Lucky: 80% chance of +10, 20% chance of -5
        // Unlucky: 80% chance of -5, 20% chance of +10
        bool gotReward = false;
        if (chosenLucky)
        {
            gotReward = Random.value < 0.8f;
        }
        else
        {
            gotReward = Random.value < 0.2f;
        }

        int scoreDelta = gotReward ? 10 : -5;
        score = Mathf.Max(0, score + scoreDelta);

        // Feedback UI & Sound
        Button clickedBtn = isCoin1 ? coin1Button : coin2Button;
        Image btnImg = clickedBtn.GetComponent<Image>();
        Color feedbackColor = gotReward ? correctColor : incorrectColor;
        string feedbackText = gotReward ? "+10" : "-5";

        if (audioSource != null)
        {
            audioSource.PlayOneShot(gotReward ? winBeep : loseBeep);
        }

        StartCoroutine(FlashButtonColor(btnImg, feedbackColor));
        ShowFloatingText(feedbackText, clickedBtn.transform.position, feedbackColor);

        UpdateScoreUI();

        // Advance to next round
        StartCoroutine(NextRoundCoroutine());
    }

    private IEnumerator NextRoundCoroutine()
    {
        yield return new WaitForSeconds(0.85f);

        if (currentRound >= maxRounds)
        {
            EndGame();
        }
        else
        {
            // Silent swap at the exact halfway point (after round 15 completes)
            if (currentRound == 15)
            {
                isCoin1Lucky = !isCoin1Lucky;
                Debug.Log("Lucky Symbols: Silently swapped lucky and unlucky roles!");
            }

            currentRound++;
            UpdateInstructionUI();
            isInputEnabled = true;
        }
    }

    private void ShowFloatingText(string text, Vector3 position, Color color)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject textObj = new GameObject("FloatingFeedback", typeof(RectTransform));
        textObj.transform.SetParent(canvas.transform, false);
        textObj.transform.position = position + new Vector3(0f, 50f, 0f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = 42;
        tmp.alignment = TextAlignmentOptions.Center;
        
        if (instructionText != null)
        {
            tmp.font = instructionText.font;
        }

        StartCoroutine(FloatAndFadeCoroutine(textObj, tmp));
    }

    private IEnumerator FloatAndFadeCoroutine(GameObject obj, TextMeshProUGUI tmp)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        Vector3 startPos = rt.anchoredPosition;
        Vector3 targetPos = startPos + new Vector3(0f, 80f, 0f);

        float duration = 0.8f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth ease out
            float progress = Mathf.Sin(t * Mathf.PI * 0.5f);
            
            rt.anchoredPosition = Vector3.Lerp(startPos, targetPos, progress);
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f - t);
            yield return null;
        }

        Destroy(obj);
    }

    private IEnumerator FlashButtonColor(Image buttonImage, Color flashColor)
    {
        if (buttonImage == null) yield break;

        Transform trans = buttonImage.transform;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.08f, 1.08f, 1.08f);

        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float wave = Mathf.Sin(t * Mathf.PI);

            buttonImage.color = Color.Lerp(normalColor, flashColor, wave);
            trans.localScale = Vector3.Lerp(originalScale, targetScale, wave);

            yield return null;
        }

        buttonImage.color = normalColor;
        trans.localScale = originalScale;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateInstructionUI()
    {
        if (instructionText != null)
        {
            instructionText.text = $"Round {currentRound} / {maxRounds}\nPick a symbol!";
        }
    }

    private void EndGame()
    {
        isGameActive = false;
        isInputEnabled = false;

        string sceneName = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(sceneName, score);

        if (bestScoreStore != null)
        {
            bestScoreStore.ShowStats(score, bestScore);
        }

        Debug.Log($"Lucky Symbols minigame finished. Score: {score}, Best Score: {bestScore}");
    }
}
