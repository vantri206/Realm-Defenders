using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class CombatGridTilemapSource
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private CombatGridCellStates tilemapStates;
    [SerializeField] private bool isRequired = true;

    public Tilemap Tilemap => tilemap;
    public CombatGridCellStates TilemapStates => tilemapStates;
    public bool IsRequired => isRequired;
}

public class CombatGrid : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;

    [Header("Tilemap Sources")]
    [SerializeField] private List<CombatGridTilemapSource> tilemapSources = new List<CombatGridTilemapSource>();

    [Header("Gizmos")]
    [SerializeField] private bool drawGridGizmos = true;
    [SerializeField] private Color gridGizmoColor = new Color(0f, 1f, 1f, 0.35f);

    private readonly Dictionary<Vector3Int, CombatGridCell> cells = new Dictionary<Vector3Int, CombatGridCell>();
    private BoundsInt cellBounds;

    public Grid Grid => grid;
    public IReadOnlyDictionary<Vector3Int, CombatGridCell> Cells => cells;
    public BoundsInt CellBounds => cellBounds;
    public int CellCount => cells.Count;

    public void BuildGridMap()
    {
        ClearGrid();

        if (grid == null)
        {
            Debug.LogError($"[CombatGrid] requires a Grid reference.", this);
            return;
        }

        foreach (CombatGridTilemapSource source in tilemapSources)
        {
            if (!IsValidSource(source))
            {
                continue;
            }

            ResolveTilemapSource(source);
        }
    }

    public void ClearGrid()
    {
        cells.Clear();
        cellBounds = new BoundsInt();
    }

    public bool TryWorldToCell(Vector3 worldPosition, out Vector3Int cellPosition)
    {
        if (grid == null)
        {
            cellPosition = default;
            return false;
        }

        cellPosition = grid.WorldToCell(worldPosition);
        return true;
    }

    public bool WorldToCell(Vector3 worldPosition, out CombatGridCell cell)
    {
        if (!TryWorldToCell(worldPosition, out Vector3Int cellPosition))
        {
            cell = null;
            return false;
        }

        return cells.TryGetValue(cellPosition, out cell);
    }

    public bool TryGetCell(Vector3Int cellPosition, out CombatGridCell cell)
    {
        return cells.TryGetValue(cellPosition, out cell);
    }

    public Vector3 CellToWorldCenter(Vector3Int cellPosition)
    {
        return grid != null ? grid.GetCellCenterWorld(cellPosition) : Vector3.zero;
    }

    private void ResolveTilemapSource(CombatGridTilemapSource source)
    {
        Tilemap tilemap = source.Tilemap;
        tilemap.CompressBounds();

        foreach (Vector3Int cellPosition in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cellPosition))
            {
                continue;
            }

            CombatGridCell cell = GetCell(cellPosition);
            cell.AddState(source.TilemapStates);
            ExpandBounds(cellPosition);
        }
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

    private bool IsValidSource(CombatGridTilemapSource source)
    {
        if (source?.Tilemap != null)
        {
            return true;
        }

        if (source?.IsRequired == true)
        {
            Debug.LogError($"[CombatGrid] has a required tilemap source without a tilemap.", this);
        }

        return false;
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

    private BoundsInt GetTilemapSourceBounds()
    {
        BoundsInt sourceBounds = new BoundsInt();
        bool hasBounds = false;

        foreach (CombatGridTilemapSource source in tilemapSources)
        {
            Tilemap tilemap = source?.Tilemap;

            if (tilemap == null)
            {
                continue;
            }

            tilemap.CompressBounds();
            BoundsInt tilemapBounds = tilemap.cellBounds;

            if (tilemapBounds.size == Vector3Int.zero)
            {
                continue;
            }

            if (!hasBounds)
            {
                sourceBounds = tilemapBounds;
                hasBounds = true;
                continue;
            }

            Vector3Int min = Vector3Int.Min(sourceBounds.min, tilemapBounds.min);
            Vector3Int max = Vector3Int.Max(sourceBounds.max, tilemapBounds.max);
            sourceBounds.SetMinMax(min, max);
        }

        return sourceBounds;
    }

    private void OnDrawGizmos()
    {
        if (!drawGridGizmos || grid == null)
        {
            return;
        }

        bool useBuiltCellBounds = Application.isPlaying && cells.Count > 0;
        BoundsInt boundsToDraw = useBuiltCellBounds ? cellBounds : GetTilemapSourceBounds();

        if (boundsToDraw.size == Vector3Int.zero)
        {
            return;
        }

        Gizmos.color = gridGizmoColor;
        DrawGridGizmos(boundsToDraw);
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
