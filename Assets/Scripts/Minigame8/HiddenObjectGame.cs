using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HiddenObjectGame : MonoBehaviour
{
    [System.Serializable]
    public class HiddenObjectData
    {
        public string objectName;
        public Sprite sprite;
        public Vector3 scale = Vector3.one;
        public float placementRadius = 0.4f;
    }

    [System.Serializable]
    public class SpawnArea
    {
        public string areaName;
        public BoxCollider2D areaCollider;
        public bool isShelf;
    }

    [Header("Prefab")]
    [SerializeField] private HiddenObject hiddenObjectPrefab;

    [Header("Spawn Areas")]
    [SerializeField] private SpawnArea floorArea;
    [SerializeField] private SpawnArea[] shelfAreas;

    [Header("Objects Database")]
    [SerializeField] private string resourcesObjectFolder = "Minigame_8";
    [SerializeField] private HiddenObjectData[] allObjects;

    [Header("UI")]
    [SerializeField] private Image targetImage;
    [SerializeField] private TMP_Text targetLabelText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text scoreValueText;
    [SerializeField] private TMP_Text timeLabelText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Gameplay")]
    [SerializeField] private int objectsPerShelf = 3;
    [SerializeField] private int floorObjectCount = 30;
    [SerializeField] private int floorColumns = 10;
    [SerializeField] private int floorRows = 3;
    [SerializeField, Range(0f, 0.45f)] private float areaPaddingPercent = 0.12f;
    [SerializeField, Range(0f, 0.45f)] private float floorJitterPercent = 0.18f;
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int scorePerCorrectClick = 10;
    [SerializeField] private int scorePerLateCorrectClick = 5;
    [SerializeField] private int wrongClickPenalty = 10;
    [SerializeField] private int missedTargetPenalty = 5;
    [SerializeField] private float layoutChangeInterval = 5f;
    [SerializeField] private float targetChangeInterval = 10f;

    private readonly List<HiddenObject> spawnedObjects = new List<HiddenObject>();
    private readonly List<PlacedObjectInfo> placedObjects = new List<PlacedObjectInfo>();

    private HiddenObjectData currentTargetData;
    private int score;
    private float gameTimer;
    private float layoutTimer;
    private float targetTimer;
    private bool gameRunning;

    private struct PlacedObjectInfo
    {
        public Vector2 position;
        public float radius;
    }

    private void Awake()
    {
        score = 0;
    }

    private void Start()
    {
        LoadObjectsFromResources();

        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        score = 0;
        gameTimer = gameDuration;
        layoutTimer = 0f;
        targetTimer = 0f;
        gameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();
        StartNewTargetRound();
    }

    private void Update()
    {
        if (!gameRunning)
        {
            return;
        }

        gameTimer -= Time.deltaTime;

        if (gameTimer <= 0f)
        {
            EndGame();
            return;
        }

        layoutTimer += Time.deltaTime;
        targetTimer += Time.deltaTime;

        bool refreshedLayout = HandleLayoutAndTargetTimers();

        if (!gameRunning || refreshedLayout)
        {
            UpdateTimerUI();
            return;
        }

        HandleMouseInput();
        UpdateTimerUI();
    }

    private bool HandleLayoutAndTargetTimers()
    {
        if (targetTimer >= targetChangeInterval)
        {
            score -= missedTargetPenalty;
            UpdateScoreUI();
            StartNewTargetRound();
            return true;
        }

        if (layoutTimer >= layoutChangeInterval)
        {
            layoutTimer = 0f;
            GenerateNewLayout(false);
            return true;
        }

        return false;
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
            Debug.LogError("Hidden Object minigame needs a MainCamera for object clicks.", this);
            return;
        }

        Physics2D.SyncTransforms();

        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(activeCamera.ScreenPointToRay(Mouse.current.position.ReadValue()));
        for (int i = 0; i < hits.Length; i++)
        {
            HiddenObject hiddenObject = hits[i].collider.GetComponentInParent<HiddenObject>();
            if (hiddenObject == null)
            {
                continue;
            }

            hiddenObject.HandleClick();
            return;
        }
    }

    private bool HasValidSetup()
    {
        if (hiddenObjectPrefab == null)
        {
            Debug.LogError("Hidden Object Prefab missing.", this);
            return false;
        }

        if (floorArea == null || floorArea.areaCollider == null)
        {
            Debug.LogError("Floor Area missing.", this);
            return false;
        }

        if (shelfAreas == null || shelfAreas.Length == 0)
        {
            Debug.LogError("Shelf Areas missing.", this);
            return false;
        }

        for (int i = 0; i < shelfAreas.Length; i++)
        {
            if (shelfAreas[i] == null || shelfAreas[i].areaCollider == null)
            {
                Debug.LogError("Every shelf area needs a BoxCollider2D.", this);
                return false;
            }
        }

        if (allObjects == null || allObjects.Length < 2)
        {
            Debug.LogError("Object Database needs at least 2 objects so the target appears only once.", this);
            return false;
        }

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i] == null || allObjects[i].sprite == null)
            {
                Debug.LogError("Every hidden object data entry needs a sprite.", this);
                return false;
            }
        }

        if (targetImage == null)
        {
            Debug.LogError("Target Image missing.", this);
            return false;
        }

        if (bestScoreStore == null)
        {
            Debug.LogError("BestScoreStore missing.", this);
            return false;
        }

        return true;
    }

    private void LoadObjectsFromResources()
    {
        if (HasValidObjectDatabase())
        {
            return;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesObjectFolder);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"No sprites found in Resources/{resourcesObjectFolder}. Make sure the images are inside a Resources folder and imported as Sprite.", this);
            return;
        }

        allObjects = new HiddenObjectData[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            allObjects[i] = new HiddenObjectData
            {
                objectName = sprites[i].name,
                sprite = sprites[i],
                scale = Vector3.one,
                placementRadius = 0.4f
            };
        }
    }

    private bool HasValidObjectDatabase()
    {
        if (allObjects == null || allObjects.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i] == null || allObjects[i].sprite == null)
            {
                return false;
            }
        }

        return true;
    }

    private void StartNewTargetRound()
    {
        layoutTimer = 0f;
        targetTimer = 0f;
        GenerateNewLayout(true);
    }

    private void GenerateNewLayout(bool chooseNewTarget)
    {
        ClearSpawnedObjects();
        placedObjects.Clear();

        if (chooseNewTarget || currentTargetData == null)
        {
            currentTargetData = GetRandomObjectDataExcept(currentTargetData);
        }

        UpdateTargetUI();

        int shelfSlots = Mathf.Max(1, objectsPerShelf);
        int floorObjects = Mathf.Max(1, floorObjectCount);
        int shelfObjectCount = shelfAreas.Length * shelfSlots;
        int totalObjects = shelfObjectCount + floorObjects;
        int targetSlot = Random.Range(0, totalObjects);
        int spawnIndex = 0;

        for (int shelfIndex = 0; shelfIndex < shelfAreas.Length; shelfIndex++)
        {
            for (int slotIndex = 0; slotIndex < shelfSlots; slotIndex++)
            {
                bool isTarget = spawnIndex == targetSlot;
                HiddenObjectData data = isTarget ? currentTargetData : GetRandomDistractorData(currentTargetData);

                SpawnObjectInShelfSlot(data, shelfAreas[shelfIndex], slotIndex, shelfSlots, isTarget);
                spawnIndex++;
            }
        }

        for (int i = 0; i < floorObjects; i++)
        {
            bool isTarget = spawnIndex == targetSlot;
            HiddenObjectData data = isTarget ? currentTargetData : GetRandomDistractorData(currentTargetData);

            SpawnObjectInFloorSlot(data, floorArea, i, floorObjects, isTarget);
            spawnIndex++;
        }
    }

    private void SpawnObjectInArea(HiddenObjectData data, SpawnArea area, bool isTarget)
    {
        if (data == null || area == null || area.areaCollider == null)
        {
            return;
        }

        Vector3 position = GetRandomPositionInArea(area.areaCollider);
        HiddenObject obj = Instantiate(hiddenObjectPrefab, position, Quaternion.identity);

        obj.Initialize(
            this,
            data.objectName,
            data.sprite,
            isTarget,
            data.scale
        );

        spawnedObjects.Add(obj);

        placedObjects.Add(new PlacedObjectInfo { position = position, radius = data.placementRadius });
    }

    private void SpawnObjectInShelfSlot(HiddenObjectData data, SpawnArea area, int slotIndex, int slotCount, bool isTarget)
    {
        if (data == null || area == null || area.areaCollider == null)
        {
            return;
        }

        Vector3 position = GetShelfSlotPosition(data, area.areaCollider, slotIndex, slotCount);

        // Debug.Log(
        //     $"Shelf={area.areaName} Slot={slotIndex} Pos={position}"
        // );

        HiddenObject obj = Instantiate(hiddenObjectPrefab, position, Quaternion.identity);

        obj.Initialize(
            this,
            data.objectName,
            data.sprite,
            isTarget,
            data.scale
        );

        spawnedObjects.Add(obj);
    }

    private void SpawnObjectInFloorSlot(HiddenObjectData data, SpawnArea area, int slotIndex, int totalSlots, bool isTarget)
    {
        if (data == null || area == null || area.areaCollider == null)
        {
            return;
        }

        Vector3 position = GetFloorSlotPosition(area.areaCollider, slotIndex, totalSlots);
        HiddenObject obj = Instantiate(hiddenObjectPrefab, position, Quaternion.identity);

        obj.Initialize(
            this,
            data.objectName,
            data.sprite,
            isTarget,
            data.scale
        );

        spawnedObjects.Add(obj);
    }

    private Vector3 GetShelfSlotPosition(
    HiddenObjectData data,
    BoxCollider2D areaCollider,
    int slotIndex,
    int slotCount)
    {
        Vector3 center = areaCollider.transform.position;

        float spacing = 2f;

        // Debug.Log(
        //     "Local Scale = " + areaCollider.transform.localScale +
        //     " | Lossy Scale = " + areaCollider.transform.lossyScale
        // );

        // Debug.Log(
        //     "Collider Size = " + areaCollider.size
        // );

        // Debug.Log(
        //     "Bounds Size = " + areaCollider.bounds.size
        // );

        return new Vector3(
            center.x + (slotIndex - 1) * spacing,
            center.y,
            0
        );
    }



    private Vector3 GetFloorSlotPosition(
    BoxCollider2D areaCollider,
    int slotIndex,
    int totalSlots)
    {
        Vector3 center = areaCollider.transform.position;

        int columns = 10;
        int rows = 3;

        float horizontalSpacing = 1.5f;
        float verticalSpacing = 1.5f;

        int row = slotIndex / columns;
        int column = slotIndex % columns;

        float startX = center.x - ((columns - 1) * horizontalSpacing * 0.5f);
        float startY = center.y + ((rows - 1) * verticalSpacing * 0.5f);

        float x = startX + column * horizontalSpacing;
        float y = startY - row * verticalSpacing;

        return new Vector3(x, y, 0f);
    }

    private Vector3 GetRandomPositionInArea(BoxCollider2D areaCollider)
    {
        Bounds bounds = areaCollider.bounds;
        float paddingX = bounds.size.x * areaPaddingPercent;
        float paddingY = bounds.size.y * areaPaddingPercent;
        float x = Random.Range(bounds.min.x + paddingX, bounds.max.x - paddingX);
        float y = Random.Range(bounds.min.y + paddingY, bounds.max.y - paddingY);

        return new Vector3(x, y, hiddenObjectPrefab.transform.position.z);
    }

    private HiddenObjectData GetRandomObjectData()
    {
        return allObjects[Random.Range(0, allObjects.Length)];
    }

    private HiddenObjectData GetRandomObjectDataExcept(HiddenObjectData excludedData)
    {
        HiddenObjectData data = GetRandomObjectData();

        int safety = 0;
        while (data == excludedData && allObjects.Length > 1 && safety < 100)
        {
            data = GetRandomObjectData();
            safety++;
        }

        return data;
    }

    private HiddenObjectData GetRandomDistractorData(HiddenObjectData targetData)
    {
        HiddenObjectData data = GetRandomObjectData();

        int safety = 0;
        while (data == targetData && safety < 100)
        {
            data = GetRandomObjectData();
            safety++;
        }

        return data;
    }

    private void UpdateTargetUI()
    {
        if (currentTargetData == null)
        {
            return;
        }

        if (targetImage != null)
        {
            targetImage.sprite = currentTargetData.sprite;
            targetImage.color = Color.white;
            targetImage.enabled = true;
            targetImage.preserveAspect = true;
            targetImage.type = Image.Type.Simple;
        }

        if (targetLabelText != null)
        {
            targetLabelText.text = "Target :";
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score :";
        }

        if (scoreValueText != null)
        {
            scoreValueText.text = score.ToString();
        }
    }

    private void UpdateTimerUI()
    {
        if (timeLabelText != null)
        {
            timeLabelText.text = "TIME :";
        }

        if (timeRemainingText != null)
        {
            int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, gameTimer));
            timeRemainingText.text = secondsLeft.ToString();
        }
    }

    public void OnObjectClicked(HiddenObject obj)
    {
        if (!gameRunning || obj == null)
        {
            return;
        }

        if (!obj.IsTarget)
        {
            score -= wrongClickPenalty;
            UpdateScoreUI();
            return;
        }

        score += targetTimer <= layoutChangeInterval ? scorePerCorrectClick : scorePerLateCorrectClick;
        UpdateScoreUI();

        StartNewTargetRound();
    }

    private void EndGame()
    {
        if (!gameRunning)
        {
            return;
        }

        gameRunning = false;
        gameTimer = 0f;
        UpdateTimerUI();
        ClearSpawnedObjects();

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);

        Debug.Log($"Hidden Object minigame finished. Current score: {score}, Best score: {bestScore}");
    }

    private void ClearSpawnedObjects()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                spawnedObjects[i].Remove();
            }
        }

        spawnedObjects.Clear();
    }

}
