using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TrafficRouterMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarMover carPrefab;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Spawning")]
    [Tooltip("Where the car physically instantiates.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("The actual GameObject (Junction or House) the car heads to first.")]
    [SerializeField] private GameObject firstTarget;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private float carSpeed = 2.5f;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;

    private int score;
    private float timeRemaining;
    private bool isGameRunning;
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        score = 0;
        timeRemaining = gameDuration;
        isGameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        if (!isGameRunning) return;

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
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Camera activeCamera = Camera.main;
        if (activeCamera == null) return;

        Physics2D.SyncTransforms();

        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(activeCamera.ScreenPointToRay(Mouse.current.position.ReadValue()));
        for (int i = 0; i < hits.Length; i++)
        {
            JunctionNode junction = hits[i].collider.GetComponent<JunctionNode>();
            if (junction == null) continue;

            junction.HandleClick();
            return;
        }
    }

    private bool HasValidSetup()
    {
        return carPrefab != null && bestScoreStore != null && spawnPoint != null && firstTarget != null;
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(spawnInterval);

        while (isGameRunning)
        {
            SpawnCar();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnCar()
    {
        int colorId = ColorPalette.GetRandomColorId();
        CarMover car = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);

        // Pass the first target GameObject directly to the car
        car.Initialize(this, colorId, carSpeed, firstTarget);
    }

    public void HandleCarArrivedAtHouse(CarMover car, HouseNode house)
    {
        if (!isGameRunning)
        {
            car.Remove();
            return;
        }

        bool isMatch = car.ColorId == house.ColorId;
        score += isMatch ? 10 : -5;

        // Console logging as requested
        Debug.Log($"[House Arrival] Car Color ID: {car.ColorId} | House Color ID: {house.ColorId} | Match: {isMatch} | Current Score: {score}");

        UpdateScoreUI();
        car.Remove();
    }

    public void HandleCarLost(CarMover car)
    {
        if (isGameRunning)
        {
            score -= 5;
            Debug.LogWarning($"[Car Lost] A car missed its tracks or target was missing. Penalty applied. Score: {score}");
            UpdateScoreUI();
        }

        car.Remove();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"{score}";
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

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);

        // 1. Remove all active cars remaining on the screen
        CarMover[] remainingCars = FindObjectsOfType<CarMover>();
        for (int i = 0; i < remainingCars.Length; i++)
        {
            remainingCars[i].Remove();
        }

        // 2. Hide all Junction Nodes
        JunctionNode[] junctions = FindObjectsOfType<JunctionNode>();
        for (int i = 0; i < junctions.Length; i++)
        {
            junctions[i].gameObject.SetActive(false);
        }

        // 3. Hide all House Nodes
        HouseNode[] houses = FindObjectsOfType<HouseNode>();
        for (int i = 0; i < houses.Length; i++)
        {
            houses[i].gameObject.SetActive(false);
        }

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);
        bestScoreStore.ShowStats(score, bestScore);
    }
}