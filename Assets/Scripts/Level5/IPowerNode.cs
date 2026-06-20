using UnityEngine;

public interface IPowerNode
{
    Vector2Int GridPosition { get; }
    PowerGridManager MyManager { get; } // Links the piece explicitly to a specific puzzle grid set
    Direction[] GetOpenHands();
    void SetPowered(bool powered);
}