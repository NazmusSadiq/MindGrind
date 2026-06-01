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

    [Header("Gameplay")]
    [SerializeField] private float levelDuration = 60f;
    [SerializeField] private float initialNormalDuration = 10f;
    [SerializeField] private float directionChangeInterval = 10f;

    private float elapsedTime;
    private float nextDirectionChangeTime;
    private float timeRemaining;
    private bool isLevelOver;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogError("Level1_Manager could not find a PlayerController.", this);
            enabled = false;
            return;
        }

        playerController.SetControlDirection(PlayerController.ControlDirection.Up);
        ShowArrow(PlayerController.ControlDirection.Up);
        timeRemaining = Mathf.Max(0f, levelDuration);
        UpdateTimerUI();
        nextDirectionChangeTime = Mathf.Max(0f, initialNormalDuration);
    }

    private void Update()
    {
        if (isLevelOver || playerController.IsDead)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            TriggerGameOver();
            return;
        }

        elapsedTime += Time.deltaTime;

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

    private void TriggerGameOver()
    {
        isLevelOver = true;
        timeRemaining = 0f;
        UpdateTimerUI();

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
            int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timeRemainingText.text = secondsLeft.ToString();
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
