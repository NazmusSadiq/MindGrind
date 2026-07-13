using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class OddOneOut : MonoBehaviour
{
    #region Types

    public enum CriteriaType
    {
        Shape,
        Background,
        ObjectColor,
        Count
    }

    public enum ShapeType
    {
        Circle,
        Triangle,
        Square,
        Pentagon
    }

    [System.Serializable]
    public struct OptionData
    {
        public ShapeType shape;
        public int backgroundIndex;
        public int objectColorIndex;
        public int count;
        public bool isCorrect;
    }

    #endregion

    #region UI References

    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    #endregion

    #region Option Cards

    [Header("Option Cards")]
    // Changed array allocation size from 4 to 6
    [SerializeField] private OptionCard[] optionCards = new OptionCard[6];

    #endregion

    #region Gameplay

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int scorePerCorrect = 10;
    [SerializeField] private int scorePenalty = 10;

    private float timeRemaining;
    private bool gameRunning;
    private int score;
    private int roundNumber;

    #endregion

    #region Round Data

    // Changed array allocation size from 4 to 6
    private OptionData[] currentOptions = new OptionData[6];
    private CriteriaType targetCriteria;
    private int correctOption;

    #endregion

    #region Unity

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
        score = 0;
        roundNumber = 1;
        timeRemaining = gameDuration;
        gameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();

        statusText.text = "Find & Click The Odd One Out.";

        RegisterOptionCallbacks();
        GenerateRound();
    }

    private void Update()
    {
        if (!gameRunning)
            return;

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    #endregion

    #region Initialization

    private bool HasValidSetup()
    {
        if (scoreText == null || timerText == null || statusText == null || bestScoreStore == null)
        {
            Debug.LogError("Missing UI references.");
            return false;
        }

        // Changed validation check from 4 to 6
        if (optionCards == null || optionCards.Length != 6)
        {
            Debug.LogError("Assign six option cards.");
            return false;
        }

        for (int i = 0; i < optionCards.Length; i++)
        {
            if (optionCards[i] == null)
            {
                Debug.LogError($"Option Card {i + 1} is missing.");
                return false;
            }
        }

        return true;
    }

    private void RegisterOptionCallbacks()
    {
        for (int i = 0; i < optionCards.Length; i++)
        {
            int index = i;
            optionCards[index].Initialize(index, OnOptionSelected);
        }
    }

    #endregion

    #region Generation Helpers

    private int RandomVariant()
    {
        return Random.Range(0, 4);
    }

    private int RandomVariantExcept(int excluded)
    {
        int value;
        do
        {
            value = Random.Range(0, 4);
        }
        while (value == excluded);

        return value;
    }

    private void Shuffle<T>(T[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int j = Random.Range(i, array.Length);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    /// <summary>
    /// Generates 6 values ensuring that no single variant appears exactly once.
    /// This prevents accidental secondary "odd ones out" in non-target criteria.
    /// </summary>
    private int[] GenerateBalancedValues()
    {
        int pattern = Random.Range(0, 3);
        int[] result = new int[6];

        // Pattern 0: Three pairs (e.g., A A B B C C)
        if (pattern == 0)
        {
            int a = RandomVariant();
            int b = RandomVariantExcept(a);
            int c;
            do
            {
                c = RandomVariant();
            } while (c == a || c == b);

            result[0] = a; result[1] = a;
            result[2] = b; result[3] = b;
            result[4] = c; result[5] = c;
        }
        // Pattern 1: Two triplets (e.g., A A A B B B)
        else if (pattern == 1)
        {
            int a = RandomVariant();
            int b = RandomVariantExcept(a);

            result[0] = a; result[1] = a; result[2] = a;
            result[3] = b; result[4] = b; result[5] = b;
        }
        // Pattern 2: Four of one, two of another (e.g., A A A A B B)
        else
        {
            int a = RandomVariant();
            int b = RandomVariantExcept(a);

            result[0] = a; result[1] = a; result[2] = a; result[3] = a;
            result[4] = b; result[5] = b;
        }

        Shuffle(result);
        return result;
    }

    #endregion

    #region Round

    private void GenerateRound()
    {
        foreach (OptionCard card in optionCards)
        {
            card.SetInteractable(true);
        }

        targetCriteria = (CriteriaType)Random.Range(0, 4);
        correctOption = Random.Range(0, 6); // Pick randomly from 6 choices now

        for (int i = 0; i < 6; i++)
        {
            currentOptions[i].isCorrect = false;
        }
        currentOptions[correctOption].isCorrect = true;

        //-----------------------------
        // SHAPE
        //-----------------------------
        if (targetCriteria == CriteriaType.Shape)
        {
            int common = RandomVariant();
            int odd = RandomVariantExcept(common);

            for (int i = 0; i < 6; i++)
                currentOptions[i].shape = (ShapeType)common;

            currentOptions[correctOption].shape = (ShapeType)odd;
        }
        else
        {
            int[] values = GenerateBalancedValues();
            for (int i = 0; i < 6; i++)
                currentOptions[i].shape = (ShapeType)values[i];
        }

        //-----------------------------
        // BACKGROUND
        //-----------------------------
        if (targetCriteria == CriteriaType.Background)
        {
            int common = RandomVariant();
            int odd = RandomVariantExcept(common);

            for (int i = 0; i < 6; i++)
                currentOptions[i].backgroundIndex = common;

            currentOptions[correctOption].backgroundIndex = odd;
        }
        else
        {
            int[] values = GenerateBalancedValues();
            for (int i = 0; i < 6; i++)
                currentOptions[i].backgroundIndex = values[i];
        }

        //-----------------------------
        // OBJECT COLOR
        //-----------------------------
        if (targetCriteria == CriteriaType.ObjectColor)
        {
            int common = RandomVariant();
            int odd = RandomVariantExcept(common);

            for (int i = 0; i < 6; i++)
                currentOptions[i].objectColorIndex = common;

            currentOptions[correctOption].objectColorIndex = odd;
        }
        else
        {
            int[] values = GenerateBalancedValues();
            for (int i = 0; i < 6; i++)
                currentOptions[i].objectColorIndex = values[i];
        }

        //-----------------------------
        // COUNT
        //-----------------------------
        if (targetCriteria == CriteriaType.Count)
        {
            int common = Random.Range(1, 5);
            int odd;
            do
            {
                odd = Random.Range(1, 5);
            }
            while (odd == common);

            for (int i = 0; i < 6; i++)
                currentOptions[i].count = common;

            currentOptions[correctOption].count = odd;
        }
        else
        {
            int[] values = GenerateBalancedValues();
            for (int i = 0; i < 6; i++)
                currentOptions[i].count = values[i] + 1;
        }

        //-----------------------------
        // Send to Cards
        //-----------------------------
        for (int i = 0; i < 6; i++)
        {
            optionCards[i].SetData(currentOptions[i]);
            optionCards[i].SetInteractable(true);
        }

        statusText.text = $"Find & Click The Odd One Out.\n\nRound {roundNumber}";
    }

    private void OnOptionSelected(int selectedIndex)
    {
        if (!gameRunning)
            return;

        foreach (OptionCard card in optionCards)
        {
            card.SetInteractable(false);
        }

        bool isCorrect = currentOptions[selectedIndex].isCorrect;

        if (isCorrect)
        {
            score += scorePerCorrect;
            statusText.text = $"Find & Click The Odd One Out.\n\nCorrect! (+{scorePerCorrect})";
        }
        else
        {
            score -= scorePenalty;
            statusText.text = $"Find & Click The Odd One Out.\n\nWrong! Correct : Option {correctOption + 1}";
        }

        UpdateScoreUI();
        roundNumber++;
        StartCoroutine(StartNextRound());
    }

    private IEnumerator StartNextRound()
    {
        yield return new WaitForSeconds(1.5f);

        if (gameRunning)
            GenerateRound();
    }

    #endregion

    #region UI

    private void UpdateScoreUI()
    {
        scoreText.text = $"Score : {score}";
    }

    private void UpdateTimerUI()
    {
        int sec = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
        timerText.text = $"Time : {sec}";
    }

    #endregion

    #region Gameplay Helpers

    private void AddScore(int amount)
    {
        score += amount;
        if (score < 0)
            score = 0;

        UpdateScoreUI();
    }

    private void ResetRound()
    {
        GenerateRound();
    }

    #endregion

    #region End Game

    private void EndGame()
    {
        if (!gameRunning)
            return;

        gameRunning = false;
        timeRemaining = 0f;
        UpdateTimerUI();

        foreach (OptionCard card in optionCards)
        {
            if (card != null)
                card.SetInteractable(false);
        }

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);

        Debug.Log($"Odd One Out Finished\nScore : {score}\nBest : {bestScore}");
    }

    #endregion
}