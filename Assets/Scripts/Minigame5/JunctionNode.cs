using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class JunctionNode : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite linearSprite;
    [SerializeField] private Sprite bentSprite;
    [SerializeField] private bool startBent;

    [Header("Linear Path")]
    [Tooltip("World-space Z rotation (degrees) the train snaps to while travelling this leg.")]
    [SerializeField] private float linearAngle;
    [Tooltip("The Target GameObject prefab (Junction or House) when linear.")]
    [SerializeField] private GameObject linearTarget;

    [Header("Bent Path")]
    [Tooltip("World-space Z rotation (degrees) the train snaps to while travelling this leg.")]
    [SerializeField] private float bentAngle;
    [Tooltip("The Target GameObject prefab (Junction or House) when bent.")]
    [SerializeField] private GameObject bentTarget;

    private bool isBent;

    private void Awake()
    {
        isBent = startBent;
        UpdateVisual();
    }

    public GameObject GetActiveTarget()
    {
        return isBent ? bentTarget : linearTarget;
    }

    public float GetActiveAngle()
    {
        return isBent ? bentAngle : linearAngle;
    }

    public void HandleClick()
    {
        isBent = !isBent;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // 1. Swap the sprite asset based on the state
        spriteRenderer.sprite = isBent ? bentSprite : linearSprite;

        // 2. Actually apply the rotation to the junction's Transform!
        float targetAngle = isBent ? bentAngle : linearAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }
}