using UnityEngine;

/// <summary>
/// Single source of truth for the 9 train/house colors.
/// id: 0 Black, 1 White, 2 Violet, 3 Pink, 4 Blue, 5 Green, 6 Yellow, 7 Orange, 8 Red.
/// </summary>
public static class ColorPalette
{
    public const int ColorCount = 9;

    private static readonly Color[] Colors =
    {
        new Color(0.000f, 0.000f, 0.000f), // 0 - Black
        new Color(1.000f, 1.000f, 1.000f), // 1 - White
        new Color(0.561f, 0.000f, 1.000f), // 2 - Violet
        new Color(1.000f, 0.412f, 0.706f), // 3 - Pink
        new Color(0.000f, 0.000f, 1.000f), // 4 - Blue
        new Color(0.000f, 1.000f, 0.000f), // 5 - Green
        new Color(1.000f, 1.000f, 0.000f), // 6 - Yellow
        new Color(1.000f, 0.498f, 0.000f), // 7 - Orange
        new Color(1.000f, 0.000f, 0.000f), // 8 - Red
    };

    public static Color GetColor(int colorId)
    {
        if (colorId < 0 || colorId >= Colors.Length)
        {
            return Color.white;
        }

        return Colors[colorId];
    }

    public static int GetRandomColorId()
    {
        return Random.Range(0, ColorCount);
    }
}
