using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class HueHuntMole : MonoBehaviour
{
    [SerializeField] private SpriteRenderer moleRenderer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip riseClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip fallClip;
    [SerializeField] private float hitFallDelay = 0.1f;

    private HueHuntMinigame gameManager;
    private Collider2D moleCollider;
    private Coroutine activeRoutine;
    private Vector3 hiddenPosition;
    private bool isResolvingHit;

    public bool IsRaised { get; private set; }
    public int TypeIndex { get; private set; }
    public Sprite CurrentSprite { get; private set; }

    private void Awake()
    {
        if (moleRenderer == null)
        {
            moleRenderer = GetComponent<SpriteRenderer>();
        }

        moleCollider = GetComponent<Collider2D>();
        hiddenPosition = transform.position;
    }

    public void Initialize(HueHuntMinigame manager)
    {
        gameManager = manager;
        ForceHide();
    }

    public void Show(Vector3 spawnPosition, int typeIndex, Sprite moleSprite, float visibleDuration)
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        TypeIndex = typeIndex;
        CurrentSprite = moleSprite;
        IsRaised = true;
        isResolvingHit = false;
        transform.position = spawnPosition;

        if (moleRenderer != null)
        {
            moleRenderer.sprite = moleSprite;
            moleRenderer.enabled = true;
        }

        if (moleCollider != null)
        {
            moleCollider.enabled = true;
        }

        PlayClip(riseClip);
        activeRoutine = StartCoroutine(AutoHideAfterDelay(visibleDuration));
    }

    public void HandleClick()
    {
        if (gameManager != null)
        {
            gameManager.HandleMoleClicked(this);
        }
    }

    public void HideAfterHit()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        isResolvingHit = true;
        IsRaised = false;

        if (moleCollider != null)
        {
            moleCollider.enabled = false;
        }

        activeRoutine = StartCoroutine(HideAfterHitRoutine());
    }

    public void ForceHide()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        CompleteHide();
    }

    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide();
    }

    private IEnumerator HideAfterHitRoutine()
    {
        PlayClip(hitClip);

        if (hitFallDelay > 0f)
        {
            yield return new WaitForSeconds(hitFallDelay);
        }

        PlayClip(fallClip);
        CompleteHide();
    }

    private void Hide()
    {
        if (!IsRaised && !isResolvingHit)
        {
            activeRoutine = null;
            return;
        }

        PlayClip(fallClip);
        CompleteHide();
    }

    private void CompleteHide()
    {
        IsRaised = false;
        TypeIndex = -1;
        CurrentSprite = null;
        isResolvingHit = false;
        transform.position = hiddenPosition;
        activeRoutine = null;

        if (moleRenderer != null)
        {
            moleRenderer.sprite = null;
            moleRenderer.enabled = false;
        }

        if (moleCollider != null)
        {
            moleCollider.enabled = false;
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
