using UnityEngine;

/// <summary>
/// Purely visual: tints the sprite when a piece is powered, and reverts when it isn't.
/// Attach to any 2D prefab that needs the glow (junctions, source, destination).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PowerLine : MonoBehaviour
{
    [SerializeField] private Color poweredColor = new Color(0.3f, 0.9f, 1f);
    [SerializeField] private Color unpoweredColor = Color.white;

    private SpriteRenderer spriteRenderer;
    private bool isPowered;

    private void Awake()
    {
        EnsureSpriteRenderer();
        ApplyState();
    }

    public void SetPowered(bool powered)
    {
        if (isPowered == powered) return;
        isPowered = powered;
        ApplyState();
    }

    private void ApplyState()
    {
        // Safe check to guarantee spriteRenderer is captured before changing color
        EnsureSpriteRenderer();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = isPowered ? poweredColor : unpoweredColor;
        }
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}