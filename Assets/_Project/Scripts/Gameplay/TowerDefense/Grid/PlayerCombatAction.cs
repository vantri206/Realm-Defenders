using UnityEngine;

public enum PlayerCombatActionMode
{
    None,
    DeployingHero,
    SelectedDeployedHero,
    RelocatingHero,
}

[DisallowMultipleComponent]
public class PlayerCombatAction : MonoBehaviour
{
    [Header("References")]
    private Camera mainCamera;
    private CombatGrid combatGrid;
    private HeroDeploymentSystem heroDeploymentSystem;
    private TileOverlayRenderer tileOverlayRenderer;
    private GhostHeroView ghostHeroView;

    private PlayerCombatActionMode currentMode = PlayerCombatActionMode.None;

    private HeroInstance deployingHero;

    private Vector3Int hoveredCellPosition;
    private CombatGridCell hoveredCell;
    private Vector3Int previousHoveredCellPosition;
    private CombatGridCell previousHoveredCell;

    private Vector2Int currentDeployDirection = Vector2Int.left;

    private bool isInitialized;

    public void Initialize(Camera mainCamera, CombatGrid combatGrid, HeroDeploymentSystem heroDeploymentSystem, TileOverlayRenderer tileOverlayRenderer, GhostHeroView ghostHeroView)
    {
        this.mainCamera = mainCamera;
        this.combatGrid = combatGrid;
        this.heroDeploymentSystem = heroDeploymentSystem;
        this.tileOverlayRenderer = tileOverlayRenderer;
        this.ghostHeroView = ghostHeroView;

        currentMode = PlayerCombatActionMode.None;
        ResetHoverState();

        if (this.mainCamera == null || this.combatGrid == null || this.heroDeploymentSystem == null || this.tileOverlayRenderer == null)
        {
            isInitialized = false;
            return;
        }

        isInitialized = true;
    }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    public void ChangeMode(PlayerCombatActionMode mode)
    {
        if (!isInitialized)
        {
            return;
        }

        ClearCurrentModeOverlays();
        currentMode = mode;
        ResetHoverState();

        switch (currentMode)
        {
            case PlayerCombatActionMode.DeployingHero:
                DrawDeployableCells();
                break;
            case PlayerCombatActionMode.SelectedDeployedHero:
                break;
            case PlayerCombatActionMode.RelocatingHero:
                break;
            default:
                break;
        }
    }

    public void RefreshMode()
    {
        switch (currentMode)
        {
            case PlayerCombatActionMode.DeployingHero:
                break;
            case PlayerCombatActionMode.SelectedDeployedHero:
                break;
            case PlayerCombatActionMode.RelocatingHero:
                break;
            default:
                break;
        }

        ChangeMode(PlayerCombatActionMode.None);
    }

    public void UpdateHover(Vector2 screenPosition)
    {
        if (GetCellFromScreenPosition(screenPosition, out CombatGridCell cell))
        {
            if (hoveredCellPosition != cell.CellPosition)
            {
                previousHoveredCell = hoveredCell;
                previousHoveredCellPosition = hoveredCellPosition;
            }

            hoveredCell = cell;
            hoveredCellPosition = cell.CellPosition;
        }
        else
        {
            previousHoveredCell = hoveredCell;
            previousHoveredCellPosition = hoveredCellPosition;

            hoveredCell = null;
            hoveredCellPosition = Vector3Int.zero;
        }

        switch (currentMode)
        {
            case PlayerCombatActionMode.DeployingHero:
                UpdateHoverCell();
                break;
            case PlayerCombatActionMode.SelectedDeployedHero:
                break;
            case PlayerCombatActionMode.RelocatingHero:
                break;
            default:

                break;
        }
    }

    public void SellSelectedHero()
    {
        
    }

    public void RelocateSelectedHero()
    {
        
    }

    public void DrawDeployableCells()
    {
        if (combatGrid == null || tileOverlayRenderer == null)
        {
            return;
        }

        tileOverlayRenderer.ClearLayer(TileOverlayLayer.CellState);

        foreach (var cell in combatGrid.GetAllDeployableCells())
        {
            tileOverlayRenderer.DrawCell(TileOverlayLayer.CellState, cell.CellPosition, TileOverlayType.Deployable);
        }
    }

    public void DrawAttackRangePreview(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null || tileOverlayRenderer == null)
        {
            return;
        }

