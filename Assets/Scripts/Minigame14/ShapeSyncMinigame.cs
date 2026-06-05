using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ShapeSyncMinigame : MonoBehaviour
{
    private const string RuntimeShapeLayerName = "ShapeLayer";
    private const int MaxSpawnPlacementAttempts = 100;

    private enum ShapeType
    {
        Square,
        Triangle,
        Circle
    }

    private class ShapePiece
    {
        public ShapeType ShapeType;
        public GameObject GameObject;
        public Image Image;
        public int ColorIndex;
        public float NextSwitchTime;
    }

    [Header("References")]
    [SerializeField] private Transform container;
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private MinigameBestScoreStore bestScoreStore;

    [Header("Gameplay")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int startingObjectsPerShape = 2;
    [SerializeField] private int maxObjectsPerShape = 20;
    [SerializeField] private float colorSwitchFrequency = 1.25f;
    [SerializeField] private Vector2 objectSizeRange = new Vector2(42f, 64f);
    [SerializeField] private float reservedTopAreaHeight = 100f;
    [SerializeField] private float spawnSpacing = 8f;

    [Header("Colors")]
    [SerializeField] private Color green = new Color(0.2f, 0.9f, 0.35f);
    [SerializeField] private Color blue = new Color(0.2f, 0.55f, 1f);
    [SerializeField] private Color pink = new Color(1f, 0.25f, 0.75f);

    private readonly List<ShapePiece> pieces = new List<ShapePiece>();
    private readonly List<Rect> occupiedSpawnRects = new List<Rect>();
    private Color[] colors;
    private Sprite squareSprite;
    private Sprite triangleSprite;
    private Sprite circleSprite;
    private RectTransform containerRectTransform;
    private ShapeType targetShape;
    private int objectsPerShape;
    private int score;
    private float timeRemaining;
    private bool isGameRunning;

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            enabled = false;
            return;
        }

        colors = new[] { green, blue, pink };
        containerRectTransform = ResolveContainerRectTransform();
        if (containerRectTransform == null)
        {
            Debug.LogError("ShapeSyncMinigame needs a UI Canvas for shape rendering.", this);
            enabled = false;
            return;
        }

        squareSprite = CreateShapeSprite(ShapeType.Square);
        triangleSprite = CreateShapeSprite(ShapeType.Triangle);
        circleSprite = CreateShapeSprite(ShapeType.Circle);

        Time.timeScale = 1f;
        score = 0;
        objectsPerShape = Mathf.Max(1, startingObjectsPerShape);
        timeRemaining = gameDuration;
        isGameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();
        StartRound();
    }

    private void Update()
    {
        if (!isGameRunning)
        {
            return;
        }

        HandleInput();
        UpdatePieceColors();

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    private bool HasRequiredReferences()
    {
        bool hasReferences = container != null && targetText != null && scoreText != null && timeRemainingText != null && bestScoreStore != null;

        if (!hasReferences)
        {
            Debug.LogError("ShapeSyncMinigame is missing required references.", this);
            return false;
        }

        if (colorSwitchFrequency <= 0f)
        {
            Debug.LogError("ShapeSyncMinigame color switch frequency must be greater than zero.", this);
            return false;
        }

        return true;
    }

    private RectTransform ResolveContainerRectTransform()
    {
        RectTransform assignedRectTransform = container as RectTransform;
        Canvas canvas = ResolveCanvas();

        if (assignedRectTransform != null
            && assignedRectTransform.GetComponentInParent<Canvas>() != null
            && assignedRectTransform.GetComponent<LayoutGroup>() == null)
        {
            assignedRectTransform.SetAsLastSibling();
            return assignedRectTransform;
        }

        if (assignedRectTransform != null)
        {
            Debug.LogWarning("ShapeSyncMinigame is using a runtime shape layer because the assigned Container is not a drawable UI layer or has a LayoutGroup.", this);
        }

        if (canvas == null)
        {
            return null;
        }

        RectTransform shapeLayer = canvas.transform.Find(RuntimeShapeLayerName) as RectTransform;
        if (shapeLayer == null)
        {
            GameObject shapeLayerObject = new GameObject(RuntimeShapeLayerName, typeof(RectTransform));
            shapeLayerObject.layer = canvas.gameObject.layer;
            shapeLayerObject.transform.SetParent(canvas.transform, false);
            shapeLayer = shapeLayerObject.GetComponent<RectTransform>();
        }

        shapeLayer.anchorMin = Vector2.zero;
        shapeLayer.anchorMax = Vector2.one;
        shapeLayer.offsetMin = Vector2.zero;
        shapeLayer.offsetMax = Vector2.zero;
        shapeLayer.pivot = new Vector2(0.5f, 0.5f);
        shapeLayer.SetAsLastSibling();

        return shapeLayer;
    }

    private Canvas ResolveCanvas()
    {
        Canvas canvas = targetText.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        return canvas;
    }

    private void HandleInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            ValidateAnswer();
        }
