using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PowerLine))]
public class PowerDestination : MonoBehaviour, IInteractable, IPowerNode
{
    [Header("Puzzle Assignment")]
    [SerializeField] private PowerGridManager puzzleManager;

    [Header("Placement")]
    public Vector2Int gridPosition;

    [Header("Base Input Direction")]
    public Direction inputDirection = Direction.West;

    [Header("Visual Indicators (UI)")]
    [SerializeField] private GameObject promptCanvas;

    [Header("Events")]
    public UnityEvent OnPowered;
    public UnityEvent OnUnpowered;

    public bool IsPowered { get; private set; }
    private PowerLine powerLine;
    private float currentVisualZRotation = 0f;

    private void Awake()
    {
        powerLine = GetComponent<PowerLine>();
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    public void Interact(GameObject interactor)
    {
        inputDirection = inputDirection.RotateCW();

        transform.localRotation *= Quaternion.Euler(0, 0, 90f);

        puzzleManager?.Recompute();
    }

    public Vector2Int GridPosition => gridPosition;
    public PowerGridManager MyManager => puzzleManager;
    public Direction[] GetOpenHands() => new Direction[] { inputDirection };

    public void SetPowered(bool powered)
    {
        if (IsPowered == powered) return;
        IsPowered = powered;
        powerLine?.SetPowered(powered);

        if (IsPowered) OnPowered?.Invoke();
        else OnUnpowered?.Invoke();
    }

    public void ShowPrompt() { if (promptCanvas != null) promptCanvas.SetActive(true); }
    public void HidePrompt() { if (promptCanvas != null) promptCanvas.SetActive(false); }
}