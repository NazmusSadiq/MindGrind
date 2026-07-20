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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;
    [SerializeField] private AudioClip rechargeClip;

    private int score;
    private int fedFishCount;
    private float timeRemaining;
    private float nextFeedAllowedTime;
    private bool isGameRunning;
    private Color readyFoodColor;
    private Color cooldownFoodColor;
    private bool wasCooldownActive;

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

        if (foodReadyImage != null)
        {
            readyFoodColor = foodReadyImage.color;
            cooldownFoodColor = new Color(readyFoodColor.r, readyFoodColor.g, readyFoodColor.b, 0.3f);
        }

        score = 0;
        fedFishCount = 0;
        timeRemaining = gameDuration;
        nextFeedAllowedTime = 0f;
        isGameRunning = true;
        wasCooldownActive = false;

        UpdateScoreUI();
        UpdateTimerUI();
    }

    private void Update()
    {
        if (!isGameRunning || Time.timeScale == 0f)
        {
            return;
        }

        HandleMouseInput();
        UpdateFoodReadyUI();
        HandleRechargeAudioCheck();

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndGame();
        }
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (Time.time < nextFeedAllowedTime)
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

        bool didFeedAttemptOccur = false;

        for (int i = 0; i < hits.Length; i++)
        {
            FishFeedingTarget fish = hits[i].collider.GetComponent<FishFeedingTarget>();
            if (fish == null)
            {
                continue;
            }

            didFeedAttemptOccur = true;

            if (!fish.IsFed)
            {
                // Feeding an unfed fish (Success)
                score += 10;
                fedFishCount++;
                PlaySound(successClip);
                fish.MarkAsFed();
            }
            else
            {
                // Feeding an already fed fish (Failure)
                score -= 5;
                if (score < 0) score = 0;
                PlaySound(failureClip);
            }

            break;
        }

        if (didFeedAttemptOccur)
        {
            nextFeedAllowedTime = Time.time + feedCooldown;
            wasCooldownActive = true;
            UpdateScoreUI();

            // Check if all fish are fed to end the game early
            if (fedFishCount >= fishTargets.Length)
            {
                EndGame();
            }
        }
    }

    private void HandleRechargeAudioCheck()
    {
        if (wasCooldownActive && Time.time >= nextFeedAllowedTime)
        {
            PlaySound(rechargeClip);
            wasCooldownActive = false;
        }
    }

    private bool HasValidSetup()
    {
        bool hasFishTargets = fishTargets != null && fishTargets.Length > 0;
        bool hasBoundaries = topLeftBoundary != null && bottomRightBoundary != null;
        bool hasReferences = scoreText != null && bestScoreStore != null;

        if (!hasFishTargets || !hasBoundaries || !hasReferences)
        {
            return false;
        }

        for (int i = 0; i < fishTargets.Length; i++)
        {
            if (fishTargets[i] == null)
            {
                return false;
            }
        }

        if (topLeftBoundary.position.x >= bottomRightBoundary.position.x || bottomRightBoundary.position.y >= topLeftBoundary.position.y)
        {
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

        // Add remaining time in seconds to total score
        int timeBonus = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining));
        score += timeBonus;

        UpdateScoreUI();
        UpdateTimerUI();

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
    }
}