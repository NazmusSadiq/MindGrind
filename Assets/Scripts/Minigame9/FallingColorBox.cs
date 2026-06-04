using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class FallingColorBox : MonoBehaviour
{
    private SearchPasswordFromBoxMinigame gameManager;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private float fallSpeed;
    private float missY;
    private bool isHandled;

    public int ColorIndex { get; private set; }
    public string HiddenLetter { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        UpdateColliderToMatchSprite();
    }

    public void Initialize(SearchPasswordFromBoxMinigame manager, int colorIndex, Color color, string hiddenLetter, float speed, float missPositionY)
    {
        gameManager = manager;
        ColorIndex = colorIndex;
        HiddenLetter = hiddenLetter;
        fallSpeed = speed;
        missY = missPositionY;
        spriteRenderer.color = color;
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
