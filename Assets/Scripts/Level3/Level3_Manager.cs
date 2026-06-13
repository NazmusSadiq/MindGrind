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

    private void Start()
    {
        MainMenu mainMenuFallback = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenuFallback != null)
            mainMenuFallback.SetStoryMode(true);

        if (playerController == null)
            playerController = Object.FindFirstObjectByType<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("PlayerController missing", this);
            enabled = false;
            return;
        }

        elapsedTime = 0f;
        UpdateTimerUI();

        MainMenu.PauseGameAndShowDetailsPanel();

        AssignRhymesToDoors();
        PlayDoorMusic(0);
    }

    private void AssignRhymesToDoors()
    {
        if (rhymes.Length < doors.Length)
        {
            Debug.LogError("Not enough rhymes for doors!");
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

            doors[i].AssignData(rhymes[index].password);
        }
    }

    // ================= GLOBAL AUDIO CONTROL =================

    public void PlayDoorMusic(int doorIndex)
    {
        if (doorIndex < 0 || doorIndex >= rhymes.Length)
            return;

        currentDoorIndex = doorIndex;

        if (globalAudioSource == null)
            return;

        globalAudioSource.clip = rhymes[doorIndex].rhymeClip;
        globalAudioSource.loop = true;
        globalAudioSource.Play();
    }

    public void StopMusic()
    {
        if (globalAudioSource != null)
            globalAudioSource.Stop();
    }

    public void OnDoorCompleted(int doorIndex)
    {
        Debug.Log($"Door {doorIndex} completed");

        if (doorIndex + 1 < doors.Length)
        {
            PlayDoorMusic(doorIndex + 1);
        }
        else
        {
            StopMusic();
            Debug.Log("All doors completed. Music stopped.");
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
        Debug.Log($"Level 3 Completed in: {elapsedTime:F2}");
    }

    private void TriggerGameOver()
    {
        isLevelOver = true;
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
}