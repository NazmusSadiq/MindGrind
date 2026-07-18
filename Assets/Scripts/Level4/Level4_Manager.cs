using TMPro;
using UnityEngine;

public class Level1_Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private GameObject upArrow;
    [SerializeField] private GameObject rightArrow;
    [SerializeField] private GameObject downArrow;
    [SerializeField] private GameObject leftArrow;

    // ================= AUDIO SFX =================
    [Header("Audio Configurations")]
    [SerializeField] private AudioClip directionChangeSound;

    [Header("Gameplay")]
    [SerializeField] private float initialNormalDuration = 10f;
    [SerializeField] private float directionChangeInterval = 10f;

    private float elapsedTime;
    private float nextDirectionChangeTime;
    private bool isLevelOver;

    private PlayerController.ControlDirection lastDirection = PlayerController.ControlDirection.Up;

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
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            enabled = false;
            return;
        }

        playerController.SetControlDirection(PlayerController.ControlDirection.Up);
        ShowArrow(PlayerController.ControlDirection.Up);
        lastDirection = PlayerController.ControlDirection.Up; // Initialize tracking

        elapsedTime = 0f;
        UpdateTimerUI();
        nextDirectionChangeTime = Mathf.Max(0f, initialNormalDuration);

        // Only pause and show details if story mode is already active
        if (storyModeActive)
        {
            MainMenu.PauseGameAndShowDetailsPanel();
        }
        else
        {
            playerController.SetGameStarted(true);
            playerController.EnableGameplayInput(true);
        }
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

        if (elapsedTime < nextDirectionChangeTime)
        {
            return;
        }

        ChangeDirection();
        nextDirectionChangeTime += Mathf.Max(0.01f, directionChangeInterval);
    }

    // NEW: Public interface method to reduce time when an enemy is eliminated
    public void ReduceElapsedTime(float amount)
    {
        if (isLevelOver) return;

        elapsedTime = Mathf.Max(0f, elapsedTime - amount);
        UpdateTimerUI();
    }

    private void ChangeDirection()
    {
        PlayerController.ControlDirection newDirection;

        // Loop until a different direction from the last one is chosen
        do
        {
            newDirection = (PlayerController.ControlDirection)Random.Range(0, 4);
        } while (newDirection == lastDirection);

        // Update tracking variable
        lastDirection = newDirection;

        playerController.SetControlDirection(newDirection);
        ShowArrow(newDirection);

        // Play the direction shift notification sound in 2D
        PlaySound2D(directionChangeSound);
    }

    // Explicit 2D Sound Spawner to play the notification clip cleanly at runtime
    private void PlaySound2D(AudioClip clip)
    {
        if (clip != null)
        {
            GameObject sfxObj = new GameObject("Temp_DirectionChange_SFX");
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f; // Forces 2D Full Volume
            source.volume = 1f;
            source.Play();
            Destroy(sfxObj, clip.length);
        }
    }

    public void TriggerLevelComplete()
    {
        if (isLevelOver) return;

        isLevelOver = true;

        ShowArrow(null);
    }

    private void TriggerGameOver()
    {
        isLevelOver = true;

        if (playerController != null)
        {
            playerController.ShowGameOverMenu();
        }
        else if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
        }
    }

    private void UpdateTimerUI()
    {
        if (timeRemainingText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);

            if (minutes > 0)
            {
                timeRemainingText.text = string.Format("{0}:{1:00}", minutes, seconds);
            }
            else
            {
                timeRemainingText.text = seconds.ToString();
            }
        }
    }

    private void ShowArrow(PlayerController.ControlDirection? direction)
    {
        SetArrowState(upArrow, direction == PlayerController.ControlDirection.Up);
        SetArrowState(rightArrow, direction == PlayerController.ControlDirection.Right);
        SetArrowState(downArrow, direction == PlayerController.ControlDirection.Down);
        SetArrowState(leftArrow, direction == PlayerController.ControlDirection.Left);
    }

    private void SetArrowState(GameObject arrow, bool isActive)
    {
        if (arrow != null)
        {
            arrow.SetActive(isActive);
        }
    }
}