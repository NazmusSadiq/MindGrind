using UnityEngine;

public class MusicDoor : MonoBehaviour, IInteractable
{
    [Header("Door Setup")]
    [SerializeField] private int doorIndex;
    [SerializeField] private Transform teleportLocation;

    [Header("Penalty")]
    [SerializeField] private float wrongPasswordPenaltySeconds = 5f;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptCanvas;

    private Level3_Manager levelManager;
    private bool isPlayerInRange;
    private bool isCompleted;

    private Level3_Manager.RhymeData assignedData;
    private string password;

    private void Start()
    {
        levelManager = Object.FindFirstObjectByType<Level3_Manager>();

        if (promptCanvas != null)
            promptCanvas.SetActive(false);
    }

    public void AssignData(Level3_Manager.RhymeData data)
    {
        assignedData = data;
        password = data.password;
    }

    public string GetPassword() => password;
    public float GetPenaltySeconds() => wrongPasswordPenaltySeconds;
    public AudioClip GetDoorAudio() => assignedData?.rhymeClip;

    public void Interact(GameObject interactor)
    {
        if (isCompleted || levelManager == null)
            return;

        // Pass control operations universally to manager script
        levelManager.OpenUniversalPanel(this, password);
    }

    public void CompleteDoor(GameObject playerObj)
    {
        if (isCompleted) return;

        isCompleted = true;
        TeleportPlayer(playerObj);

        if (levelManager != null)
            levelManager.OnDoorCompleted(doorIndex);
    }

    private void TeleportPlayer(GameObject playerObj)
    {
        if (playerObj == null || teleportLocation == null) return;

        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerObj.transform.position = teleportLocation.position;

        if (cc != null) cc.enabled = true;
    }

    public void ClearInteractionPrompt()
    {
        UpdatePromptState(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isCompleted) return;
        isPlayerInRange = true;
        UpdatePromptState(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = false;
        UpdatePromptState(false);
    }

    private void UpdatePromptState(bool shouldShow)
    {
        if (promptCanvas == null) return;

        if (isCompleted)
        {
            promptCanvas.SetActive(false);
            return;
        }

        promptCanvas.SetActive(shouldShow);
    }
}