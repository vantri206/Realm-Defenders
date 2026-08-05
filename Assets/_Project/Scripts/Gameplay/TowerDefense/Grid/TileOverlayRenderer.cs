using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TileOverlayBrush
{
    [SerializeField] private TileOverlayType type;
    [SerializeField] private TileBase tile;
    [SerializeField] private Color color = new Color(1f, 1f, 1f, 1f);

    public TileOverlayType Type => type;
    public TileBase Tile => tile;
    public Color Color => color;
}

public class TileOverlayRenderer : MonoBehaviour
{
    [Header("Runtime Overlay Tilemaps")]
    [SerializeField] private Tilemap areaOverlayTilemap;
    [SerializeField] private Tilemap warningOverlayTilemap;
    [SerializeField] private Tilemap cellStateOverlayTilemap;

    [Header("Brushes")]
    [SerializeField] private List<TileOverlayBrush> brushes = new List<TileOverlayBrush>();

    private readonly Dictionary<TileOverlayLayer, Tilemap> overlayTilemaps = new Dictionary<TileOverlayLayer, Tilemap>();
    private readonly Dictionary<TileOverlayType, TileOverlayBrush> overlayBrushes = new Dictionary<TileOverlayType, TileOverlayBrush>();
    private readonly Dictionary<TileOverlayLayer, HashSet<Vector3Int>> paintedCell = new Dictionary<TileOverlayLayer, HashSet<Vector3Int>>();

    private void Awake()
    {
        SetupBrushesAndTilemaps();
    }

    private void OnValidate()
    {
        SetupBrushesAndTilemaps();
    }

    public void DrawCell(TileOverlayLayer layer, Vector3Int cellPosition, TileOverlayType type)
    {
        if (type == TileOverlayType.None)
        {
            ClearCell(layer, cellPosition);
            return;
        }

        if (!TryGetTilemap(layer, out Tilemap tilemap) || !TryGetBrush(type, out TileOverlayBrush brush))
        {
            return;
        }

        if (brush.Tile == null)
        {
            return;
        }

        tilemap.SetTile(cellPosition, brush.Tile);
        tilemap.SetTileFlags(cellPosition, TileFlags.None);
        tilemap.SetColor(cellPosition, brush.Color);
        GetPaintedCells(layer).Add(cellPosition);
    }

    public void DrawCells(TileOverlayLayer layer, IReadOnlyCollection<Vector3Int> cellPositions, TileOverlayType type)
    {
        if (cellPositions == null)
        {
            return;
        }

        foreach (Vector3Int cellPosition in cellPositions)
        {
            DrawCell(layer, cellPosition, type);
        }
    }

    public void ClearCell(TileOverlayLayer layer, Vector3Int cellPosition)
    {
        if (!TryGetTilemap(layer, out Tilemap tilemap))
        {
            return;
        }

        tilemap.SetTile(cellPosition, null);

        if (paintedCell.TryGetValue(layer, out HashSet<Vector3Int> paintedCells))
        {
            paintedCells.Remove(cellPosition);
        }
    }

    public void ClearLayer(TileOverlayLayer layer)
    {
        if (TryGetTilemap(layer, out Tilemap tilemap))
        {
            tilemap.ClearAllTiles();
        }

        if (paintedCell.TryGetValue(layer, out HashSet<Vector3Int> paintedCells))
        {
            paintedCells.Clear();
        }
    }

    public void ClearAllCells()
    {
        ClearLayer(TileOverlayLayer.Area);
        ClearLayer(TileOverlayLayer.Warning);
        ClearLayer(TileOverlayLayer.CellState);
    }

    private void SetupBrushesAndTilemaps()
    {
        overlayTilemaps.Clear();
        overlayBrushes.Clear();

        RegisterTilemap(TileOverlayLayer.Area, areaOverlayTilemap);
        RegisterTilemap(TileOverlayLayer.Warning, warningOverlayTilemap);
        RegisterTilemap(TileOverlayLayer.CellState, cellStateOverlayTilemap);

        for (int i = 0; i < brushes.Count; i++)
        {
            TileOverlayBrush brush = brushes[i];
            if (brush == null || brush.Type == TileOverlayType.None)
            {
                continue;
            }

            overlayBrushes[brush.Type] = brush;
        }
    }

    private void RegisterTilemap(TileOverlayLayer layer, Tilemap tilemap)
    {
        if (tilemap == null)
        {
            return;
        }

        overlayTilemaps[layer] = tilemap;
    }

    private bool TryGetTilemap(TileOverlayLayer layer, out Tilemap tilemap)
    {
        if (!overlayTilemaps.TryGetValue(layer, out tilemap) || tilemap == null)
        {
            SetupBrushesAndTilemaps();
        }

        return overlayTilemaps.TryGetValue(layer, out tilemap) && tilemap != null;
    }

    private bool TryGetBrush(TileOverlayType type, out TileOverlayBrush brush)
    {
        if (!overlayBrushes.TryGetValue(type, out brush))
        {
            SetupBrushesAndTilemaps();
        }

        return overlayBrushes.TryGetValue(type, out brush) && brush != null;
    }

    private HashSet<Vector3Int> GetPaintedCells(TileOverlayLayer layer)
    {
        if (!paintedCell.TryGetValue(layer, out HashSet<Vector3Int> paintedCells))
        {
            paintedCells = new HashSet<Vector3Int>();
            paintedCell.Add(layer, paintedCells);
        }

        return paintedCells;
    }
}
