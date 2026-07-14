using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FishFeedingMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishFeedingTarget[] fishTargets;
    [SerializeField] private Transform topLeftBoundary;
    [SerializeField] private Transform bottomRightBoundary;
    [SerializeField] private Image foodReadyImage;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 45f;
    [SerializeField] private float feedCooldown = 3f;
    [SerializeField] private float minFishSpeed = 1.5f;
    [SerializeField] private float maxFishSpeed = 3f;

    private int score;
    private float timeRemaining;
    private float nextFeedAllowedTime;
    private bool isGameRunning;
    private Color readyFoodColor;
    private Color cooldownFoodColor;

    public bool IsGameRunning => isGameRunning;

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        Vector2 minBounds = new Vector2(topLeftBoundary.position.x, bottomRightBoundary.position.y);
        Vector2 maxBounds = new Vector2(bottomRightBoundary.position.x, topLeftBoundary.position.y);

        for (int i = 0; i < fishTargets.Length; i++)
        {
            fishTargets[i].Initialize(this, minBounds, maxBounds, minFishSpeed, maxFishSpeed);
        }

        score = 0;
        timeRemaining = gameDuration;
        nextFeedAllowedTime = 0f;
        isGameRunning = true;
        readyFoodColor = foodReadyImage.color;
        cooldownFoodColor = new Color(0f, 0f, 0f, readyFoodColor.a);

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateFoodReadyUI();
    }

    private void Update()
    {
        if (!isGameRunning)
        {
            return;
        }

        HandleMouseInput();
        UpdateFoodReadyUI();

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
            FishFeedingTarget fishTarget = hits[i].collider.GetComponentInParent<FishFeedingTarget>();
            if (fishTarget == null)
            {
                continue;
            }

            fishTarget.HandleClick();
            return;
        }
    }

    public void HandleFishClicked(FishFeedingTarget fishTarget)
    {
        if (!isGameRunning || fishTarget == null || Time.time < nextFeedAllowedTime)
        {
            return;
        }

        score += fishTarget.IsFed ? -5 : 10;
        fishTarget.MarkFed();
        UpdateScoreUI();
        Debug.Log($"Fed fish '{fishTarget.name}'.", fishTarget);

        if (AreAllFishFed())
        {
            EndGame();
            return;
        }

        nextFeedAllowedTime = Time.time + feedCooldown;
        UpdateFoodReadyUI();
    }

    private bool AreAllFishFed()
    {
        for (int i = 0; i < fishTargets.Length; i++)
        {
            if (!fishTargets[i].IsFed)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasValidSetup()
    {
        bool hasFishTargets = fishTargets != null && fishTargets.Length > 1;
        bool hasBoundaries = topLeftBoundary != null && bottomRightBoundary != null;
        bool hasReferences = foodReadyImage != null && scoreText != null && bestScoreStore != null;

        if (!hasFishTargets || !hasBoundaries || !hasReferences)
        {
            Debug.LogError("FishFeedingMinigame is missing required references.", this);
            return false;
        }

        for (int i = 0; i < fishTargets.Length; i++)
        {
            if (fishTargets[i] == null)
            {
                Debug.LogError("All 10 fish targets must be assigned.", this);
                return false;
            }
        }

        if (topLeftBoundary.position.x >= bottomRightBoundary.position.x || topLeftBoundary.position.y <= bottomRightBoundary.position.y)
        {
            Debug.LogError("FishFeedingMinigame boundaries are not configured correctly.", this);
            return false;
        }

        return true;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    private void UpdateFoodReadyUI()
    {
        if (foodReadyImage == null)
        {
            return;
        }

        if (feedCooldown <= 0f || Time.time >= nextFeedAllowedTime)
        {
            foodReadyImage.color = readyFoodColor;
            return;
        }

        float cooldownProgress = 1f - ((nextFeedAllowedTime - Time.time) / feedCooldown);
        foodReadyImage.color = Color.Lerp(cooldownFoodColor, readyFoodColor, Mathf.Clamp01(cooldownProgress));
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

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
        Debug.Log($"Minigame finished. Current score: {score}, Best score: {bestScore}");
    }
}
