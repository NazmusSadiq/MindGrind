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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

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
            JunctionNode junction = hits[i].collider.GetComponent<JunctionNode>();
            if (junction == null)
            {
                continue;
            }

            junction.HandleClick();
            return;
        }
    }

    private bool HasValidSetup()
    {
        bool hasReferences = carPrefab != null && scoreText != null && bestScoreStore != null;
        bool hasSpawn = spawnPoint != null && firstTarget != null;

        if (!hasReferences || !hasSpawn)
        {
            return false;
        }

        return true;
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
        CarMover car = Instantiate(carPrefab, spawnPoint.position, Quaternion.identity);

        int randomColorId = Random.Range(0, ColorPalette.ColorCount);

        car.Initialize(this, randomColorId, carSpeed, firstTarget);
    }

    public void HandleCarArrivedAtHouse(CarMover car, HouseNode house)
    {
        if (!isGameRunning || car == null || house == null)
        {
            return;
        }

        if (car.ColorId == house.ColorId)
        {
            score += 10;
            PlaySound(successClip);
        }
        else
        {
            score -= 5;
            if (score < 0) score = 0;
            PlaySound(failureClip);
        }

        UpdateScoreUI();
        car.Remove();
    }

    public void HandleCarLost(CarMover car)
    {
        if (!isGameRunning || car == null)
        {
            return;
        }

        score -= 5;
        if (score < 0) score = 0;
        PlaySound(failureClip);

        UpdateScoreUI();
        car.Remove();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
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

        CarMover[] remainingCars = FindObjectsOfType<CarMover>();
        for (int i = 0; i < remainingCars.Length; i++)
        {
            remainingCars[i].Remove();
        }

        JunctionNode[] junctions = FindObjectsOfType<JunctionNode>();
        for (int i = 0; i < junctions.Length; i++)
        {
            junctions[i].gameObject.SetActive(false);
        }

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