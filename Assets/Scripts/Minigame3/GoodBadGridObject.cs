using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class GoodBadGridObject : MonoBehaviour
{
    private GoodBadMinigame gameManager;
    private Collider2D objectCollider;
    private SpriteRenderer spriteRenderer;

    public bool IsGood { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsHandled { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        objectCollider = GetComponent<Collider2D>();
    }

    public void Initialize(GoodBadMinigame manager)
    {
        gameManager = manager;
        Clear();
    }

    public void Configure(Sprite sprite, bool isGood)
    {
        IsGood = isGood;
        IsActive = true;
        IsHandled = false;

        spriteRenderer.sprite = sprite;
        spriteRenderer.enabled = sprite != null;
        objectCollider.enabled = sprite != null;
    }

    public void HandleClick()
    {
        if (!IsActive || IsHandled || gameManager == null)
        {
            return;
        }

        IsHandled = true;
        gameManager.HandleGridObjectClicked(this);
    }

    public void Clear()
    {
        IsGood = false;
        IsActive = false;
        IsHandled = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
            spriteRenderer.enabled = false;
        }

        if (objectCollider != null)
        {
            objectCollider.enabled = false;
        }
    }
}
