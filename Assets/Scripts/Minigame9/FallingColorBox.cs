using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class FallingColorBox : MonoBehaviour
{
    private SearchPasswordFromBoxMinigame gameManager;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rigidBody;
    private float missY;
    private bool isHandled;

    public int ColorIndex { get; private set; }
    public string HiddenLetter { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rigidBody = GetComponent<Rigidbody2D>();

        if (rigidBody == null)
        {
            rigidBody = gameObject.AddComponent<Rigidbody2D>();
        }

        rigidBody.gravityScale = 0f;
        rigidBody.freezeRotation = true;
        rigidBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        UpdateColliderToMatchSprite();
    }

    public void Initialize(SearchPasswordFromBoxMinigame manager, int colorIndex, Color color, string hiddenLetter, Vector2 launchVelocity, float gravityScale, float missPositionY)
    {
        gameManager = manager;
        ColorIndex = colorIndex;
        HiddenLetter = hiddenLetter;
        isHandled = false;
        missY = missPositionY;
        spriteRenderer.color = color;
        rigidBody.linearVelocity = launchVelocity;
        rigidBody.gravityScale = gravityScale;
        UpdateColliderToMatchSprite();
    }

    private void Update()
    {
        if (isHandled)
        {
            return;
        }

        if (transform.position.y <= missY)
        {
            isHandled = true;

            if (rigidBody != null)
            {
                rigidBody.linearVelocity = Vector2.zero;
                rigidBody.simulated = false;
            }

            if (gameManager != null)
            {
                gameManager.HandleBoxMissed(this);
            }
        }
    }

    public void HandleClick()
    {
        TryHandleClick();
    }

    public void RevealLetter()
    {
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
        gameManager.HandleBoxClicked(this);
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
