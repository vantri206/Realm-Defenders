using System.Collections.Generic;
using UnityEngine;

public class FlowField
{
    private const int STRAIGHT_COST = 10;
    private const int DIAGONAL_COST = 14; // Approximation of sqrt(2) * 10

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
        foreach (FlowFieldCell cell in cells.Values)
        {
            cell.ResetValue();
        }

        if (!cells.TryGetValue(targetCellPosition, out FlowFieldCell flowTargetCell) || flowTargetCell.Cost == GameplayConstants.BLOCKED_COST)
        {
            return;
        }

        SortedDictionary<int, Queue<FlowFieldCell>> openCellsByCost = new SortedDictionary<int, Queue<FlowFieldCell>>();

        void Enqueue(FlowFieldCell cell, int integrationCost)
        {
            if (!openCellsByCost.TryGetValue(integrationCost, out Queue<FlowFieldCell> cellsWithSameCost))
            {
                cellsWithSameCost = new Queue<FlowFieldCell>();
                openCellsByCost.Add(integrationCost, cellsWithSameCost);
            }

            cellsWithSameCost.Enqueue(cell);
        }

        flowTargetCell.SetIntegrationCost(0);
        flowTargetCell.SetNodeState(SearchNodeState.Open);
        Enqueue(flowTargetCell, 0);

        while (openCellsByCost.Count > 0)
        {
            KeyValuePair<int, Queue<FlowFieldCell>> lowestCostEntry;
            using (SortedDictionary<int, Queue<FlowFieldCell>>.Enumerator enumerator = openCellsByCost.GetEnumerator())
            {
                enumerator.MoveNext();
                lowestCostEntry = enumerator.Current;
            }

            int queuedIntegrationCost = lowestCostEntry.Key;
            Queue<FlowFieldCell> lowestCostCells = lowestCostEntry.Value;
            FlowFieldCell currentCell = lowestCostCells.Dequeue();
            if (lowestCostCells.Count == 0)
            {
                openCellsByCost.Remove(queuedIntegrationCost);
            }

            if (queuedIntegrationCost != currentCell.IntegrationCost)
            {
                continue;
            }

            currentCell.SetNodeState(SearchNodeState.Closed);

            for (int i = 0; i < GridDirectionHelpers.EightWayDirectionCount; i++)
            {
                Vector2Int offset = GridDirectionHelpers.EightWayOffsets[i];
                Vector3Int neighborCellPosition = currentCell.CellPosition + (Vector3Int)offset;

                if (!cells.TryGetValue(neighborCellPosition, out FlowFieldCell neighborCell) ||
                    neighborCell.Cost == GameplayConstants.BLOCKED_COST ||
                    !CanMoveDiagonal(currentCell.CellPosition, offset))
                {
                    continue;
                }

                int directionCost = IsDiagonalDirection(offset) ? DIAGONAL_COST : STRAIGHT_COST;
                int targetCellCost = neighborCell.Cost;
                int moveCost = directionCost * targetCellCost;
                int newIntegrationCost = currentCell.IntegrationCost + moveCost;

                if (!neighborCell.SetIntegrationCost(newIntegrationCost))
                {
                    continue;
                }

                neighborCell.SetBestDirection(-offset);
                neighborCell.SetNodeState(SearchNodeState.Open);
                Enqueue(neighborCell, newIntegrationCost);
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
