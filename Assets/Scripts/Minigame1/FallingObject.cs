using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class FallingObject : MonoBehaviour
{
    private SymbolMatchMinigame gameManager;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private float fallSpeed;
    private float missY;
    private bool isHandled;

    public int SymbolIndex { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        UpdateColliderToMatchSprite();
    }

    public void Initialize(SymbolMatchMinigame manager, int symbolIndex, Sprite sprite, float speed, float missPositionY)
    {
        gameManager = manager;
        SymbolIndex = symbolIndex;
        fallSpeed = speed;
        missY = missPositionY;
        spriteRenderer.sprite = sprite;
        UpdateColliderToMatchSprite();
    }

    private void Update()
    {
        if (isHandled)
        {
            return;
        }

        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        if (transform.position.y <= missY)
        {
            isHandled = true;

            if (gameManager != null)
            {
                gameManager.HandleObjectMissed(this);
            }
        }
    }

    public void HandleClick()
    {
        TryHandleClick();
    }

    public void Remove()
    {
        Destroy(gameObject);
    }

    private void TryHandleClick()
    {
        if (isHandled || gameManager == null)
        {
            return;
        }

        isHandled = true;
        gameManager.HandleObjectClicked(this);
    }

    private void UpdateColliderToMatchSprite()
    {
        if (boxCollider == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        boxCollider.offset = spriteRenderer.sprite.bounds.center;
        boxCollider.size = spriteRenderer.sprite.bounds.size;
    }
}
