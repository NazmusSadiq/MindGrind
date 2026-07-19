using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Level3_Manager : MonoBehaviour
{
    [System.Serializable]
    public class RhymeData
    {
        public AudioClip rhymeClip;
        public string password;
    }

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text timeRemainingText;

    [Header("Audio (GLOBAL)")]
    [SerializeField] private AudioSource globalAudioSource;

    [Header("Rhyme Pool")]
    [SerializeField] private RhymeData[] rhymes;

    [Header("Doors")]
    [SerializeField] private MusicDoor[] doors;

    [Header("Universal Password UI")]
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text placeholderText;

    private float elapsedTime;
    private bool isLevelOver;

    private int currentDoorIndex = 0;
    private Coroutine audioLoopCoroutine;
    private bool isMusicPausedForInteraction = false;

    // Runtime reference tracking which door is being handled
    private MusicDoor activeInteractedDoor;
    private bool isPanelOpen = false;
    private float lastCheckPasswordTime = -1f;
    private const float CheckPasswordDebounce = 0.15f;

    private void Start()
    {
        bool storyModeActive = false;

        System.Reflection.FieldInfo storyModeField = typeof(MainMenu).GetField("isStoryMode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (storyModeField != null)
        {
            storyModeActive = (bool)storyModeField.GetValue(null);
        }

        if (playerController == null)
            playerController = Object.FindFirstObjectByType<PlayerController>();

        if (playerController == null)
        {
            enabled = false;
            return;
        }

        // Initialize Universal UI state safely
        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        if (placeholderText == null && passwordInput != null && passwordInput.placeholder != null)
        {
            placeholderText = passwordInput.placeholder.GetComponent<TMP_Text>();
        }

        elapsedTime = 0f;
        MinigamePenaltyTracker.ResetPenalty();
        UpdateTimerUI();

        AssignRhymesToDoors();

        if (storyModeActive)
        {
            MainMenu.PauseGameAndShowDetailsPanel();
            StartCoroutine(WaitToStartStoryModeMusic());
        }
        else
        {
            playerController.SetGameStarted(true);
            playerController.EnableGameplayInput(true);
            PlayDoorMusic(0);
        }
    }

    private System.Collections.IEnumerator WaitToStartStoryModeMusic()
    {
        yield return new WaitUntil(() => Time.timeScale > 0f);
        yield return new WaitForSeconds(0.25f);
        PlayDoorMusic(0);
    }

    private void AssignRhymesToDoors()
    {
        if (rhymes.Length < doors.Length) return;

        List<int> used = new List<int>();
        for (int i = 0; i < doors.Length; i++)
        {
            int index;
            do { index = Random.Range(0, rhymes.Length); } while (used.Contains(index));
            used.Add(index);
            doors[i].AssignData(rhymes[index]);
        }
    }

    // --- UNIVERSAL INTERACTION PIPELINE ---

    public void OpenUniversalPanel(MusicDoor door, string targetPassword)
    {
        if (playerController == null) return;

        activeInteractedDoor = door;
        isPanelOpen = true;

        playerController.SetGameStarted(false);

        PauseMusicForInteraction(true);

        submitButton?.onClick.RemoveAllListeners();
        submitButton?.onClick.AddListener(CheckPassword);

        backButton?.onClick.RemoveAllListeners();
        backButton?.onClick.AddListener(CloseUniversalPanel);

        passwordInput?.onSubmit.RemoveAllListeners();
        passwordInput?.onSubmit.AddListener((text) => CheckPassword());

        if (placeholderText != null)
        {
            placeholderText.text = $"letter count of words, e.g. {targetPassword.Length} digits";
        }

        passwordPanel.SetActive(true);
        passwordInput.text = "";
        feedbackText.text = "";

        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            passwordInput.shouldHideMobileInput = true;
            passwordInput.DeactivateInputField();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    public void CloseUniversalPanel()
    {
        isPanelOpen = false;
        passwordPanel.SetActive(false);

        if (playerController != null)
            playerController.SetGameStarted(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (activeInteractedDoor != null)
        {
            activeInteractedDoor.ClearInteractionPrompt();
        }

        if (!isLevelOver)
            PauseMusicForInteraction(false);

        activeInteractedDoor = null;
        Time.timeScale = 1f;
    }

    private void CheckPassword()
    {
        if (activeInteractedDoor == null || Time.unscaledTime - lastCheckPasswordTime < CheckPasswordDebounce)
            return;

        lastCheckPasswordTime = Time.unscaledTime;

        if (passwordInput.text.Trim().Equals(activeInteractedDoor.GetPassword(), System.StringComparison.OrdinalIgnoreCase))
        {
            activeInteractedDoor.CompleteDoor(playerController.gameObject);
            CloseUniversalPanel();
        }
        else
        {
            AddTimePenalty(activeInteractedDoor.GetPenaltySeconds());
            feedbackText.text = "Incorrect Password";
            passwordInput.text = "";
        }
    }

    public void InputDigitFromString(string digit)
    {
        if (!isPanelOpen || string.IsNullOrEmpty(digit) || passwordInput == null)
            return;

        if (char.IsDigit(digit[0]))
        {
            passwordInput.text += digit[0];
        }
    }

    public void TriggerBackspace()
    {
        if (!isPanelOpen || passwordInput == null)
            return;

        if (passwordInput.text.Length > 0)
        {
            passwordInput.text = passwordInput.text.Substring(0, passwordInput.text.Length - 1);
        }
    }

    // --- MUSIC & PROGRESSION FLOW ---

    public void PlayDoorMusic(int doorIndex)
    {
        if (doorIndex < 0 || doorIndex >= doors.Length) return;

        currentDoorIndex = doorIndex;
        isMusicPausedForInteraction = false;

        if (globalAudioSource == null) return;

        if (audioLoopCoroutine != null) StopCoroutine(audioLoopCoroutine);

        globalAudioSource.Stop();
        globalAudioSource.clip = doors[doorIndex].GetDoorAudio();
        globalAudioSource.loop = false;

        audioLoopCoroutine = StartCoroutine(TimedAudioLoopCoroutine());
    }

    private System.Collections.IEnumerator TimedAudioLoopCoroutine()
    {
        while (!isLevelOver)
        {
            if (globalAudioSource.clip != null && !isMusicPausedForInteraction)
            {
                globalAudioSource.Play();
                yield return new WaitWhile(() => globalAudioSource.isPlaying || isMusicPausedForInteraction);
            }
            if (isMusicPausedForInteraction)
            {
                yield return null;
                continue;
            }
            yield return new WaitForSeconds(10f);
        }
    }

    public void PauseMusicForInteraction(bool pause)
    {
        if (globalAudioSource == null) return;
        isMusicPausedForInteraction = pause;

        if (pause)
        {
            if (globalAudioSource.isPlaying) globalAudioSource.Pause();
        }
        else
        {
            if (!globalAudioSource.isPlaying && globalAudioSource.clip != null) globalAudioSource.UnPause();
        }
    }

    public void StopMusic()
    {
        if (audioLoopCoroutine != null) StopCoroutine(audioLoopCoroutine);
        if (globalAudioSource != null) globalAudioSource.Stop();
    }

    public void OnDoorCompleted(int doorIndex)
    {
        if (doorIndex + 1 < doors.Length)
            PlayDoorMusic(doorIndex + 1);
        else
            StopMusic();
    }

    private void Update()
    {
        if (isLevelOver) return;
        if (playerController.IsDead)
        {
            TriggerGameOver();
            return;
        }
        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    public void TriggerLevelComplete()
    {
        if (isLevelOver) return;
        isLevelOver = true;
        StopMusic();
    }

    private void TriggerGameOver()
    {
        isLevelOver = true;
        StopMusic();
        playerController.ShowGameOverMenu();
    }

    private void UpdateTimerUI()
    {
        if (timeRemainingText == null) return;
        int m = Mathf.FloorToInt(elapsedTime / 60f);
        int s = Mathf.FloorToInt(elapsedTime % 60f);
        timeRemainingText.text = m > 0 ? $"{m}:{s:00}" : s.ToString();
    }

    public float GetElapsedTime() => elapsedTime;

    public void AddTimePenalty(float seconds)
    {
        if (seconds <= 0f) return;
        elapsedTime += seconds;
        MinigamePenaltyTracker.AddPenalty(seconds);
        UpdateTimerUI();
    }
}