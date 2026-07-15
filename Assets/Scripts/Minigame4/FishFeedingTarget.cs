using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class FishFeedingTarget : MonoBehaviour
{
    [SerializeField] private float boundaryPadding = 0.25f;

    private FishFeedingMinigame gameManager;
    private SpriteRenderer fishRenderer;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private Vector2 moveDirection;
    private float moveSpeed;
    private float minSpeed;
    private float maxSpeed;

    public bool IsFed { get; private set; }

    private void Awake()
    {
        fishRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.IsGameRunning)
        {
            return;
        }

        Move();
        UpdateRotation();
    }

    public void Initialize(FishFeedingMinigame manager, Vector2 minWorldBounds, Vector2 maxWorldBounds, float minimumSpeed, float maximumSpeed)
    {
        gameManager = manager;
        minBounds = minWorldBounds;
        maxBounds = maxWorldBounds;
        minSpeed = Mathf.Max(0.01f, minimumSpeed);
        maxSpeed = Mathf.Max(minSpeed, maximumSpeed);
        IsFed = false;

        PickNewDirection();
        transform.position = new Vector3(
            Random.Range(minBounds.x + boundaryPadding, maxBounds.x - boundaryPadding),
            Random.Range(minBounds.y + boundaryPadding, maxBounds.y - boundaryPadding),
            0f
        );
    }

    public void MarkAsFed()
    {
        IsFed = true;
        // Optional visual change: You can tint your fish here if needed, e.g.:
        // if (fishRenderer != null) fishRenderer.color = Color.green;
    }

    private void Move()
    {
        Vector3 nextPosition = transform.position + (Vector3)(moveDirection * moveSpeed * Time.deltaTime);

        bool hitHorizontalBoundary = nextPosition.x <= minBounds.x + boundaryPadding || nextPosition.x >= maxBounds.x - boundaryPadding;
        bool hitVerticalBoundary = nextPosition.y <= minBounds.y + boundaryPadding || nextPosition.y >= maxBounds.y - boundaryPadding;

        if (hitHorizontalBoundary)
        {
            moveDirection.x *= -1f;
            nextPosition.x = Mathf.Clamp(nextPosition.x, minBounds.x + boundaryPadding, maxBounds.x - boundaryPadding);
        }

        if (hitVerticalBoundary)
        {
            moveDirection.y *= -1f;
            nextPosition.y = Mathf.Clamp(nextPosition.y, minBounds.y + boundaryPadding, maxBounds.y - boundaryPadding);
        }

        transform.position = nextPosition;
    }

    private void PickNewDirection()
    {
        Vector2 newDirection = Random.insideUnitCircle;
        if (newDirection.sqrMagnitude < 0.01f)
        {
            newDirection = Vector2.right;
        }

        moveDirection = newDirection.normalized;
        moveSpeed = Random.Range(minSpeed, maxSpeed);
    }

    private void UpdateRotation()
    {
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

            if (moveDirection.x < 0f)
            {
                angle += 180f;
                transform.localRotation = Quaternion.Euler(0f, 180f, -angle);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}