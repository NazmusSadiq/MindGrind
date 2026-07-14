using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HueHuntMinigame : MonoBehaviour
{
    private const int BombTargetIndex = -1;
    private const float InitialSpawnDelay = 1f;

    [Header("References")]
    [SerializeField] private HueHuntMole mole;
    [SerializeField] private Canvas gameplayCanvas;
    [SerializeField] private RectTransform[] spawnPoints;
    [SerializeField] private Image targetMoleImage;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float spawnInterval = 0.75f;
    [SerializeField] private float moleVisibleDuration = 1f;
    [SerializeField] private Sprite[] moleTypeSprites;
    [SerializeField] private Sprite bombTargetSprite;

    private int score;
    private int currentTargetIndex;
    private float timeRemaining;
    private bool isGameRunning;
    private int lastSpawnPointIndex = -1;
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        mole.Initialize(this);
        mole.ForceHide();

        score = 0;
        timeRemaining = gameDuration;
        isGameRunning = true;

        PickNextTargetType();
        UpdateScoreUI();
        UpdateTimerUI();
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        if (!isGameRunning)
        {
            return;
        }

        HandleMouseInput();

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

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
            HueHuntMole clickedMole = hits[i].collider.GetComponentInParent<HueHuntMole>();
            if (clickedMole == null || !clickedMole.IsRaised)
            {
                continue;
            }

            clickedMole.HandleClick();
            return;
        }
    }

    public void HandleMoleClicked(HueHuntMole mole)
    {
        if (!isGameRunning || mole == null)
        {
            return;
        }

        if (!mole.IsRaised)
        {
            return;
        }

        score += currentTargetIndex == BombTargetIndex ? -5 : mole.TypeIndex == currentTargetIndex ? 10 : -10;
        UpdateScoreUI();
        mole.HideAfterHit();
    }

    private bool HasValidSetup()
    {
        bool hasSpawnPoints = spawnPoints != null && spawnPoints.Length > 0;
        bool hasMoleTypes = moleTypeSprites != null && moleTypeSprites.Length >= 2;
        bool hasReferences = mole != null && gameplayCanvas != null && targetMoleImage != null && bestScoreStore != null && bombTargetSprite != null;

        if (!hasSpawnPoints || !hasMoleTypes || !hasReferences)
        {
            Debug.LogError("HueHuntMinigame is missing required references.", this);
            return false;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError("Every spawn point needs a Transform assigned.", this);
                return false;
            }
        }

        for (int i = 0; i < moleTypeSprites.Length; i++)
        {
            if (moleTypeSprites[i] == null)
            {
                Debug.LogError("Every mole type sprite needs a Sprite assigned.", this);
                return false;
            }
        }

        return true;
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(InitialSpawnDelay);

        while (isGameRunning)
        {
            if (!mole.IsRaised)
            {
                SpawnMoleAtRandomPoint();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnMoleAtRandomPoint()
    {
        int spawnPointIndex = GetNextSpawnPointIndex();
        int moleTypeIndex = Random.Range(0, moleTypeSprites.Length);
        Vector3 spawnWorldPosition = GetSpawnWorldPosition(spawnPoints[spawnPointIndex]);

        PickNextTargetType();
        mole.Show(spawnWorldPosition, moleTypeIndex, moleTypeSprites[moleTypeIndex], moleVisibleDuration);
        lastSpawnPointIndex = spawnPointIndex;
    }

    private int GetNextSpawnPointIndex()
    {
        if (spawnPoints.Length <= 1)
        {
            return 0;
        }

        int spawnPointIndex = Random.Range(0, spawnPoints.Length - 1);
        if (spawnPointIndex >= lastSpawnPointIndex)
        {
            spawnPointIndex++;
        }

        return spawnPointIndex;
    }

    private Vector3 GetSpawnWorldPosition(RectTransform spawnPoint)
    {
        Camera canvasCamera = gameplayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : gameplayCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, spawnPoint.position);

        Camera worldCamera = Camera.main;
        float depth = Mathf.Abs(worldCamera.transform.position.z - mole.transform.position.z);
        Vector3 worldPoint = worldCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        worldPoint.z = mole.transform.position.z;

        return worldPoint;
    }

    private void PickNextTargetType()
    {
        float randomValue = Random.value;

        if (randomValue < 0.4f)
        {
            currentTargetIndex = 0;
            targetMoleImage.sprite = moleTypeSprites[currentTargetIndex];
            return;
        }

        if (randomValue < 0.8f)
        {
            currentTargetIndex = 1;
            targetMoleImage.sprite = moleTypeSprites[currentTargetIndex];
            return;
        }

        if (randomValue <= 1f)
        {
            currentTargetIndex = BombTargetIndex;
            targetMoleImage.sprite = bombTargetSprite;
            return;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    private void UpdateTimerUI()
    {
        if (timeRemainingText != null)
        {
            int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timeRemainingText.text = secondsLeft.ToString();
        }
    }

    private void EndGame()
    {
        isGameRunning = false;
        timeRemaining = 0f;
        UpdateTimerUI();

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        mole.ForceHide();

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
        Debug.Log($"Minigame finished. Current score: {score}, Best score: {bestScore}");
    }
}
