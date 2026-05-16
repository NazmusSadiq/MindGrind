using UnityEngine;

public class HueHuntHole : MonoBehaviour
{
    [SerializeField] private SpriteRenderer discRenderer;
    [SerializeField] private Transform moleSpawnPoint;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color blueColor = Color.blue;

    public Vector3 MoleSpawnPosition => moleSpawnPoint != null ? moleSpawnPoint.position : transform.position;

    public void Initialize()
    {
        ResetDisc();
    }

    public void SetDisc(bool isRed)
    {
        if (discRenderer == null)
        {
            return;
        }

        discRenderer.color = isRed ? redColor : blueColor;
    }

    public void ResetDisc()
    {
        if (discRenderer == null)
        {
            return;
        }

        discRenderer.color = defaultColor;
    }
}
