using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Level2_Manager : MonoBehaviour
{
    [System.Serializable]
    public struct CameraKeyframe
    {
        [Tooltip("The world position (X, Y, Z) the camera should reach.")]
        public Vector3 position;
        [Tooltip("The directional look orientation (X, Y, Z Euler angles) the camera should match.")]
        public Vector3 rotationAngles;
        [Tooltip("The absolute timestamp (in seconds from cinematic start) when the camera must arrive at this point.")]
        public float timeStamp;
    }

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_Text powerSourcesText;
    [SerializeField] private Camera mainCamera;

    [Header("Door Unlocking")]
    [SerializeField] private DoorSceneTrigger exitGate;

    [Header("Box Setup")]
    [SerializeField] private List<BoxController> allBoxes = new List<BoxController>();

    [Header("Custom Timeline Settings")]
    [SerializeField] private List<CameraKeyframe> cameraTimeline = new List<CameraKeyframe>();

    private float elapsedTime;
    private bool isLevelOver;
    private bool cinematicActive;
    private int powerSourcesFound = 0;

    private void Start()
    {
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

        if (exitGate != null)
        {
            exitGate.canOpen = false;
        }

        DistributeBoxContents();

        elapsedTime = 0f;
        UpdateTimerUI();
        UpdatePowerSourcesUI();

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

    // NEW: Public interface method to reduce time when an enemy is eliminated
    public void ReduceElapsedTime(float amount)
    {
        if (isLevelOver || cinematicActive) return;

        elapsedTime = Mathf.Max(0f, elapsedTime - amount);
        UpdateTimerUI();
        Debug.Log($"[Time Bonus] Reduced elapsed match time by {amount} seconds.");
    }

    private void DistributeBoxContents()
    {
        if (allBoxes.Count != 10)
        {
            Debug.LogWarning($"Level2_Manager expects exactly 10 boxes, but found {allBoxes.Count} references assigned in Inspector.", this);
        }

        List<bool> contentPool = new List<bool>();
        for (int i = 0; i < allBoxes.Count; i++)
        {
            contentPool.Add(i < allBoxes.Count / 2);
        }

        for (int i = 0; i < contentPool.Count; i++)
        {
            bool temp = contentPool[i];
            int randomIndex = Random.Range(i, contentPool.Count);
            contentPool[i] = contentPool[randomIndex];
            contentPool[randomIndex] = temp;
        }

        for (int i = 0; i < allBoxes.Count; i++)
        {
            if (allBoxes[i] != null)
            {
                allBoxes[i].SetupBox(contentPool[i], this);
            }
        }
    }

    public void StartCinematicReveal()
    {
        if (cameraTimeline == null || cameraTimeline.Count == 0)
        {
            Debug.LogError("[Cinematic] Cannot start reveal sequence because no Camera Keyframes are configured in the Inspector!", this);
            return;
        }
        StartCoroutine(TimelineCinematicRoutine());
    }

    private IEnumerator TimelineCinematicRoutine()
    {
        cinematicActive = true;
        playerController.SetGameStarted(false);
        Debug.Log("[Cinematic] Sequence started. Player tracking frozen. Revealing all valid Power Cells and text prompts.");

        CameraFollowLockedRotation followScript = mainCamera.GetComponent<CameraFollowLockedRotation>();
        if (followScript != null)
        {
            followScript.enabled = false;
        }

        Vector3 originalCamPosition = mainCamera.transform.position;
        Quaternion originalCamRotation = mainCamera.transform.rotation;

        ToggleAllBoxesCinematic(true);

        float timelineTimer = 0f;
        int currentKeyframeIndex = 0;

        Vector3 startPos = originalCamPosition;
        Quaternion startRot = originalCamRotation;
        float startTime = 0f;

        while (currentKeyframeIndex < cameraTimeline.Count)
        {
            CameraKeyframe targetFrame = cameraTimeline[currentKeyframeIndex];
            float targetTime = targetFrame.timeStamp;
            Quaternion targetRotation = Quaternion.Euler(targetFrame.rotationAngles);

            if (targetTime <= startTime)
            {
                mainCamera.transform.position = targetFrame.position;
                mainCamera.transform.rotation = targetRotation;

                startPos = mainCamera.transform.position;
                startRot = mainCamera.transform.rotation;
                startTime = targetTime;
                currentKeyframeIndex++;
                continue;
            }

            while (timelineTimer < targetTime)
            {
                timelineTimer += Time.deltaTime;

                float segmentProgress = Mathf.Clamp01((timelineTimer - startTime) / (targetTime - startTime));

                mainCamera.transform.position = Vector3.Lerp(startPos, targetFrame.position, segmentProgress);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRotation, segmentProgress);

                yield return null;
            }

            mainCamera.transform.position = targetFrame.position;
            mainCamera.transform.rotation = targetRotation;

            startPos = mainCamera.transform.position;
            startRot = mainCamera.transform.rotation;
            startTime = targetTime;
            currentKeyframeIndex++;
        }

        Debug.Log("[Cinematic] Final coordinate node reached. Holding visibility state for 2 seconds padding loop.");
        yield return new WaitForSeconds(2f);

        ToggleAllBoxesCinematic(false);

        Debug.Log("[Cinematic] Returning camera transformation parameters back to the player layout.");
        float returnLerp = 0f;
        Vector3 endTimelinePos = mainCamera.transform.position;
        Quaternion endTimelineRot = mainCamera.transform.rotation;

        while (returnLerp < 1f)
        {
            returnLerp += Time.deltaTime * 3f;
            mainCamera.transform.position = Vector3.Lerp(endTimelinePos, originalCamPosition, returnLerp);
            mainCamera.transform.rotation = Quaternion.Slerp(endTimelineRot, originalCamRotation, returnLerp);
            yield return null;
        }

        mainCamera.transform.position = originalCamPosition;
        mainCamera.transform.rotation = originalCamRotation;

        if (followScript != null)
        {
            followScript.enabled = true;
        }

        cinematicActive = false;
        playerController.SetGameStarted(true);
        Debug.Log("[Cinematic] Sequence complete. Control returned back to the player gameplay inputs.");
    }

    private void ToggleAllBoxesCinematic(bool show)
    {
        foreach (BoxController box in allBoxes)
        {
            if (box != null)
            {
                box.ToggleRevealIndicator(show);
            }
        }
    }

    public void RegisterPowerSourceFound()
    {
        powerSourcesFound++;
        Debug.Log($"Power Source Collected! Count: {powerSourcesFound}/5");

        UpdatePowerSourcesUI();

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

    private void UpdatePowerSourcesUI()
    {
        if (powerSourcesText != null)
        {
            powerSourcesText.text = $"{powerSourcesFound}/5";
        }
    }
}