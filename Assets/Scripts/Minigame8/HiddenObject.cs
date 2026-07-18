using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HiddenObject : MonoBehaviour
{
    private HiddenObjectGame gameManager;
    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D polygonCollider;

    public string ObjectName { get; private set; }
    public bool IsTarget { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        polygonCollider = GetComponent<PolygonCollider2D>();

        if (polygonCollider == null)
        {
            polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        }
    }

    /// <summary>
    /// Called immediately after spawning.
    /// </summary>
    public void Initialize(
        HiddenObjectGame manager,
        string objectName,
        Sprite sprite,
        bool isTarget,
        Vector3 scale)
    {
        gameManager = manager;

        ObjectName = objectName;
        IsTarget = isTarget;

        spriteRenderer.sprite = sprite;

        ApplyFixedHeightScale(1f);

        RefreshCollider();
    }

    /// <summary>
    /// Rebuild collider so it matches current sprite.
    /// </summary>
    private void RefreshCollider()
    {
        if (polygonCollider != null)
        {
            Destroy(polygonCollider);
        }

        polygonCollider = gameObject.AddComponent<PolygonCollider2D>();

        polygonCollider.isTrigger = true;
    }

    private void ApplyFixedHeightScale(float targetHeight)
    {
        if (spriteRenderer.sprite == null)
            return;

        Bounds spriteBounds = spriteRenderer.sprite.bounds;

        float originalHeight = spriteBounds.size.y;

        if (originalHeight <= 0)
            return;

        float scaleFactor = targetHeight / originalHeight;

        transform.localScale = Vector3.one * scaleFactor;
    }

    public void HandleClick()
    {
        if (gameManager != null)
        {
            gameManager.OnObjectClicked(this);
        }
    }

    public void SetTarget(bool value)
    {
        IsTarget = value;
    }

    public void Remove()
    {
        Destroy(gameObject);
    }
}