        tileOverlayRenderer.ClearLayer(TileOverlayLayer.CellState);
    }

    public void UpdateHoverCell()
    {
        if (hoveredCell == null)
        {
            RestorePreviousHoverCell();
            previousHoveredCell = null;
            return;
        }

        if (previousHoveredCell != null && hoveredCellPosition == previousHoveredCellPosition)
        {
            return;
        }

        if (previousHoveredCell != null && hoveredCellPosition != previousHoveredCellPosition)
        {
            RestorePreviousHoverCell();
            previousHoveredCell = null;
        }

        ApplyCurrentHoverCell();
    }

    public void RestorePreviousHoverCell()
    {
        if (previousHoveredCell == null)
        {
            return;
        }
        
        if (currentMode == PlayerCombatActionMode.None)
        {
            tileOverlayRenderer.ClearCell(TileOverlayLayer.CellState, previousHoveredCellPosition);
        }
        else if (currentMode == PlayerCombatActionMode.DeployingHero)
        {
            if (previousHoveredCell.CanDeployHero())
            {
                tileOverlayRenderer.DrawCell(TileOverlayLayer.CellState, previousHoveredCellPosition, TileOverlayType.Deployable);
                
            }
            else
            {
                tileOverlayRenderer.ClearCell(TileOverlayLayer.CellState, previousHoveredCellPosition);
            }
        }
        else
        {
            tileOverlayRenderer.ClearCell(TileOverlayLayer.CellState, previousHoveredCellPosition);
        }
    }

    public void ApplyCurrentHoverCell()
    {
        if (hoveredCell == null)
        {
            return;
        }

        if (currentMode == PlayerCombatActionMode.None)
        {
            tileOverlayRenderer.DrawCell(TileOverlayLayer.CellState, hoveredCellPosition, TileOverlayType.Hover);
        }
        else if (currentMode == PlayerCombatActionMode.DeployingHero)
        {
            if (hoveredCell.CanDeployHero())
            {
                tileOverlayRenderer.DrawCell(TileOverlayLayer.CellState, hoveredCellPosition, TileOverlayType.Selected);
                DrawPreviewAttackRange(hoveredCell);
            }
            else
            {
                tileOverlayRenderer.DrawCell(TileOverlayLayer.CellState, hoveredCellPosition, TileOverlayType.Invalid);
            }
        }
        else
        {
            tileOverlayRenderer.ClearCell(TileOverlayLayer.CellState, hoveredCellPosition);
        }
    }

    public bool GetCellFromScreenPosition(Vector2 screenPosition, out CombatGridCell cell)
    {
        cell = null;
        if (mainCamera == null || combatGrid == null)
        {
            return false;
        }

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;
        return combatGrid.WorldToCell(worldPosition, out cell);
    }

    public void ClearCurrentModeOverlays()
    {
        if (tileOverlayRenderer == null)
        {
            return;
        }

        switch (currentMode)
        {
            case PlayerCombatActionMode.DeployingHero:
                tileOverlayRenderer.ClearLayer(TileOverlayLayer.CellState);
                break;
            case PlayerCombatActionMode.SelectedDeployedHero:
                break;
            case PlayerCombatActionMode.RelocatingHero:
                break;
        }
    }

    public void ResetHoverState()
    {
        hoveredCellPosition = Vector3Int.zero;
        hoveredCell = null;
        previousHoveredCellPosition = Vector3Int.zero;
        previousHoveredCell = null;
    }

    
    public void ShowDeployGhost(HeroInstance heroInstance)
    {
        ghostHeroView.Show(heroInstance);
        currentDeployDirection = Vector2Int.left;
        ghostHeroView.SetFacingDirection(currentDeployDirection);
    }

    public void UpdateDeployGhost(Vector2 screenPosition)
    {
        if (ghostHeroView == null || hoveredCell == null)
        {
            return;
        }

        Vector3 cellPosition = combatGrid.CellToWorldCenter(hoveredCellPosition);
        ghostHeroView.UpdateWorldPosition(cellPosition);

        Vector2Int facingDirection = GetDeployDirection(screenPosition, cellPosition);
        ghostHeroView.SetFacingDirection(facingDirection);
    }

    private Vector2Int GetDeployDirection(Vector2 screenPosition, Vector3 cellPosition)
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(screenPosition);
        mouseWorld.z = cellPosition.z;

        Vector2 delta = mouseWorld - cellPosition;

        if (delta.sqrMagnitude <= 0.01f)
        {
            return currentDeployDirection;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return delta.x > 0f ? Vector2Int.right : Vector2Int.left;
        }

        return delta.y > 0f ? Vector2Int.up : Vector2Int.down;
    }

    public void HideDeployGhost()
    {
        ghostHeroView.Hide();
    }

    public void DeployingHero(HeroInstance heroInstance)
    {
        deployingHero = heroInstance;
    }

    public void CancelDeployHero()
    {
        deployingHero = null;
    }

    private void DrawPreviewAttackRange(CombatGridCell cell)
    {
        if (cell == null || tileOverlayRenderer == null)
        {
            return;
        }

        tileOverlayRenderer.ClearLayer(TileOverlayLayer.Area);

        if (deployingHero == null)
        {
            return;
        }

        var attackPattern = deployingHero.Definition.AttackPattern;
        if (attackPattern == null)
        {
            return;
        }

        foreach (var offset in attackPattern)
        {
            Vector3Int targetCellPosition = cell.CellPosition + new Vector3Int(offset.x, offset.y, 0);
            if (combatGrid.TryGetCell(targetCellPosition, out CombatGridCell targetCell))
            {
                tileOverlayRenderer.DrawCell(TileOverlayLayer.Area, targetCellPosition, TileOverlayType.AttackArea);
            }
        }
    }
}
