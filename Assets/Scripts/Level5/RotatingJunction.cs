using UnityEngine;

public class RotatingJunction : MonoBehaviour, IInteractable, IPowerNode
{
    public enum PieceType { Straight, Corner }

    [Header("Puzzle Assignment")]
    [SerializeField] private PowerGridManager puzzleManager;

    [Header("Piece Definition (Direct Inspector Tracking)")]
    public PieceType pieceType = PieceType.Straight;
    public Direction handA = Direction.North;
    public Direction handB = Direction.East;

    [Header("Placement")]
    public Vector2Int gridPosition;

    [Header("Visual Indicators (UI)")]
    [SerializeField] private GameObject promptCanvas;

    private float baseTiltX;
    private float baseTiltY;
    private float currentVisualZRotation = 0f;
    private PowerLine powerLine;

    private void Awake()
    {
        powerLine = GetComponent<PowerLine>();

        // Capture base editor tilt layout angles
        baseTiltX = transform.localEulerAngles.x;
        baseTiltY = transform.localEulerAngles.y;
        currentVisualZRotation = transform.localEulerAngles.z;

        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    private void Start()
    {
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    public void Interact(GameObject interactor)
    {
        Rotate90Clockwise(triggerRecompute: true);
    }

    public void Rotate90Clockwise(bool triggerRecompute = true)
    {
        // Directly invoke your 0-parameter extension method cleanly
        handA = handA.RotateCW();
        handB = handB.RotateCW();

        // Perform the physical asset spin on the Z-axis
        currentVisualZRotation += 90f;
        transform.localRotation = Quaternion.Euler(baseTiltX, baseTiltY, currentVisualZRotation);

        // Request immediate recompute on your isolated grid puzzle set
        if (triggerRecompute && puzzleManager != null)
        {
            puzzleManager.Recompute();
        }
    }

    // IPowerNode Interface matching layout properties directly
    public Vector2Int GridPosition => gridPosition;
    public PowerGridManager MyManager => puzzleManager;
    public Direction[] GetOpenHands() => new Direction[] { handA, handB };
    public void SetPowered(bool powered) => powerLine?.SetPowered(powered);

    public void ShowPrompt() { if (promptCanvas != null) promptCanvas.SetActive(true); }
    public void HidePrompt() { if (promptCanvas != null) promptCanvas.SetActive(false); }
}