using UnityEngine;

[RequireComponent(typeof(PowerLine))]
public class PowerSource : MonoBehaviour, IInteractable, IPowerNode
{
    [Header("Puzzle Assignment")]
    [SerializeField] private PowerGridManager puzzleManager;

    [Header("Initial Output Direction")]
    public Direction outputDirection = Direction.East;

    [Header("Placement")]
    public Vector2Int gridPosition;

    [Header("Visual Indicators (UI)")]
    [SerializeField] private GameObject promptCanvas;

    private PowerLine powerLine;
    private float currentVisualZRotation = 0f;

    private void Awake()
    {
        powerLine = GetComponent<PowerLine>();
        powerLine?.SetPowered(true);
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    public void Interact(GameObject interactor)
    {
        outputDirection = outputDirection.RotateCW();

        transform.localRotation *= Quaternion.Euler(0, 0, 90f);

        puzzleManager?.Recompute();
    }

    public Vector2Int GridPosition => gridPosition;
    public PowerGridManager MyManager => puzzleManager;
    public Direction[] GetOpenHands() => new Direction[] { outputDirection };

    public void SetPowered(bool powered) { /* Permanent Source */ }
    public void ShowPrompt() { if (promptCanvas != null) promptCanvas.SetActive(true); }
    public void HidePrompt() { if (promptCanvas != null) promptCanvas.SetActive(false); }
}