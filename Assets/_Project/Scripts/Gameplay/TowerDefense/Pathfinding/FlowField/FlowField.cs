using System.Collections.Generic;
using UnityEngine;

public class FlowField
{
    private Vector3Int targetCellPosition;
    private Dictionary<Vector3Int, FlowFieldCell> cells;

    private Queue<FlowFieldCell> openCells = new Queue<FlowFieldCell>();
    
    public Vector3Int TargetCellPosition => targetCellPosition;
    public IReadOnlyDictionary<Vector3Int, FlowFieldCell> Cells => cells;

    private const int BLOCKED_COST = 255; // Assuming 255 is the maximum cost for a blocked cell

    public FlowField(Vector3Int targetCellPosition, Dictionary<Vector3Int, byte> costGrid)
    {
        this.targetCellPosition = targetCellPosition;
        cells = new Dictionary<Vector3Int, FlowFieldCell>();
        foreach (var cell in costGrid)
        {
            cells[cell.Key] = new FlowFieldCell(cell.Key, cell.Value);
        }
    }

    public FlowFieldCell GetCell(Vector3Int cellPosition)
    {
        if (cells.TryGetValue(cellPosition, out FlowFieldCell cell))
        {
            return cell;
        }
        return null;
    }

    public void BuildFlowField()
    {
        openCells.Clear();
        if (cells.TryGetValue(targetCellPosition, out FlowFieldCell targetCell) && targetCell.Cost < BLOCKED_COST)
        {
            targetCell.SetIntegrationCost(0);
            openCells.Enqueue(targetCell);
        }
        else
        {
            Debug.LogWarning($"[FlowField] Target cell position {targetCellPosition} is not in the flow field.");
            return;
        }

        while (openCells.Count > 0)
        {
            FlowFieldCell currentCell = openCells.Dequeue();

            if (currentCell.NodeState == SearchNodeState.Closed)
            {
                continue;
            }
            currentCell.SetNodeState(SearchNodeState.Closed);
            for (int  i = 0; i < GridDirectionHelpers.EightWayDirectionCount; i++)
            {
                Vector2Int offset = GridDirectionHelpers.EightWayOffsets[i];
                Vector3Int neighborCellPosition = currentCell.CellPosition + (Vector3Int)offset;

                if (cells.TryGetValue(neighborCellPosition, out FlowFieldCell neighborCell))
                {
                    if (neighborCell.Cost == BLOCKED_COST || !CanMoveDiagonal(currentCell.CellPosition, offset))
                    {
                        continue;
                    }

                    bool isUpdated = neighborCell.SetIntegrationCost(currentCell.IntegrationCost + neighborCell.Cost);
                    if (isUpdated)
                    {
                        neighborCell.SetBestDirection(-offset); // negative to point towards the current cell
                        neighborCell.SetNodeState(SearchNodeState.Open);
                        openCells.Enqueue(neighborCell);
                    }
                }
            }
        }
    }

    private bool IsDiagonalDirection(Vector2Int direction)
    {
        return direction.x != 0 && direction.y != 0;
    }

    private bool CanMoveDiagonal(Vector3Int currentCellPosition, Vector2Int diagonalDirection)
    {
        if (!IsDiagonalDirection(diagonalDirection))
        {
            return true;
        }

        Vector3Int horizontalNeighbor = currentCellPosition + new Vector3Int(diagonalDirection.x, 0, 0);
        Vector3Int verticalNeighbor = currentCellPosition + new Vector3Int(0, diagonalDirection.y, 0);

        return IsCellWalkable(horizontalNeighbor) && IsCellWalkable(verticalNeighbor);
    }

    private bool IsCellWalkable(Vector3Int cellPosition)
    {
        if (cells.TryGetValue(cellPosition, out FlowFieldCell cell))
        {
            return cell.Cost < BLOCKED_COST;
        }
        return false;
    }
}