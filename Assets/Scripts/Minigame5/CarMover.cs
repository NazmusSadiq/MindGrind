using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CarMover : MonoBehaviour
{
    [SerializeField] private float arrivalThreshold = 0.05f;

    [Header("Rotation Setup")]
    [Tooltip("How long (in seconds) the car takes to fully blend to its new target angle.")]
    [SerializeField] private float rotationDuration = 0.4f;

    [Tooltip("The sprite's facing direction at 0 rotation.")]
    [SerializeField] private float spriteForwardOffset = 180f;

    private TrafficRouterMinigame minigame;
    private SpriteRenderer spriteRenderer;

    private GameObject currentTargetObject;
    private float moveSpeed;
    private int colorId;
    private bool isActive;

    private Quaternion targetRotation;
    private float rotationElapsedTime;

    public int ColorId => colorId;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(TrafficRouterMinigame owner, int trainColorId, float speed, GameObject initialTarget)
    {
        minigame = owner;
        colorId = trainColorId;
        moveSpeed = speed;
        currentTargetObject = initialTarget;
        isActive = true;

        spriteRenderer.color = ColorPalette.GetColor(colorId);

        // Initialize our rotations so it doesn't snap wildly on frame one
        if (currentTargetObject != null)
        {
            CalculateAndSetTargetRotation(currentTargetObject.transform.position, true);
        }
    }

    private void Update()
    {
        if (!isActive || currentTargetObject == null) return;

        Vector3 targetPosition = currentTargetObject.transform.position;

        // 1. Interpolate Rotation smoothly over time
        if (rotationElapsedTime < rotationDuration)
        {
            rotationElapsedTime += Time.deltaTime;
            // Calculate progress percentage (0.0 to 1.0)
            float t = Mathf.Clamp01(rotationElapsedTime / rotationDuration);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
        else
        {
            transform.rotation = targetRotation;
        }

        // 2. Translate Position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // 3. Check Destination
        if (Vector3.Distance(transform.position, targetPosition) <= arrivalThreshold)
        {
            EvaluateTargetArrival();
        }
    }

    private void CalculateAndSetTargetRotation(Vector3 targetPos, bool instant = false)
    {
        Vector3 direction = targetPos - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            // Calculate angle based on movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteForwardOffset;

            // Assign the target configuration
            targetRotation = Quaternion.Euler(0f, 0f, angle);

            // Reset the interpolation timer
            rotationElapsedTime = 0f;

            if (instant)
            {
                transform.rotation = targetRotation;
                rotationElapsedTime = rotationDuration;
            }
        }
    }

    private void EvaluateTargetArrival()
    {
        // 1. Is the reached object a House?
        HouseNode house = currentTargetObject.GetComponent<HouseNode>();
        if (house != null)
        {
            isActive = false;
            minigame.HandleCarArrivedAtHouse(this, house);
            return;
        }

        // 2. Is the reached object a Junction?
        JunctionNode junction = currentTargetObject.GetComponent<JunctionNode>();
        if (junction != null)
        {
            GameObject nextTarget = junction.GetActiveTarget();
            if (nextTarget == null)
            {
                isActive = false;
                minigame.HandleCarLost(this);
                return;
            }

            // Route onto next target
            currentTargetObject = nextTarget;

            // Recalculate where we are turning next to begin the interpolation process
            CalculateAndSetTargetRotation(currentTargetObject.transform.position);
            return;
        }

        // Catch-all safety fallback
        isActive = false;
        minigame.HandleCarLost(this);
    }

    public void Remove()
    {
        isActive = false;
        Destroy(gameObject);
    }
}