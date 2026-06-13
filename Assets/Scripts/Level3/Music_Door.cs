using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicDoor : MonoBehaviour, IInteractable
{
    [Header("Door Setup")]
    [SerializeField] private int doorIndex;
    [SerializeField] private Transform teleportLocation;

    [Header("UI")]
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptCanvas;

    private PlayerController currentPlayer;
    private Level3_Manager levelManager;

    private bool isPlayerInRange;
    private bool isCompleted;
    private bool isPanelOpen;

    private string password;

    private void Start()
    {
        levelManager = Object.FindFirstObjectByType<Level3_Manager>();

        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        if (promptCanvas != null)
            promptCanvas.SetActive(false);

        submitButton?.onClick.AddListener(CheckPassword);
        backButton?.onClick.AddListener(ClosePanel);
    }

    public void AssignData(string pwd)
    {
        password = pwd;
    }

    public void Interact(GameObject interactor)
    {
        if (isCompleted)
            return;

        currentPlayer = interactor.GetComponent<PlayerController>();
        OpenPanel();
    }

    private void OpenPanel()
    {
        if (currentPlayer == null) return;

        isPanelOpen = true;

        // 🔥 SAME PATTERN AS LEVEL2 CINEMATIC
        currentPlayer.SetGameStarted(false);

        UpdatePromptState();

        passwordPanel.SetActive(true);
        passwordInput.text = "";
        feedbackText.text = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        passwordInput.Select();
        passwordInput.ActivateInputField();
    }

    private void ClosePanel()
    {
        isPanelOpen = false;

        passwordPanel.SetActive(false);

        if (currentPlayer != null)
            currentPlayer.SetGameStarted(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdatePromptState();
    }

    private void CheckPassword()
    {
        if (isCompleted) return;

        if (passwordInput.text.Trim()
            .Equals(password, System.StringComparison.OrdinalIgnoreCase))
        {
            CompleteDoor();
            ClosePanel();
        }
        else
        {
            feedbackText.text = "Incorrect Password";
        }
    }

    private void CompleteDoor()
    {
        if (isCompleted) return;

        isCompleted = true;

        TeleportPlayer();

        if (levelManager != null)
            levelManager.OnDoorCompleted(doorIndex);
    }

    private void TeleportPlayer()
    {
        if (currentPlayer == null || teleportLocation == null)
            return;

        CharacterController cc = currentPlayer.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        currentPlayer.transform.position = teleportLocation.position;

        if (cc != null)
            cc.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isCompleted)
            return;

        isPlayerInRange = true;
        UpdatePromptState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInRange = false;
        UpdatePromptState();
    }

    private void UpdatePromptState()
    {
        if (promptCanvas == null)
            return;

        if (isCompleted || isPanelOpen)
        {
            promptCanvas.SetActive(false);
            return;
        }

        promptCanvas.SetActive(isPlayerInRange);
    }
}