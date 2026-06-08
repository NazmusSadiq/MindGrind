using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Level2_Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private Camera mainCamera;

    [Header("Door Unlocking")]
    [SerializeField] private DoorSceneTrigger exitGate;

    [Header("Box Setup")]
    [SerializeField] private List<BoxController> allBoxes = new List<BoxController>();

    [Header("Cinematic Settings")]
    [SerializeField] private float viewDurationPerBox = 1.5f;
    [SerializeField] private float cameraPanSpeed = 4f;

    private float elapsedTime;
    private bool isLevelOver;
    private bool cinematicActive;
    private int powerSourcesFound = 0;

    private void Start()
    {
        // Force story mode tracking rules
        MainMenu mainMenuFallback = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenuFallback != null)
        {
            mainMenuFallback.SetStoryMode(true);
        }

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogError("Level2_Manager could not find a PlayerController.", this);
            enabled = false;
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Keep the exit gate locked safely at the start
        if (exitGate != null)
        {
            exitGate.canOpen = false;
        }

        // Configure the box roles safely before the reveal phase runs
        DistributeBoxContents();

        elapsedTime = 0f;
        UpdateTimerUI();

        // Halt player state instantly and trigger the UI overview card
        MainMenu.PauseGameAndShowDetailsPanel();
    }

    private void Update()
    {
        if (isLevelOver || cinematicActive) return;

        if (playerController.IsDead)
        {
            TriggerGameOver();
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    private void DistributeBoxContents()
    {
        if (allBoxes.Count != 10)
        {
            Debug.LogWarning($"Level2_Manager expects exactly 10 boxes, but found {allBoxes.Count} references assigned in Inspector.", this);
        }

        // Create an explicit layout pool containing 5 items of each type
        List<bool> contentPool = new List<bool>();
        for (int i = 0; i < allBoxes.Count; i++)
        {
            // First half true (Power Source), second half false (Explosive)
            contentPool.Add(i < allBoxes.Count / 2);
        }

        // Fisher-Yates Shuffle array data elements perfectly for unpredictable generation mechanics
        for (int i = 0; i < contentPool.Count; i++)
        {
            bool temp = contentPool[i];
            int randomIndex = Random.Range(i, contentPool.Count);
            contentPool[i] = contentPool[randomIndex];
            contentPool[randomIndex] = temp;
        }

        // Pass calculated variables over down to the targeted targets directly
        for (int i = 0; i < allBoxes.Count; i++)
        {
            if (allBoxes[i] != null)
            {
                allBoxes[i].SetupBox(contentPool[i], this);
            }
        }
    }

    // Called from MainMenu via PlayerController.EnableGameplayInput when UI closes
    public void StartCinematicReveal()
    {
        StartCoroutine(CinematicPanRoutine());
    }

    private IEnumerator CinematicPanRoutine()
    {
        cinematicActive = true;

        // Temporarily intercept input permissions away until full pan finishes
        playerController.SetGameStarted(false);

        Vector3 originalCamPosition = mainCamera.transform.position;
        Quaternion originalCamRotation = mainCamera.transform.rotation;

        // Separate and find the specific boxes containing our required power sources
        List<BoxController> powerBoxes = allBoxes.FindAll(b => b != null && b.IsPowerSource);

        foreach (BoxController targetBox in powerBoxes)
        {
            // Offset the target position so the camera looks down at the box from a slight distance
            Vector3 targetLookPos = targetBox.transform.position + new Vector3(0, 4f, -4f);
            Quaternion targetRotation = Quaternion.LookRotation(targetBox.transform.position - targetLookPos);

            // Smoothly pan camera towards targeted coordinate frame
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * cameraPanSpeed;
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetLookPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, t);
                yield return null;
            }

            // Uncover what is inside the box visually while framing it
            targetBox.ToggleRevealIndicator(true);
            yield return new WaitForSeconds(viewDurationPerBox);
            targetBox.ToggleRevealIndicator(false);
        }

        // Smoothly return back inside standard position context space behind the player frame
        float returnTime = 0f;
        while (returnTime < 1f)
        {
            returnTime += Time.deltaTime * cameraPanSpeed;
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, originalCamPosition, returnTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, originalCamRotation, returnTime);
            yield return null;
        }

        mainCamera.transform.position = originalCamPosition;
        mainCamera.transform.rotation = originalCamRotation;

        // Turn gameplay system tracking calculations back alive cleanly
        cinematicActive = false;
        playerController.SetGameStarted(true);
    }

    public void RegisterPowerSourceFound()
    {
        powerSourcesFound++;
        Debug.Log($"Power Source Collected! Count: {powerSourcesFound}/5");

        if (powerSourcesFound >= 5)
        {
            UnlockExitGate();
        }
    }

    private void UnlockExitGate()
    {
        if (exitGate != null)
        {
            exitGate.canOpen = true;
            Debug.Log("All 5 power sources recovered! Exit Portal Gate is now unlocked.");
        }
    }

    public void TriggerLevelComplete()
    {
        if (isLevelOver) return;
        isLevelOver = true;
        Debug.Log($"Level 2 Completed in: {elapsedTime:F2} seconds!");
    }

    private void TriggerGameOver()
    {
        isLevelOver = true;
        if (playerController != null)
        {
            playerController.ShowGameOverMenu();
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
}