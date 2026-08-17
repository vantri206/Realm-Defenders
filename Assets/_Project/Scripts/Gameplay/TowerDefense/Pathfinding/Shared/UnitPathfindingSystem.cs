using System.Collections.Generic;
using UnityEngine;

public class UnitPathfindingSystem : MonoBehaviour
{
    private readonly Dictionary<UnitMovementType, Dictionary<Vector3Int, byte>> costGrids = new Dictionary<UnitMovementType, Dictionary<Vector3Int, byte>>();
    private readonly Dictionary<UnitMovementType, Dictionary<Vector3Int, FlowField>> flowFields = new Dictionary<UnitMovementType, Dictionary<Vector3Int, FlowField>>();
    private LocalBFSPathfinding localBFSPathfinding = new LocalBFSPathfinding();

    public void BuildCostGrid(IReadOnlyDictionary<Vector3Int, CombatGridCell> gridCells)
    {
        costGrids.Clear();
        flowFields.Clear();

        BuildCostGrid(gridCells, UnitMovementType.Ground);
        BuildCostGrid(gridCells, UnitMovementType.Flying);
    }

    private void BuildCostGrid(IReadOnlyDictionary<Vector3Int, CombatGridCell> gridCells, UnitMovementType movementType)
    {
        Dictionary<Vector3Int, byte> costGrid = new Dictionary<Vector3Int, byte>();
        foreach (KeyValuePair<Vector3Int, CombatGridCell> cell in gridCells)
        {
            costGrid[cell.Key] = UnitMovementRules.GetPathfindingCost(movementType, cell.Value);
        }

        costGrids[movementType] = costGrid;
        flowFields[movementType] = new Dictionary<Vector3Int, FlowField>();
    }

    private FlowField GetOrCreateFlowField(Vector3Int targetCellPosition, UnitMovementType movementType)
    {
        if (!costGrids.TryGetValue(movementType, out Dictionary<Vector3Int, byte> costGrid) ||
            !flowFields.TryGetValue(movementType, out Dictionary<Vector3Int, FlowField> movementFlowFields))
        {
            Debug.LogError($"[UnitPathfindingSystem] Cost grid for movement type '{movementType}' has not been built.", this);
            return null;
        }

        if (!movementFlowFields.TryGetValue(targetCellPosition, out FlowField flowField))
        {
            flowField = new FlowField(targetCellPosition, costGrid);
            flowField.BuildFlowField();
            movementFlowFields[targetCellPosition] = flowField;
        }
        return flowField;
    }

    public Vector2Int TryGetFlowFieldDirection(Vector3Int currentCellPosition, Vector3Int targetCellPosition, UnitMovementType movementType)
    {
        FlowField flowField = GetOrCreateFlowField(targetCellPosition, movementType);
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

    public bool TryGetLocalBFSDirection(Vector3Int currentCellPosition, Vector3Int targetCellPosition, int searchRange, out Vector2Int direction)
    {
        direction = Vector2Int.zero;
        if (!TryGetGroundCostGrid(out Dictionary<Vector3Int, byte> costGrid))
        {
            return false;
        }

        return localBFSPathfinding.TryGetDirection(costGrid, searchRange, currentCellPosition, targetCellPosition, out direction);
    }

    public bool TryGetLocalBFSDirection(Vector3Int currentCellPosition, IReadOnlyCollection<Vector3Int> targetCellPositions, 
                                        int searchRange, out Vector2Int direction)
    {
        direction = Vector2Int.zero;
        if (!TryGetGroundCostGrid(out Dictionary<Vector3Int, byte> costGrid))
        {
            return false;
        }

        return localBFSPathfinding.TryGetDirection(costGrid, searchRange, currentCellPosition, targetCellPositions, out direction);
    }

    private bool TryGetGroundCostGrid(out Dictionary<Vector3Int, byte> costGrid)
    {
        return costGrids.TryGetValue(UnitMovementType.Ground, out costGrid);
    }
}
