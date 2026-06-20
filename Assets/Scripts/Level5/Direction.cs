using UnityEngine;

public enum Direction
{
    North, // Up
    East,  // Right
    South, // Down
    West   // Left
}

public static class DirectionExtensions
{
    /// <summary>Returns the exact opposite direction for checking handshakes.</summary>
    public static Direction Opposite(this Direction d)
    {
        return (Direction)(((int)d + 2) % 4);
    }

    /// <summary>Shifts the data direction clockwise.</summary>
    public static Direction RotateCW(this Direction d)
    {
        return (Direction)(((int)d + 1) % 4);
    }

    /// <summary>
    /// Updated to match your custom grid:
    /// (0,2) (1,2) (2,2)
    /// (0,1) (1,1) (2,1)
    /// (0,0) (1,0) (2,0)
    /// </summary>
    public static Vector2Int ToVector(this Direction d)
    {
        switch (d)
        {
            case Direction.North: return new Vector2Int(0, 1);   // Up (Adds to Y)
            case Direction.East: return new Vector2Int(1, 0);   // Right (Adds to X)
            case Direction.South: return new Vector2Int(0, -1);  // Down (Subtracts from Y)
            case Direction.West: return new Vector2Int(-1, 0);  // Left (Subtracts from X)
            default: return Vector2Int.zero;
        }
    }
}