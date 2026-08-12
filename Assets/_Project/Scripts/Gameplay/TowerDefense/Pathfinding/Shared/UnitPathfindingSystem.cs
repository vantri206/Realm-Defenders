using System.Collections.Generic;
using UnityEngine;

public class UnitPathfindingSystem : MonoBehaviour
{
    private Dictionary<Vector3Int, byte> costGrid = new Dictionary<Vector3Int, byte>();
    private Dictionary<Vector3Int, FlowField> flowFields = new Dictionary<Vector3Int, FlowField>();

    public void BuildCostGrid(IReadOnlyDictionary<Vector3Int, CombatGridCell> gridCells)
    {
        costGrid.Clear();
        flowFields.Clear();

        foreach (var cell in gridCells)
        {
            Vector3Int cellPosition = cell.Key;
            CombatGridCell gridCell = cell.Value;

            byte cost = (byte)255;
            if (gridCell.CellStates.HasFlag(CombatGridCellStates.Blocked))
            {
                cost = 255;
            } 
            else if (gridCell.CellStates.HasFlag(CombatGridCellStates.Walkable))
            {
                cost = 1;
            } 

            costGrid[cellPosition] = cost;
        }
    }

    private FlowField GetOrCreateFlowField(Vector3Int targetCellPosition)
    {
        if (!flowFields.TryGetValue(targetCellPosition, out FlowField flowField))
        {
            flowField = new FlowField(targetCellPosition, costGrid);
            flowField.BuildFlowField();
            flowFields[targetCellPosition] = flowField;
        }
        return flowField;
    }

    public Vector2Int TryGetFlowFieldDirection(Vector3Int currentCellPosition, Vector3Int targetCellPosition)
    {
        FlowField flowField = GetOrCreateFlowField(targetCellPosition);
        if (flowField == null)
        {
            return Vector2Int.zero;
        }

        FlowFieldCell currentCell = flowField.GetCell(currentCellPosition);
        if (currentCell == null)
        {
            return Vector2Int.zero;
        }

        return currentCell.BestDirection;
    }

    public void TryGetLocalBFSDirection(Vector3Int currentCellPosition, Vector3Int targetCellPosition, out Vector2Int direction)
    {
        // Use Local BFS for hero pathfinding in local range
        direction = Vector2Int.zero;
    }
}
