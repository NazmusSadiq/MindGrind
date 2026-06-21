using UnityEngine;

/// <summary>
/// A plain point along a straight or gently curved stretch of rail.
/// No rotation logic of its own - CarMover faces the direction of travel automatically
/// when heading toward (or away from) one of these.
/// </summary>
public class Waypoint : MonoBehaviour
{
    [Tooltip("The next node the train heads to after this one (Waypoint, JunctionNode, or HouseNode).")]
    [SerializeField] private Transform nextNode;

    public Transform NextNode => nextNode;
}