#else
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ValidateAnswer();
        }
#endif
    }

    private void StartRound()
    {
        ClearPieces();
        occupiedSpawnRects.Clear();
        targetShape = (ShapeType)Random.Range(0, 3);
        targetText.text = $"Target: {targetShape}s";

        SpawnPieces(ShapeType.Square);
        SpawnPieces(ShapeType.Triangle);
        SpawnPieces(ShapeType.Circle);
    }

    private void SpawnPieces(ShapeType shapeType)
    {
        for (int i = 0; i < objectsPerShape; i++)
        {
            int colorIndex = Random.Range(0, colors.Length);
            GameObject pieceObject = CreateUiPiece(shapeType, i, colorIndex);
            Image image = pieceObject.GetComponent<Image>();

            pieces.Add(new ShapePiece
            {
                ShapeType = shapeType,
                GameObject = pieceObject,
                Image = image,
                ColorIndex = colorIndex,
                NextSwitchTime = Time.time + Random.Range(0f, colorSwitchFrequency)
            });
        }
    }

    private GameObject CreateUiPiece(ShapeType shapeType, int index, int colorIndex)
    {
        GameObject pieceObject = new GameObject($"{shapeType}_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pieceObject.transform.SetParent(containerRectTransform, false);

        RectTransform rectTransform = pieceObject.GetComponent<RectTransform>();
        float size = Random.Range(objectSizeRange.x, objectSizeRange.y);
        rectTransform.sizeDelta = new Vector2(size, size);
        rectTransform.anchoredPosition = GetAvailableAnchoredPosition(size);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Image image = pieceObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = GetSprite(shapeType);
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = colors[colorIndex];

        return pieceObject;
    }

    private Vector2 GetAvailableAnchoredPosition(float size)
    {
        Rect spawnBounds = GetSpawnBounds(size);

        for (int i = 0; i < MaxSpawnPlacementAttempts; i++)
        {
            Vector2 position = GetRandomAnchoredPosition(spawnBounds);
            Rect spawnRect = GetSpawnRect(position, size);

            if (!OverlapsOccupiedSpawnRect(spawnRect))
            {
                occupiedSpawnRects.Add(spawnRect);
                return position;
            }
        }

        Vector2 fallbackPosition = GetBestAvailableAnchoredPosition(spawnBounds, size);
        occupiedSpawnRects.Add(GetSpawnRect(fallbackPosition, size));
        return fallbackPosition;
    }

    private Rect GetSpawnBounds(float size)
    {
        Rect rect = containerRectTransform.rect;
        float halfSize = size * 0.5f;
        float minX = rect.xMin + halfSize;
        float maxX = rect.xMax - halfSize;
        float minY = rect.yMin + halfSize;
        float maxY = rect.yMax - Mathf.Max(0f, reservedTopAreaHeight) - halfSize;

        if (minX > maxX)
        {
            minX = maxX = 0f;
        }

        if (minY > maxY)
        {
            minY = maxY = 0f;
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private Vector2 GetRandomAnchoredPosition(Rect spawnBounds)
    {
        return new Vector2(
            Random.Range(spawnBounds.xMin, spawnBounds.xMax),
            Random.Range(spawnBounds.yMin, spawnBounds.yMax));
    }

    private Vector2 GetBestAvailableAnchoredPosition(Rect spawnBounds, float size)
    {
        Vector2 bestPosition = GetRandomAnchoredPosition(spawnBounds);
        float bestDistance = -1f;
        float gridStep = Mathf.Max(1f, size + (Mathf.Max(0f, spawnSpacing) * 2f));
        int columns = Mathf.Max(1, Mathf.FloorToInt(spawnBounds.width / gridStep) + 1);
        int rows = Mathf.Max(1, Mathf.FloorToInt(spawnBounds.height / gridStep) + 1);

        for (int y = 0; y < rows; y++)
        {
            float yPosition = rows == 1 ? spawnBounds.center.y : Mathf.Lerp(spawnBounds.yMin, spawnBounds.yMax, y / (float)(rows - 1));
            for (int x = 0; x < columns; x++)
            {
                float xPosition = columns == 1 ? spawnBounds.center.x : Mathf.Lerp(spawnBounds.xMin, spawnBounds.xMax, x / (float)(columns - 1));
                Vector2 candidatePosition = new Vector2(xPosition, yPosition);
                Rect spawnRect = GetSpawnRect(candidatePosition, size);

                if (!OverlapsOccupiedSpawnRect(spawnRect))
                {
                    return candidatePosition;
                }

                float candidateDistance = GetNearestOccupiedSpawnDistance(candidatePosition);
                if (candidateDistance > bestDistance)
                {
                    bestDistance = candidateDistance;
                    bestPosition = candidatePosition;
                }
            }
        }

        return bestPosition;
    }

    private Rect GetSpawnRect(Vector2 position, float size)
    {
        float halfExtent = (size * 0.5f) + Mathf.Max(0f, spawnSpacing);
        return Rect.MinMaxRect(
            position.x - halfExtent,
            position.y - halfExtent,
            position.x + halfExtent,
            position.y + halfExtent);
    }

    private bool OverlapsOccupiedSpawnRect(Rect spawnRect)
    {
        for (int i = 0; i < occupiedSpawnRects.Count; i++)
        {
            if (spawnRect.Overlaps(occupiedSpawnRects[i]))
            {
                return true;
            }
        }

        return false;
    }

    private float GetNearestOccupiedSpawnDistance(Vector2 position)
    {
        if (occupiedSpawnRects.Count == 0)
        {
            return float.MaxValue;
        }

        float nearestDistance = float.MaxValue;
        for (int i = 0; i < occupiedSpawnRects.Count; i++)
        {
            nearestDistance = Mathf.Min(nearestDistance, Vector2.Distance(position, occupiedSpawnRects[i].center));
        }

        return nearestDistance;
    }

    private Sprite GetSprite(ShapeType shapeType)
    {
        if (shapeType == ShapeType.Triangle)
        {
            return triangleSprite;
        }

        if (shapeType == ShapeType.Circle)
        {
            return circleSprite;
        }

        return squareSprite;
    }

    private void UpdatePieceColors()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            ShapePiece piece = pieces[i];
            if (piece.GameObject == null || Time.time < piece.NextSwitchTime)
            {
                continue;
            }

            piece.ColorIndex = GetNextColorIndex(piece.ColorIndex);
            SetPieceColor(piece, colors[piece.ColorIndex]);
            piece.NextSwitchTime = Time.time + colorSwitchFrequency;
        }
    }

    private void SetPieceColor(ShapePiece piece, Color color)
    {
        if (piece.Image != null)
        {
            piece.Image.color = color;
        }
    }

    private int GetNextColorIndex(int currentColorIndex)
    {
        if (colors.Length <= 1)
        {
            return 0;
        }

        int nextColorIndex = Random.Range(0, colors.Length - 1);
        if (nextColorIndex >= currentColorIndex)
        {
            nextColorIndex++;
        }

        return nextColorIndex;
    }

    private void ValidateAnswer()
    {
        if (!isGameRunning)
        {
            return;
        }

        if (TargetPiecesHaveSameColor())
        {
            score += 10;
            objectsPerShape = Mathf.Min(maxObjectsPerShape, objectsPerShape + 1);
            UpdateScoreUI();
            StartRound();
            return;
        }

        score -= 10;
        UpdateScoreUI();
    }

    private bool TargetPiecesHaveSameColor()
    {
        int expectedColorIndex = -1;

        for (int i = 0; i < pieces.Count; i++)
        {
            ShapePiece piece = pieces[i];
            if (piece.ShapeType != targetShape)
            {
                continue;
            }

            if (expectedColorIndex < 0)
            {
                expectedColorIndex = piece.ColorIndex;
                continue;
            }

            if (piece.ColorIndex != expectedColorIndex)
            {
                return false;
            }
        }

        return expectedColorIndex >= 0;
    }

    private Sprite CreateShapeSprite(ShapeType shapeType)
    {
        const int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool isInside = shapeType == ShapeType.Square
                    || (shapeType == ShapeType.Circle
                        ? IsInsideCircle(x, y, textureSize)
                        : IsInsideTriangle(x, y, textureSize));

                texture.SetPixel(x, y, isInside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    }

    private bool IsInsideCircle(int x, int y, int textureSize)
    {
        float center = (textureSize - 1) * 0.5f;
        float radius = center - 2f;
        float dx = x - center;
        float dy = y - center;

        return dx * dx + dy * dy <= radius * radius;
    }

    private bool IsInsideTriangle(int x, int y, int textureSize)
    {
        float normalizedX = x / (float)(textureSize - 1);
        float normalizedY = y / (float)(textureSize - 1);
        float halfWidthAtY = normalizedY * 0.5f;

        return normalizedY >= 0.04f
            && normalizedY <= 0.96f
            && normalizedX >= 0.5f - halfWidthAtY
            && normalizedX <= 0.5f + halfWidthAtY;
    }

    private void ClearPieces()
    {
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i].GameObject != null)
            {
                Destroy(pieces[i].GameObject);
            }
        }

        pieces.Clear();
    }

    private void UpdateScoreUI()
    {
        scoreText.text = score.ToString();
    }

    private void UpdateTimerUI()
    {
        int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
        timeRemainingText.text = secondsLeft.ToString();
    }

    private void EndGame()
    {
        if (!isGameRunning)
        {
            return;
        }

        isGameRunning = false;
        timeRemaining = 0f;
        UpdateTimerUI();
        ClearPieces();

        string minigameId = SceneManager.GetActiveScene().name;
        int bestScore = MinigameBestScoreStore.UpdateBestScore(minigameId, score);

        bestScoreStore.ShowStats(score, bestScore);
        Debug.Log($"Minigame finished. Current score: {score}, Best score: {bestScore}");
    }
}
