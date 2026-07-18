using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    private float elapsedTime;
    private bool isLevelOver;

    private int currentDoorIndex = 0;
    private Coroutine audioLoopCoroutine;
    private bool isMusicPausedForInteraction = false;

    private void Start()
    {
        bool storyModeActive = false;

        // Retrieve the static 'isStoryMode' field via reflection from MainMenu
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

        elapsedTime = 0f;
        MinigamePenaltyTracker.ResetPenalty();
        UpdateTimerUI();

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

        AssignRhymesToDoors();
        PlayDoorMusic(0);
    }

    private void AssignRhymesToDoors()
    {
        if (rhymes.Length < doors.Length)
        {
            return;
        }

        List<int> used = new List<int>();

        for (int i = 0; i < doors.Length; i++)
        {
            int index;

            do
            {
                index = Random.Range(0, rhymes.Length);
            }
            while (used.Contains(index));

            used.Add(index);

            doors[i].AssignData(rhymes[index]);
        }
    }


    public void PlayDoorMusic(int doorIndex)
    {
        if (doorIndex < 0 || doorIndex >= doors.Length)
            return;

        currentDoorIndex = doorIndex;
        isMusicPausedForInteraction = false;

        if (globalAudioSource == null)
            return;

        if (audioLoopCoroutine != null)
            StopCoroutine(audioLoopCoroutine);

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
            if (globalAudioSource.isPlaying)
                globalAudioSource.Pause();
        }
        else
        {
            if (!globalAudioSource.isPlaying && globalAudioSource.clip != null)
                globalAudioSource.UnPause();
        }
    }

    public void StopMusic()
    {
        if (audioLoopCoroutine != null)
            StopCoroutine(audioLoopCoroutine);

        if (globalAudioSource != null)
            globalAudioSource.Stop();
    }

    public void OnDoorCompleted(int doorIndex)
    {

        if (doorIndex + 1 < doors.Length)
        {
            PlayDoorMusic(doorIndex + 1);
        }
        else
        {
            StopMusic();
        }
    }

    private void Update()
    {
        if (isLevelOver)
            return;

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
        if (isLevelOver)
            return;

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
        if (timeRemainingText == null)
            return;

        int m = Mathf.FloorToInt(elapsedTime / 60f);
        int s = Mathf.FloorToInt(elapsedTime % 60f);

        timeRemainingText.text = m > 0 ? $"{m}:{s:00}" : s.ToString();
    }

    public float GetElapsedTime() => elapsedTime;

    /// <summary>
    /// Adds a time penalty (e.g. wrong password on a door) to the elapsed
    /// time used by the on-screen timer, and mirrors it into
    /// MinigamePenaltyTracker so DoorSceneTrigger's final score calculation
    /// picks it up too.
    /// </summary>
    public void AddTimePenalty(float seconds)
    {
        if (seconds <= 0f)
            return;

        elapsedTime += seconds;
        MinigamePenaltyTracker.AddPenalty(seconds);

        UpdateTimerUI();

    }
}