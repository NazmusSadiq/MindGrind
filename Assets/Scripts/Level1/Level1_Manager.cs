using TMPro;
using UnityEngine;

public class Level1_Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text timeRemainingText; // Displays elapsed time now
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private GameObject upArrow;
    [SerializeField] private GameObject rightArrow;
    [SerializeField] private GameObject downArrow;
    [SerializeField] private GameObject leftArrow;

    [Header("Gameplay")]
    [SerializeField] private float initialNormalDuration = 10f;
    [SerializeField] private float directionChangeInterval = 10f;

    private float elapsedTime;
    private float nextDirectionChangeTime;
    private bool isLevelOver;

    private void Start()
    {
        MainMenu mainMenuFallback = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenuFallback != null)
        {
            mainMenuFallback.SetStoryMode(true);
        }
        else
        {
            typeof(MainMenu).GetMethod("SetStoryMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?
                .Invoke(new GameObject("Temp_Menu_Initializer").AddComponent<MainMenu>(), new object[] { true });

        }

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogError("Level1_Manager could not find a PlayerController.", this);
            enabled = false;
            return;
        }

        playerController.SetControlDirection(PlayerController.ControlDirection.Up);
        ShowArrow(PlayerController.ControlDirection.Up);

        elapsedTime = 0f;
        UpdateTimerUI();
        nextDirectionChangeTime = Mathf.Max(0f, initialNormalDuration);

        MainMenu.PauseGameAndShowDetailsPanel();
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

    private void ChangeDirection()
    {
        PlayerController.ControlDirection direction = (PlayerController.ControlDirection)Random.Range(0, 4);

        playerController.SetControlDirection(direction);
        ShowArrow(direction);
    }

    public void TriggerLevelComplete()
    {
        if (isLevelOver) return;

        isLevelOver = true;
        Debug.Log($"Level Completed in: {elapsedTime:F2} seconds!");

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