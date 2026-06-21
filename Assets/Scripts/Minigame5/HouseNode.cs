using UnityEngine;

/// <summary>
/// A destination. Assign colorId 0-8 in the Inspector and the sprite tint
/// is applied automatically (both in Edit Mode via OnValidate and at runtime).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HouseNode : MonoBehaviour
{
    [SerializeField, Range(0, ColorPalette.ColorCount - 1)] private int colorId;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int ColorId => colorId;

    private void Awake()
    {
        ApplyColor();
    }

    private void OnValidate()
    {
        // Lets you see the correct tint immediately in the Scene view while placing houses.
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = ColorPalette.GetColor(colorId);
        }
    }
}
