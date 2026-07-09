using UnityEngine;
using UnityEngine.UI;

public class ShapeSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject shapePrefab;

    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite triangleSprite;
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Sprite pentagonSprite;

    [Header("Colors")]
    [SerializeField] private Color[] objectColors = new Color[4];

    [Header("Layout")]

    [SerializeField]
    private float offset = 40f;

    [SerializeField]
    private float shapeSize = 55f;

    public void GenerateShapes(
        OddOneOut.ShapeType shape,
        int colorIndex,
        int count)
    {
        Debug.Log($"Offset = {offset}");

        Clear();

        Sprite sprite = GetSprite(shape);

        Color color = objectColors[colorIndex];

        Vector2[] positions = GetPositions(count);

        for (int i = 0; i < positions.Length; i++)
        {
            Debug.Log($"Generated Position {i}: {positions[i]}");
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(shapePrefab);
            obj.transform.SetParent(transform, false);

            RectTransform rect =
                obj.GetComponent<RectTransform>();

            Image img =
                obj.GetComponent<Image>();

            rect.sizeDelta =
                new Vector2(shapeSize, shapeSize);

            rect.anchoredPosition =
                positions[i];

            Debug.Log($"Assigned: {rect.anchoredPosition}");

            img.sprite = sprite;

            img.color = color;
        }
    }

    private void Clear()
    {
        for (int i = transform.childCount - 1;
            i >= 0;
            i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private Sprite GetSprite(OddOneOut.ShapeType shape)
    {
        switch (shape)
        {
            case OddOneOut.ShapeType.Circle:
                return circleSprite;

            case OddOneOut.ShapeType.Triangle:
                return triangleSprite;

            case OddOneOut.ShapeType.Square:
                return squareSprite;

            default:
                return pentagonSprite;
        }
    }

    private Vector2[] GetPositions(int count)
    {
        switch (count)
        {
            //-------------------------
            // 1
            //-------------------------

            case 1:

                return new Vector2[]
                {
                    Vector2.zero
                };

            //-------------------------
            // 2
            //-------------------------

            case 2:

                return new Vector2[]
                {
                    new Vector2(0, offset),

                    new Vector2(0,-offset)
                };

            //-------------------------
            // 3
            //-------------------------

            case 3:

                return new Vector2[]
                {
                    new Vector2(-offset, offset),

                    new Vector2(offset, offset),

                    new Vector2(0,-offset)
                };

            //-------------------------
            // 4
            //-------------------------

            default:

                return new Vector2[]
                {
                    new Vector2(-offset, offset),

                    new Vector2(offset, offset),

                    new Vector2(-offset,-offset),

                    new Vector2(offset,-offset)
                };
        }
    }
}