using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCombatAction : MonoBehaviour
{
    [Header("References")]
    private Camera mainCamera;
    private CombatGrid combatGrid;
    private HeroDeploymentSystem heroDeploymentSystem;
    private HeroDetailView heroDetailView;
    private TileOverlayRenderer tileOverlayRenderer;
    private GhostHeroView ghostHeroView;

    private PlayerCombatActionMode currentMode = PlayerCombatActionMode.None;

    private HeroInstance deployingHero;
    private CombatGridCell currentDeployCell;
    private Vector2Int currentDeployDirection = Vector2Int.left;

    private Vector3Int hoveredCellPosition;
    private CombatGridCell hoveredCell;
    private Vector3Int previousHoveredCellPosition;
    private CombatGridCell previousHoveredCell;

    private bool isInitialized;

    public void Initialize(Camera mainCamera, CombatGrid combatGrid, HeroDeploymentSystem heroDeploymentSystem, HeroDetailView heroDetailView, TileOverlayRenderer tileOverlayRenderer, GhostHeroView ghostHeroView)
    {
        this.mainCamera = mainCamera;
        this.combatGrid = combatGrid;
        this.heroDeploymentSystem = heroDeploymentSystem;
        this.heroDetailView = heroDetailView;
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
            case PlayerCombatActionMode.SelectingDeployDirection:
                DrawPreviewAttackRange(currentDeployCell);
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
            case PlayerCombatActionMode.SelectingDeployDirection:
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
            ClearPreviewAttackRange();
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
                ClearPreviewAttackRange();
                break;
            case PlayerCombatActionMode.SelectingDeployDirection:
                ClearPreviewAttackRange();
                break;
            case PlayerCombatActionMode.SelectedDeployedHero:
                tileOverlayRenderer.ClearLayer(TileOverlayLayer.CellState);
                ClearPreviewAttackRange();
                break;
            case PlayerCombatActionMode.RelocatingHero:
                tileOverlayRenderer.ClearLayer(TileOverlayLayer.CellState);
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
        if (ghostHeroView == null)
        {
            return;
        }

        currentDeployDirection = Vector2Int.left;
        ghostHeroView.Show(heroInstance);
    }

    public void HideDeployGhost()
    {
        if (ghostHeroView == null)
        {
            return;
        }

        ghostHeroView.Hide();
    }

    public void UpdateDeployGhost(Vector2 screenPosition)
    {
        if (ghostHeroView == null || hoveredCell == null)
        {
            return;
        }

        Vector3 bottomCenterPos = combatGrid.CellToWorldBottomCenter(hoveredCell.CellPosition);
        ghostHeroView.UpdateWorldPosition(bottomCenterPos);
    }

    public void UpdateDeployDirection(Vector2Int direction)
    {
        if (currentMode != PlayerCombatActionMode.SelectingDeployDirection)
        {
            return;
        }

        if (ghostHeroView == null)
        {
            return;
        }

        currentDeployDirection = direction;
        ghostHeroView.SetFacingDirection(currentDeployDirection);
        DrawPreviewAttackRange(currentDeployCell);
    }
    
    public bool SaveDeployCellPosition()
    {
        if (hoveredCell == null || !hoveredCell.CanDeployHero())
        {
            return false;
        }

        currentDeployCell = hoveredCell;
        return true;
    }

    public void CancelDeployHero()
    {
        deployingHero = null;
        heroDeploymentSystem.ClearSelection();
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

        IReadOnlyList<Vector2Int> attackPattern = deployingHero.Definition.AttackPattern;
        if (attackPattern == null)
        {
            return;
        }

        List<Vector2Int> rotatedPattern = RotateAttackPattern(attackPattern, currentDeployDirection);
        foreach (var offset in rotatedPattern)
        {
            Vector3Int targetCellPosition = cell.CellPosition + new Vector3Int(offset.x, offset.y, 0);
            tileOverlayRenderer.DrawCell(TileOverlayLayer.Area, targetCellPosition, TileOverlayType.AttackArea);
        }
    }

    private static List<Vector2Int> RotateAttackPattern(IReadOnlyList<Vector2Int> attackPattern, Vector2Int direction)
    {
        List<Vector2Int> rotatedPattern = new List<Vector2Int>();

        if (attackPattern == null)
        {
            return rotatedPattern;
        }

        for (int i = 0; i < attackPattern.Count; i++)
        {
            Vector2Int offset = attackPattern[i];
            Vector2Int rotatedOffset = offset;

            if (direction == Vector2Int.right)
            {
                rotatedOffset = new Vector2Int(-offset.x, -offset.y);
            }
            else if (direction == Vector2Int.up)
            {
                rotatedOffset = new Vector2Int(offset.y, -offset.x);
            }
            else if (direction == Vector2Int.down)
            {
                rotatedOffset = new Vector2Int(-offset.y, offset.x);
            }

            rotatedPattern.Add(rotatedOffset);
        }

        return rotatedPattern;
    }

    private void ClearPreviewAttackRange()
    {
        if (tileOverlayRenderer == null)
        {
            return;
        }

        tileOverlayRenderer.ClearTypeInLayer(TileOverlayLayer.Area, TileOverlayType.AttackArea);
    }

    public void ShowDetailHero(HeroInstance heroInstance)
    {
        if (heroDetailView == null)
        {
            return;
        }

        heroDetailView.Show(heroInstance);
    }

    public void PerformAction()
    {
        if (currentMode == PlayerCombatActionMode.SelectingDeployDirection)
        {
            if (currentDeployCell != null && deployingHero != null)
            {
                HeroRuntime deployedHero = heroDeploymentSystem.DeploySelectedHero(currentDeployCell, currentDeployDirection);
                if (deployedHero != null)
                {
                    FinishDeployHero();
                }
            }
        }
    }

    public bool StartDeployHero(HeroInstance heroInstance, Vector2 screenPosition)
    {
        if (heroDeploymentSystem == null || heroInstance == null || !heroInstance.IsValid)
        {
            return false;
        }

        if (!heroDeploymentSystem.SelectHero(heroInstance))
        {
            return false;
        }

        deployingHero = heroInstance;
        currentDeployCell = null;
        currentDeployDirection = Vector2Int.left;

        ShowDeployGhost(heroInstance);
        UpdateHover(screenPosition);

        ChangeMode(PlayerCombatActionMode.DeployingHero);
        return true;
    }

    public void FinishDeployHero()
    {
        deployingHero = null;
        currentDeployCell = null;
        currentDeployDirection = Vector2Int.left;

        HideDeployGhost();
        RefreshMode();
        CancelDeployHero();
    }
}
