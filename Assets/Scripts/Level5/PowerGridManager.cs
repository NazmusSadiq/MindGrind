using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PowerGridManager : MonoBehaviour
{
    [Header("Puzzle Events")]
    public UnityEvent OnPuzzleSolved;

    [Header("Shuffle Layout")]
    public bool shuffleOnStart = true;

    // NEW: Public getter property allowing Level5_Manager to check if this puzzle is currently solved
    public bool IsSolved => alreadySolved;

    private readonly Dictionary<Vector2Int, IPowerNode> grid = new Dictionary<Vector2Int, IPowerNode>();
    private readonly List<IPowerNode> allNodes = new List<IPowerNode>();
    private IPowerNode sourceNode;
    private IPowerNode destinationNode;
    private bool alreadySolved;

    private void Start()
    {
        BuildGrid();
        if (shuffleOnStart) ShuffleJunctions();
        Recompute();
    }

    private void BuildGrid()
    {
        grid.Clear();
        allNodes.Clear();

        // 1. Scan the scene for all nodes
        var nodesInScene = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var behaviour in nodesInScene)
        {
            if (behaviour is IPowerNode node)
            {
                // 2. Only track this node if it belongs to THIS specific manager
                if (node.MyManager == this)
                {
                    grid[node.GridPosition] = node;
                    allNodes.Add(node);

                    if (behaviour is PowerSource) sourceNode = node;
                    if (behaviour is PowerDestination) destinationNode = node;
                }
            }
        }
    }

    private void ShuffleJunctions()
    {
        foreach (var node in allNodes)
        {
            if (node.MyManager == this)
            {
                if (node is IInteractable interactableObject)
                {
                    int randomRotations = Random.Range(0, 4);

                    for (int i = 0; i < randomRotations; i++)
                    {                        
                        interactableObject.Interact(null);
                    }
                }
            }
        }
    }

    public void Recompute()
    {
        if (sourceNode == null) return;

        HashSet<IPowerNode> poweredNodes = new HashSet<IPowerNode>();
        Queue<IPowerNode> processingQueue = new Queue<IPowerNode>();

        poweredNodes.Add(sourceNode);
        processingQueue.Enqueue(sourceNode);

        while (processingQueue.Count > 0)
        {
            IPowerNode currentNode = processingQueue.Dequeue();

            foreach (Direction currentHandDir in currentNode.GetOpenHands())
            {
                Vector2Int neighborCoordinates = currentNode.GridPosition + currentHandDir.ToVector();

                // Proximity Check inside this manager's local isolated grid
                if (grid.TryGetValue(neighborCoordinates, out IPowerNode neighborNode))
                {
                    Direction expectedOppositeDirection = currentHandDir.Opposite();

                    bool hasMatchingHandshake = false;
                    foreach (Direction neighborHandDir in neighborNode.GetOpenHands())
                    {
                        if (neighborHandDir == expectedOppositeDirection)
                        {
                            hasMatchingHandshake = true;
                            break;
                        }
                    }

                    if (hasMatchingHandshake && poweredNodes.Add(neighborNode))
                    {
                        processingQueue.Enqueue(neighborNode);
                    }
                }
            }
        }

        // Apply visual updates only to this manager's puzzle pieces
        foreach (var node in allNodes)
        {
            node.SetPowered(poweredNodes.Contains(node));
        }

        // Evaluate win state updates dynamically 
        bool solvedNow = destinationNode != null && poweredNodes.Contains(destinationNode);

        if (solvedNow && !alreadySolved)
        {
            OnPuzzleSolved?.Invoke();
            Debug.Log($"[Puzzle Manager] {gameObject.name} has been solved!");
        }

        // Keeps 'alreadySolved' synced perfectly so Level5_Manager knows instantly if a player un-solves it
        alreadySolved = solvedNow;
    }
}