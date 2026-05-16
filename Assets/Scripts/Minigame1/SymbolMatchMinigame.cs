using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SymbolMatchMinigame : MonoBehaviour
{
    [System.Serializable]
    public class SymbolEntry
    {
        public string id;
        public Sprite sprite;
    }

    [Header("References")]
    [SerializeField] private FallingObject fallingObjectPrefab;
    [SerializeField] private Image targetSymbolImage;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private Transform spawnLeftPoint;
    [SerializeField] private Transform spawnRightPoint;
    [SerializeField] private Transform missLine;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Symbols")]
    [SerializeField] private SymbolEntry[] symbols;
    [SerializeField, Range(0f, 1f)] private float targetSpawnChance = 0.35f;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 45f;
    [SerializeField] private float spawnInterval = 0.75f;
    [SerializeField] private Vector2 fallSpeedRange = new Vector2(3.5f, 5f);
    [SerializeField] private Vector2 spawnRotationRange = new Vector2(0f, 360f);

    private int currentTargetIndex;
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
        PickNextTarget();
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
            FallingObject fallingObject = hits[i].collider.GetComponent<FallingObject>();
            if (fallingObject == null)
            {
                continue;
            }

            fallingObject.HandleClick();
            return;
        }
    }

    private bool HasValidSetup()
    {
        bool hasValidSymbols = symbols != null && symbols.Length > 0;
        bool hasSpawnPoints = spawnLeftPoint != null && spawnRightPoint != null;
        bool hasReferences = fallingObjectPrefab != null && targetSymbolImage != null && missLine != null && bestScoreStore != null;

        if (!hasValidSymbols || !hasSpawnPoints || !hasReferences)
        {
            Debug.LogError("SymbolMatchMinigame is missing required references.", this);
            return false;
        }

        for (int i = 0; i < symbols.Length; i++)
        {
            if (symbols[i] == null || symbols[i].sprite == null)
            {
                Debug.LogError("Every symbol entry needs a sprite assigned.", this);
                return false;
            }
        }

        return true;
    }

    private IEnumerator SpawnLoop()
    {
        while (isGameRunning)
        {
            SpawnFallingObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnFallingObject()
    {
        int symbolIndex = GetRandomSpawnSymbolIndex();
        float spawnX = Random.Range(spawnLeftPoint.position.x, spawnRightPoint.position.x);
        Vector3 spawnPosition = new Vector3(spawnX, spawnLeftPoint.position.y, 0f);
        float spawnRotationZ = Random.Range(spawnRotationRange.x, spawnRotationRange.y);
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, spawnRotationZ);

        FallingObject fallingObject = Instantiate(fallingObjectPrefab, spawnPosition, spawnRotation);
        float fallSpeed = Random.Range(fallSpeedRange.x, fallSpeedRange.y);

        fallingObject.Initialize(this, symbolIndex, symbols[symbolIndex].sprite, fallSpeed, missLine.position.y);
    }

    private int GetRandomSpawnSymbolIndex()
    {
        if (symbols.Length == 1)
        {
            return 0;
        }

        if (Random.value <= targetSpawnChance)
        {
            return currentTargetIndex;
        }

        return Random.Range(0, symbols.Length);
    }

    private void PickNextTarget()
    {
        currentTargetIndex = Random.Range(0, symbols.Length);
        targetSymbolImage.sprite = symbols[currentTargetIndex].sprite;
        targetSymbolImage.SetNativeSize();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
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

    public void HandleObjectClicked(FallingObject fallingObject)
    {
        if (!isGameRunning || fallingObject == null)
        {
            return;
        }

        if (fallingObject.SymbolIndex == currentTargetIndex)
        {
            score += 10;
            PickNextTarget();
        }
        else
        {
            score -= 5;
        }

        UpdateScoreUI();

        fallingObject.Remove();
    }

    public void HandleObjectMissed(FallingObject fallingObject)
    {
        if (!isGameRunning || fallingObject == null)
        {
            return;
        }

        if (fallingObject.SymbolIndex == currentTargetIndex)
        {
            score -= 5;
            UpdateScoreUI();
            PickNextTarget();
        }

        fallingObject.Remove();
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

        FallingObject[] remainingObjects = FindObjectsOfType<FallingObject>();
        for (int i = 0; i < remainingObjects.Length; i++)
        {
            remainingObjects[i].Remove();
        }

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);

        Debug.Log($"Minigame finished. Current score: {score}, Best score: {bestScore}");
    }
}
