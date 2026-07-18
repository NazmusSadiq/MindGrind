using UnityEngine;
using UnityEngine.InputSystem;

public class BoxController : MonoBehaviour, IInteractable
{
    [Header("Visual Indicators (UI)")]
    [SerializeField] private GameObject promptCanvas;

    [Header("Sprite Graphic Swaps")]
    [SerializeField] private SpriteRenderer boxSpriteRenderer;
    [SerializeField] private Sprite coveredSprite;
    [SerializeField] private Sprite powerCellSprite;
    [SerializeField] private Sprite emptySprite;

    [Header("Audio SFX")]
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failureSound;

    private Level2_Manager levelManager;
    private bool isPowerSource;
    private bool isOpened;
    private bool isPlayerInRange; 

    public bool IsPowerSource => isPowerSource;

    public void SetupBox(bool containsPower, Level2_Manager manager)
    {
        isPowerSource = containsPower;
        levelManager = manager;
        isOpened = false;
        isPlayerInRange = false;

        SetBoxSprite(coveredSprite);

        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    private void Start()
    {
        if (promptCanvas != null) promptCanvas.SetActive(false);

        if (boxSpriteRenderer == null)
        {
            boxSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void Interact(GameObject interactor)
    {
        if (isOpened)
        {
            return;
        }

        OpenBox();
    }

    private void OpenBox()
    {
        isOpened = true;
        HidePrompt();

        SetBoxSprite(emptySprite);

        if (isPowerSource)
        {

            PlaySound2D(successSound);

            if (levelManager != null) levelManager.RegisterPowerSourceFound();
        }
        else
        {

            PlaySound2D(failureSound);

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) player.TakeDamage(25);
        }
    }

    private void PlaySound2D(AudioClip clip)
    {
        if (clip != null)
        {
            GameObject sfxObj = new GameObject("Temp_Box_SFX");
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f; // Forces 2D Full Volume
            source.volume = 1f;
            source.Play();
            Destroy(sfxObj, clip.length);
        }
    }

    public void ShowPrompt()
    {
        isPlayerInRange = true;
        if (isOpened) return;

        if (promptCanvas != null) promptCanvas.SetActive(true);
    }

    public void HidePrompt()
    {
        isPlayerInRange = false; 
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    public void ToggleRevealIndicator(bool showReveal)
    {
        if (isOpened) return;

        if (showReveal)
        {
            if (isPowerSource)
            {
                SetBoxSprite(powerCellSprite);
            }

            if (promptCanvas != null) promptCanvas.SetActive(true);
        }
        else
        {
            SetBoxSprite(coveredSprite);

            if (promptCanvas != null)
            {
                promptCanvas.SetActive(isPlayerInRange);
            }
        }
    }

    private void SetBoxSprite(Sprite targetSprite)
    {
        if (boxSpriteRenderer != null && targetSprite != null)
        {
            boxSpriteRenderer.sprite = targetSprite;
        }
    }
}