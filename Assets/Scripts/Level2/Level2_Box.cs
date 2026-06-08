using UnityEngine;
using UnityEngine.InputSystem;

public class BoxController : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRadius = 2.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Visual Indicators")]
    [SerializeField] private GameObject promptCanvas;
    [SerializeField] private GameObject powerSourceIndicator;
    [SerializeField] private GameObject visualMeshObject;

    private Level2_Manager levelManager;
    private bool isPowerSource;
    private bool isOpened;
    private bool playerInRange;
    private InputAction interactAction;

    public bool IsPowerSource => isPowerSource;

    public void SetupBox(bool containsPower, Level2_Manager manager)
    {
        isPowerSource = containsPower;
        levelManager = manager;
        isOpened = false;

        // Force reset indicator objects instantly 
        ToggleRevealIndicator(false);
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    private void Start()
    {
        // Cache our input reference system bound inside your existing Player Input setup rules
        PlayerInput playerInput = Object.FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
        }
    }

    private void Update()
    {
        if (isOpened) return;

        // Perform spatial overlap operations to locate checking elements
        bool checkRange = Physics.CheckSphere(transform.position, interactRadius, playerLayer);

        if (checkRange && !playerInRange)
        {
            playerInRange = true;
            if (promptCanvas != null) promptCanvas.SetActive(true);
        }
        else if (!checkRange && playerInRange)
        {
            playerInRange = false;
            CleanUpPrompt();
        }

        // Process actual execution routines based on real-time hardware status values
        if (playerInRange && interactAction != null && interactAction.WasPerformedThisFrame())
        {
            OpenBox();
        }
    }

    private void OpenBox()
    {
        isOpened = true;
        playerInRange = false;
        CleanUpPrompt();

        if (isPowerSource)
        {
            ProcessPowerSourceDiscovery();
        }
        else
        {
            ProcessExplosiveDetonation();
        }

        // Hide or destroy physical representation node asset structures out from view
        if (visualMeshObject != null)
        {
            visualMeshObject.SetActive(false);
        }

        // Alternatively, use Destroy(gameObject) if you do not have persistent rendering requirements
    }

    private void ProcessPowerSourceDiscovery()
    {
        if (levelManager != null)
        {
            levelManager.RegisterPowerSourceFound();
        }
        // Place custom VFX instantiation hooks here
    }

    private void ProcessExplosiveDetonation()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Inflict health penalty down through damage routine pathways
            player.TakeDamage(25);
            Debug.LogWarning("Boom! Player triggered an explosive chest box hazard unit!");
        }
        // Place screenshake/particle ignition routines here
    }

    public void ToggleRevealIndicator(bool visible)
    {
        // Only allow visualization flags to process if object matches structural requirements
        if (powerSourceIndicator != null && isPowerSource)
        {
            powerSourceIndicator.SetActive(visible);
        }
    }

    private void CleanUpPrompt()
    {
        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}