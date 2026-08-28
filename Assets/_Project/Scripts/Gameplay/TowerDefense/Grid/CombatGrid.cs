using System.Collections.Generic;
using UnityEngine;

public class CombatGrid : MonoBehaviour
{
    [Header("Gizmos")]
    [SerializeField] private bool drawGridGizmos = true;
    [SerializeField] private Color gridGizmoColor = new Color(0f, 1f, 1f, 0.35f);

    private Grid grid;
    private readonly Dictionary<Vector3Int, CombatGridCell> cells = new Dictionary<Vector3Int, CombatGridCell>();
    private BoundsInt cellBounds;

    public Grid Grid => grid;
    public IReadOnlyDictionary<Vector3Int, CombatGridCell> Cells => cells;
    public Vector3 CellSize => grid != null ? grid.cellSize : Vector3.zero;

    public bool BuildGridMap(Grid grid, IReadOnlyList<CombatGridCellDefinition> cellDefinitions)
    {
        ClearGrid();
        this.grid = grid;

        if (grid == null)
        {
            Debug.LogError("[CombatGrid] Grid is required to build the combat grid.", this);
            return false;
        }

        if (cellDefinitions == null || cellDefinitions.Count == 0)
        {
            Debug.LogError("[CombatGrid] Grid cell definitions are required to build the combat grid.", this);
            return false;
        }

        for (int i = 0; i < cellDefinitions.Count; i++)
        {
            CombatGridCellDefinition definition = cellDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            CombatGridCell cell = GetCell(definition.CellPosition);
            cell.AddState(definition.CellStates);
            ExpandBounds(definition.CellPosition);
        }

        return cells.Count > 0;
    }

    public void ClearGrid()
    {
        cells.Clear();
        cellBounds = new BoundsInt();
    }

    public bool TryGetCell(Vector3Int cellPosition, out CombatGridCell cell)
    {
        return cells.TryGetValue(cellPosition, out cell);
    }

    public bool TryWorldToCellPosition(Vector3 worldPosition, out Vector3Int cellPosition)
    {
        if (!TryWorldToCell(worldPosition, out CombatGridCell cell))
        {
            cellPosition = default;
            return false;
        }

        cellPosition = cell.CellPosition;
        return true;
    }

    public bool TryWorldToCell(Vector3 worldPosition, out CombatGridCell cell)
    {
        if (grid == null)
        {
            cell = null;
            return false;
        }

        Vector3Int cellPosition = grid.WorldToCell(worldPosition);
        return cells.TryGetValue(cellPosition, out cell);
    }

    public bool TryCellToWorldCenter(Vector3Int cellPosition, out Vector3 worldPosition)
    {
        if (grid == null || !cells.ContainsKey(cellPosition))
        {
            worldPosition = Vector3.zero;
            return false;
        }   

        worldPosition = grid.GetCellCenterWorld(cellPosition);
        return true;
    }

    public bool TryCellToWorldBottomCenter(Vector3Int cellPosition, out Vector3 worldPosition)
    {
        if (grid == null || !cells.ContainsKey(cellPosition))
        {
            worldPosition = Vector3.zero;
            return false;
        }

        worldPosition = grid.GetCellCenterWorld(cellPosition) - new Vector3(0f, CellSize.y * 0.5f, 0f);
        return true;
    }

    public bool TryCellToWorldCenter(CombatGridCell cell, out Vector3 worldPosition)
    {
        if (cell == null)
        {
            worldPosition = Vector3.zero;
            return false;
        }

        return TryCellToWorldCenter(cell.CellPosition, out worldPosition);
    }

    public bool TryCellToWorldBottomCenter(CombatGridCell cell, out Vector3 worldPosition)
    {
        if (cell == null)
        {
            worldPosition = Vector3.zero;
            return false;
        }

        return TryCellToWorldBottomCenter(cell.CellPosition, out worldPosition);
    }

    private CombatGridCell GetCell(Vector3Int cellPosition)
    {
        if (!cells.TryGetValue(cellPosition, out CombatGridCell cell))
        {
            cell = new CombatGridCell(cellPosition);
            cells.Add(cellPosition, cell);
        }

        return cell;
    }

    public IReadOnlyCollection<CombatGridCell> GetAllDeployableCells()
    {
        List<CombatGridCell> deployableCells = new List<CombatGridCell>();

        foreach (var cell in cells.Values)
        {
            if (cell.CanDeployHero())
            {
                deployableCells.Add(cell);
            }
        }

        return deployableCells;
    }

    private void ExpandBounds(Vector3Int cellPosition)
    {
        if (cells.Count == 1)
        {
            cellBounds = new BoundsInt(cellPosition, Vector3Int.one);
            return;
        }

        Vector3Int min = Vector3Int.Min(cellBounds.min, cellPosition);
        Vector3Int max = Vector3Int.Max(cellBounds.max, cellPosition + Vector3Int.one);
        cellBounds.SetMinMax(min, max);
    }

    private void OnDrawGizmos()
    {
        if (!drawGridGizmos || grid == null)
        {
            return;
        }

        if (cells.Count == 0 || cellBounds.size == Vector3Int.zero)
        {
            return;
        }

        Gizmos.color = gridGizmoColor;
        DrawGridGizmos(cellBounds);
    }

    private void DrawGridGizmos(BoundsInt boundsToDraw)
    {
        for (int x = boundsToDraw.xMin; x <= boundsToDraw.xMax; x++)
        {
            Vector3Int startCell = new Vector3Int(x, boundsToDraw.yMin, 0);
            Vector3Int endCell = new Vector3Int(x, boundsToDraw.yMax, 0);
            Gizmos.DrawLine(grid.CellToWorld(startCell), grid.CellToWorld(endCell));
        }

        for (int y = boundsToDraw.yMin; y <= boundsToDraw.yMax; y++)
        {
            Vector3Int startCell = new Vector3Int(boundsToDraw.xMin, y, 0);
            Vector3Int endCell = new Vector3Int(boundsToDraw.xMax, y, 0);
            Gizmos.DrawLine(grid.CellToWorld(startCell), grid.CellToWorld(endCell));
        }
    }
}
