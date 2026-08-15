using System.Collections.Generic;
using UnityEngine;

public class LocalBFSPathfinding
{
    private readonly Queue<Vector3Int> openCells = new Queue<Vector3Int>();
    private readonly HashSet<Vector3Int> visitedCells = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> targetCells = new HashSet<Vector3Int>();

    private readonly Dictionary<Vector3Int, Vector3Int> parents = new Dictionary<Vector3Int, Vector3Int>();

    private byte blockedCost = GameplayConstants.BLOCKED_COST;

    public bool TryGetDirection(IReadOnlyDictionary<Vector3Int, byte> costGrid, int searchRange,
                            Vector3Int currentCellPosition, Vector3Int targetCellPosition, out Vector2Int direction)
    {
        IReadOnlyCollection<Vector3Int> cells = new List<Vector3Int> { targetCellPosition };
        return TryGetDirection(costGrid, searchRange, currentCellPosition, cells, out direction);
    }

    public bool TryGetDirection(IReadOnlyDictionary<Vector3Int, byte> costGrid, int searchRange,
                            Vector3Int currentCellPosition, IReadOnlyCollection<Vector3Int> targetCellPositions, 
                            out Vector2Int direction)
    {
        direction = Vector2Int.zero;

        if (costGrid == null || !IsCellWalkable(costGrid, currentCellPosition) || targetCellPositions == null || targetCellPositions.Count == 0)
        {
            return false;
        }

        openCells.Clear();
        visitedCells.Clear();
        targetCells.Clear();
        parents.Clear();

        foreach (Vector3Int targetCellPosition in targetCellPositions)
        {
            if (IsCellWalkable(costGrid, targetCellPosition))
            {
                targetCells.Add(targetCellPosition);
            }
        }

        if (targetCells.Count == 0)
        {
            return false;
        }

        if (targetCells.Contains(currentCellPosition))
        {
            return true;
        }

        openCells.Enqueue(currentCellPosition);
        visitedCells.Add(currentCellPosition);

        while (openCells.Count > 0)
        {
            Vector3Int current = openCells.Dequeue();

            for (int i = 0; i < GridDirectionHelpers.EightWayDirectionCount; i++)
            {
                Vector2Int offset = GridDirectionHelpers.EightWayOffsets[i];
                Vector3Int neighbor = current + new Vector3Int(offset.x, offset.y, 0);

                if (visitedCells.Contains(neighbor) || !IsWithinSearchRange(currentCellPosition, neighbor, searchRange))
                {
                    continue;
                }

                if (!IsCellWalkable(costGrid, neighbor))
                {
                    continue;
                }

                if (GridDirectionHelpers.IsDiagonalMove(offset) && !CanMoveDiagonal(costGrid, current, offset))
                {
                    continue;
                }

                visitedCells.Add(neighbor);
                parents[neighbor] = current;

                if (targetCells.Contains(neighbor))
                {
                    direction = GetStartMoveDirection(currentCellPosition, neighbor);
                    return direction != Vector2Int.zero;
                }

                openCells.Enqueue(neighbor);
            }
        }

        return false;
    }

    private Vector2Int GetStartMoveDirection(Vector3Int startCellPosition, Vector3Int targetCellPosition)
    {
        Vector3Int nextCellPosition = GetStartMoveCell(startCellPosition, targetCellPosition);
        if (nextCellPosition == startCellPosition)
        {
            return Vector2Int.zero;
        }

        Vector3Int offset = nextCellPosition - startCellPosition;
        return new Vector2Int(Mathf.Clamp(offset.x, -1, 1), Mathf.Clamp(offset.y, -1, 1));
    }

    private Vector3Int GetStartMoveCell(Vector3Int startCellPosition, Vector3Int currentCellPosition)
    {
        if  (parents.TryGetValue(currentCellPosition, out Vector3Int parent) && parent != startCellPosition)
        {
            return GetStartMoveCell(startCellPosition, parent);
        }

        return currentCellPosition;
    }

    private bool IsWithinSearchRange(Vector3Int startCellPosition, Vector3Int finishCellPosition, int searchRange)
    {
        if (searchRange <= 0)
        {
            return true;
        }

        int deltaX = Mathf.Abs(finishCellPosition.x - startCellPosition.x);
        int deltaY = Mathf.Abs(finishCellPosition.y - startCellPosition.y);
        return deltaX <= searchRange && deltaY <= searchRange;
    }

    private bool IsCellWalkable(IReadOnlyDictionary<Vector3Int, byte> costGrid, Vector3Int cellPosition)
    {
        return costGrid.TryGetValue(cellPosition, out byte cost) && cost < blockedCost;
    }

    private bool CanMoveDiagonal(IReadOnlyDictionary<Vector3Int, byte> costGrid, Vector3Int currentCellPosition, Vector2Int direction)
    {
        if (direction.x == 0 || direction.y == 0)
        {
            return true;
        }

        Vector3Int horizontalNeighbor = currentCellPosition + new Vector3Int(direction.x, 0, 0);
        Vector3Int verticalNeighbor = currentCellPosition + new Vector3Int(0, direction.y, 0);
        return IsCellWalkable(costGrid, horizontalNeighbor) && IsCellWalkable(costGrid, verticalNeighbor);
    }
}
