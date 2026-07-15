using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoodBadMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Sprite[] objectSprites;
    [SerializeField] private Image[] goodPreviewSlots;
    [SerializeField] private Image[] badPreviewSlots;
    [SerializeField] private GoodBadGridObject[] gridObjects;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text goodText;
    [SerializeField] private TMP_Text badText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Gameplay")]
    [SerializeField] private int totalRounds = 5;
    [SerializeField] private float previewDuration = 3f;
    [SerializeField] private float gridDuration = 5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

    private int score;
    private int roundsCompleted;
    private bool isGameRunning;
    private bool isGridPhaseActive;
    private Coroutine roundRoutine;
    private Sprite[] currentRoundGoodSprites;
    private Sprite[] currentRoundBadSprites;
    private Vector3[] startingGridPositions;

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        for (int i = 0; i < gridObjects.Length; i++)
        {
            gridObjects[i].Initialize(this);
        }

        score = 0;
        roundsCompleted = 0;
        isGameRunning = true;
        currentRoundGoodSprites = new Sprite[goodPreviewSlots.Length];
        currentRoundBadSprites = new Sprite[badPreviewSlots.Length];
        startingGridPositions = new Vector3[gridObjects.Length];

        CacheStartingGridPositions();

        HidePreviewSlots(goodPreviewSlots);
        HidePreviewSlots(badPreviewSlots);
        ClearGrid();
        UpdateScoreUI();
        roundRoutine = StartCoroutine(RoundLoop());
    }

    private void Update()
    {
        if (!isGameRunning)
        {
            return;
        }

        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (!isGridPhaseActive || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
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
            GoodBadGridObject gridObject = hits[i].collider.GetComponent<GoodBadGridObject>();
            if (gridObject == null)
            {
                continue;
            }

            gridObject.HandleClick();
            return;
        }
    }

    public void HandleGridObjectClicked(GoodBadGridObject gridObject)
    {
        if (!isGameRunning || !isGridPhaseActive || gridObject == null)
        {
            return;
        }

        // If the clicked object is NOT good (i.e., bad), we correctly identified the bad object! (Success)
        if (!gridObject.IsGood)
        {
            score += 10;
            PlaySound(successClip);
        }
        else
        {
            // If the clicked object is Good, that's an incorrect penalty hit! (Failure)
            score -= 5;
            if (score < 0)
            {
                score = 0;
            }
            PlaySound(failureClip);
        }

        UpdateScoreUI();
        gridObject.Clear();
    }

    private bool HasValidSetup()
    {
        bool hasObjectSprites = objectSprites != null && objectSprites.Length == 8;
        bool hasGoodPreviewSlots = goodPreviewSlots != null && goodPreviewSlots.Length == 4;
        bool hasBadPreviewSlots = badPreviewSlots != null && badPreviewSlots.Length == 4;
        bool hasGridObjects = gridObjects != null && gridObjects.Length == 8;
        bool hasReferences = scoreText != null && bestScoreStore != null;

        if (!hasObjectSprites || !hasGoodPreviewSlots || !hasBadPreviewSlots || !hasGridObjects || !hasReferences)
        {
            Debug.LogError("GoodBadMinigame is missing required references.", this);
            return false;
        }

        for (int i = 0; i < objectSprites.Length; i++)
        {
            if (objectSprites[i] == null)
            {
                Debug.LogError("All object sprites must be assigned.", this);
                return false;
            }
        }

        for (int i = 0; i < goodPreviewSlots.Length; i++)
        {
            if (goodPreviewSlots[i] == null || badPreviewSlots[i] == null)
            {
                Debug.LogError("All preview slots must be assigned.", this);
                return false;
            }
        }

        for (int i = 0; i < gridObjects.Length; i++)
        {
            if (gridObjects[i] == null)
            {
                Debug.LogError("Every grid object slot needs a GoodBadGridObject assigned.", this);
                return false;
            }
        }

        return true;
    }

    private IEnumerator RoundLoop()
    {
        while (isGameRunning && roundsCompleted < totalRounds)
        {
            PrepareRound();
            ShowRoundPreview();
            yield return RunPhase(previewDuration);

            if (!isGameRunning)
            {
                yield break;
            }

            HidePreviewSlots(goodPreviewSlots);
            HidePreviewSlots(badPreviewSlots);
            ShowGridRound();
            isGridPhaseActive = true;
            yield return RunPhase(gridDuration);
            isGridPhaseActive = false;
            ClearGrid();
            roundsCompleted++;
        }

        if (isGameRunning)
        {
            EndGame();
        }
    }

    private IEnumerator RunPhase(float duration)
    {
        float elapsed = 0f;
        while (isGameRunning && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PrepareRound()
    {
        Sprite[] shuffledObjectSprites = ShuffleCopy(objectSprites);

        for (int i = 0; i < goodPreviewSlots.Length; i++)
        {
            currentRoundGoodSprites[i] = shuffledObjectSprites[i];
            currentRoundBadSprites[i] = shuffledObjectSprites[i + goodPreviewSlots.Length];
        }
    }

    private void ShowRoundPreview()
    {
        for (int i = 0; i < goodPreviewSlots.Length; i++)
        {
            goodPreviewSlots[i].sprite = currentRoundGoodSprites[i];
            goodPreviewSlots[i].enabled = currentRoundGoodSprites[i] != null;
            badPreviewSlots[i].sprite = currentRoundBadSprites[i];
            badPreviewSlots[i].enabled = currentRoundBadSprites[i] != null;
        }
        goodText.enabled = true;
        badText.enabled = true;
    }

    private void ShowGridRound()
    {
        Sprite[] gridSprites = new Sprite[gridObjects.Length];
        bool[] gridGoodFlags = new bool[gridObjects.Length];

        for (int i = 0; i < currentRoundGoodSprites.Length; i++)
        {
            gridSprites[i] = currentRoundGoodSprites[i];
            gridGoodFlags[i] = true;
        }

        for (int i = 0; i < currentRoundBadSprites.Length; i++)
        {
            int gridIndex = i + currentRoundGoodSprites.Length;
            gridSprites[gridIndex] = currentRoundBadSprites[i];
            gridGoodFlags[gridIndex] = false;
        }

        ShuffleGridAssignment(gridSprites, gridGoodFlags);
        ShuffleGridPositions();

        for (int i = 0; i < gridObjects.Length; i++)
        {
            gridObjects[i].Configure(gridSprites[i], gridGoodFlags[i]);
        }
        goodText.enabled = false;
        badText.enabled = false;
    }

    private void ShuffleGridAssignment(Sprite[] sprites, bool[] goodFlags)
    {
        for (int i = sprites.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (sprites[i], sprites[swapIndex]) = (sprites[swapIndex], sprites[i]);
            (goodFlags[i], goodFlags[swapIndex]) = (goodFlags[swapIndex], goodFlags[i]);
        }
    }

    private Sprite[] ShuffleCopy(Sprite[] source)
    {
        return ShuffleCopyInternal(source);
    }

    private void CacheStartingGridPositions()
    {
        for (int i = 0; i < gridObjects.Length; i++)
        {
            startingGridPositions[i] = gridObjects[i].transform.position;
        }
    }

    private void ShuffleGridPositions()
    {
        Vector3[] shuffledPositions = ShuffleCopyInternal(startingGridPositions);

        for (int i = 0; i < gridObjects.Length; i++)
        {
            gridObjects[i].transform.position = shuffledPositions[i];
        }
    }

    private void RestoreGridPositions()
    {
        for (int i = 0; i < gridObjects.Length; i++)
        {
            gridObjects[i].transform.position = startingGridPositions[i];
        }
    }

    private T[] ShuffleCopyInternal<T>(T[] source)
    {
        T[] copy = new T[source.Length];
        source.CopyTo(copy, 0);

        for (int i = copy.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (copy[i], copy[swapIndex]) = (copy[swapIndex], copy[i]);
        }

        return copy;
    }

    private void HidePreviewSlots(Image[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].sprite = null;
            slots[i].enabled = false;
        }
    }

    private void ClearGrid()
    {
        for (int i = 0; i < gridObjects.Length; i++)
        {
            gridObjects[i].Clear();
        }
    }

    private void UpdateScoreUI()
    {
        scoreText.text = score.ToString();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void EndGame()
    {
        isGameRunning = false;
        isGridPhaseActive = false;

        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
        }

        HidePreviewSlots(goodPreviewSlots);
        HidePreviewSlots(badPreviewSlots);
        ClearGrid();
        RestoreGridPositions();
        goodText.enabled = false;
        badText.enabled = false;

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
        Debug.Log($"Minigame finished. Current score: {score}, Best score: {bestScore}");
    }
}