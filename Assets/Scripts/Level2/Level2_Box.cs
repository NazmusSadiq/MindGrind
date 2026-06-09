using UnityEngine;
using UnityEngine.InputSystem;

public class BoxController : MonoBehaviour, IInteractable
{
    [Header("Visual Indicators (UI)")]
    [SerializeField] private GameObject promptCanvas;

    [Header("Sprite Graphic Swaps")]
    [SerializeField] private SpriteRenderer boxSpriteRenderer;
    [SerializeField] private Sprite coveredSprite;
    [SerializeField] private Sprite powerCellSprite;
    [SerializeField] private Sprite emptySprite;

    private Level2_Manager levelManager;
    private bool isPowerSource;
    private bool isOpened;
    private bool isPlayerInRange; // Tracked locally to handle smooth UI handoffs

    public bool IsPowerSource => isPowerSource;

    public void SetupBox(bool containsPower, Level2_Manager manager)
    {
        isPowerSource = containsPower;
        levelManager = manager;
        isOpened = false;
        isPlayerInRange = false;

        SetBoxSprite(coveredSprite);

        if (promptCanvas != null) promptCanvas.SetActive(false);
        Debug.Log($"[BoxController Log] {gameObject.name} initialized. PowerSource: {isPowerSource}");
    }

    private void Start()
    {
        if (promptCanvas != null) promptCanvas.SetActive(false);

        if (boxSpriteRenderer == null)
        {
            boxSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void Interact(GameObject interactor)
    {
        if (isOpened)
        {
            Debug.Log($"[BoxController Log] {gameObject.name} has already been opened! Ignoring interaction.");
            return;
        }

        Debug.Log($"[BoxController Log] Interact() successfully triggered on {gameObject.name} by {interactor.name}!");
        OpenBox();
    }

    private void OpenBox()
    {
        isOpened = true;
        HidePrompt();

        SetBoxSprite(emptySprite);

        if (isPowerSource)
        {
            Debug.Log($"[BoxController Log] {gameObject.name} opened: Power Cell found!");
            if (levelManager != null) levelManager.RegisterPowerSourceFound();
        }
        else
        {
            Debug.LogWarning($"[BoxController Log] {gameObject.name} opened: Boom! Explosive chest triggered!");
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) player.TakeDamage(25);
        }
    }

    public void ShowPrompt()
    {
        isPlayerInRange = true; // Player entered physically
        if (isOpened) return;

        Debug.Log($"[BoxController Log] Player inside trigger radius of {gameObject.name}. Showing prompt UI.");
        if (promptCanvas != null) promptCanvas.SetActive(true);
    }

    public void HidePrompt()
    {
        isPlayerInRange = false; // Player walked away
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    public void ToggleRevealIndicator(bool showReveal)
    {
        if (isOpened) return;

        if (showReveal)
        {
            // During cinematic: swap the sprite if it's a power source
            if (isPowerSource)
            {
                SetBoxSprite(powerCellSprite);
            }

            // Always turn on the text prompt for all boxes during cinematic so players can track them
            if (promptCanvas != null) promptCanvas.SetActive(true);
        }
        else
        {
            // Reset back to covered sprite if it was revealing a power cell
            SetBoxSprite(coveredSprite);

            // Clean up text prompts: only keep text visible if player is physically standing in range
            if (promptCanvas != null)
            {
                promptCanvas.SetActive(isPlayerInRange);
            }
        }
    }

    private void SetBoxSprite(Sprite targetSprite)
    {
        if (boxSpriteRenderer != null && targetSprite != null)
        {
            boxSpriteRenderer.sprite = targetSprite;
        }
    }
}