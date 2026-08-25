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
    private StageSystem stageSystem;
    private CombatTimeController combatTime;

    [Header("Hero Action")]
    [SerializeField] private SimpleSpriteAnimatorVFX retreatVFXPrefab;

    private PlayerCombatActionMode currentMode = PlayerCombatActionMode.None;

    private HeroCombatState deployingHero;
    private CombatGridCell currentDeployCell;
    private Vector2Int currentDeployDirection = Vector2Int.left;

    private Vector3Int hoveredCellPosition;
    private CombatGridCell hoveredCell;
    private Vector3Int previousHoveredCellPosition;
    private CombatGridCell previousHoveredCell;

    private float previousSpeedMultiplier = 1f;
    private float speedMultiplierOverride = 1f;

    private bool isInitialized;

    public bool HasSpeedOverride => speedMultiplierOverride != 1f;

    public void Initialize(Camera mainCamera, CombatGrid combatGrid, HeroDeploymentSystem heroDeploymentSystem, HeroDetailView heroDetailView,
                           TileOverlayRenderer tileOverlayRenderer, GhostHeroView ghostHeroView, StageSystem stageSystem, CombatTimeController combatTime)
    {
        this.mainCamera = mainCamera;
        this.combatGrid = combatGrid;
        this.heroDeploymentSystem = heroDeploymentSystem;
        this.heroDetailView = heroDetailView;
        this.tileOverlayRenderer = tileOverlayRenderer;
        this.ghostHeroView = ghostHeroView;
        this.stageSystem = stageSystem;
        this.combatTime = combatTime;

        currentMode = PlayerCombatActionMode.None;
        ResetHoverState();

        if (this.mainCamera == null || this.combatGrid == null || this.heroDeploymentSystem == null ||
            this.tileOverlayRenderer == null || this.combatTime == null)
        {
            Debug.LogError("[PlayerCombatAction] mainCamera, combatGrid, heroDeploymentSystem, tileOverlayRenderer, and CombatTimeController are required to initialize player combat actions.", this);
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

        if (currentMode == PlayerCombatActionMode.SelectedDeployedHero && mode != PlayerCombatActionMode.SelectedDeployedHero)
        {
            HeroRuntime selectedHeroRuntime = heroDeploymentSystem.SelectedHeroRuntime;
            if (selectedHeroRuntime != null)
            {
                selectedHeroRuntime.HideActionHUD();
            }

            heroDeploymentSystem.ClearSelection();
        }

        bool previousActionMode = IsActionMode(currentMode);
        bool isActionMode = IsActionMode(mode);

        if (!previousActionMode && isActionMode)
        {
            StartActionTime();
        }
        else if (previousActionMode && !isActionMode)
        {
            StopActionTime();
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
                DrawHeroSelectedCell(heroDeploymentSystem.SelectedHeroRuntime);
                break;
            default:
                break;
        }
    }

    private static bool IsActionMode(PlayerCombatActionMode mode)
    {
        return mode == PlayerCombatActionMode.DeployingHero || mode == PlayerCombatActionMode.SelectingDeployDirection || mode == PlayerCombatActionMode.SelectedDeployedHero;
    }

    private void StartActionTime()
    {
        if (combatTime == null || HasSpeedOverride)
        {
            return;
        }

        previousSpeedMultiplier = combatTime.CombatSpeedMultiplier;

        speedMultiplierOverride = GameplayConstants.ACTION_SPEED_MULTIPLIER;
        combatTime.SetSpeedMultiplier(speedMultiplierOverride);
    }

    private void StopActionTime()
    {
        if (combatTime == null || !HasSpeedOverride)
        {
            return;
        }

        combatTime.SetSpeedMultiplier(previousSpeedMultiplier);

        speedMultiplierOverride = 1f;
    }

    private void OnEnable()
    {
        if (isInitialized && IsActionMode(currentMode))
        {
            StartActionTime();
        }
    }

    private void OnDisable()
    {
        StopActionTime();
    }

    public void RefreshMode()
    {
        switch (currentMode)
        {
            case PlayerCombatActionMode.DeployingHero:
                break;
            case PlayerCombatActionMode.SelectedDeployedHero:
                break;
            case PlayerCombatActionMode.SelectingDeployDirection:
                break;
            default:
                break;
        }

        ChangeMode(PlayerCombatActionMode.None);
    }

    public void UpdateHover(Vector2 screenPosition)
    {
        if (!isInitialized)
        {
            Debug.LogError("[PlayerCombatAction] Cannot update hover before Initialize succeeds.", this);
            return;
        }

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
            case PlayerCombatActionMode.SelectingDeployDirection:
                break;
            default:
                break;
        }
    }

    public void HandleCurrentHeroAction(HeroActionType actionType)
    {
        if(!TryGetSelectedHeroRuntime(out HeroRuntime selectedHeroRuntime))
        {
            return;
        }
        
        switch (actionType)
        {
            case HeroActionType.None:
                break;
            case HeroActionType.Retreat:
                RetreatHero(selectedHeroRuntime);
                break;
            case HeroActionType.Skill:
                CastHeroSkill(selectedHeroRuntime);
                break;
            case HeroActionType.Upgrade:
                UpgradeHero(selectedHeroRuntime);
                break;
            default:
                Debug.LogWarning($"[PlayerCombatAction] Unhandled hero action type: {actionType}", this);
                break;
        }
    }

    public void RetreatHero(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null)
        {
            return;
        }

        int deployCost = heroRuntime.CombatState.DeployCost;
        Vector3 retreatPosition = heroRuntime.WorldPosition;

        if (!heroDeploymentSystem.RetreatSelectedHero())
        {
            return;
        }

        UnregisterDeployedHeroEvents(heroRuntime);
        stageSystem.RefundRetreatMeat(deployCost);

        if (retreatVFXPrefab != null)
        {
            CombatVFXSpawner.SpawnSimpleSpriteVFX(retreatVFXPrefab, retreatPosition);
        }

        ChangeMode(PlayerCombatActionMode.None);
    }

    public void CastHeroSkill(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null)
        {
            return;
        }
    }

    public void UpgradeHero(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null)
        {
            return;
        }
    }

    private bool TryGetSelectedHeroRuntime(out HeroRuntime heroRuntime)
    {
        heroRuntime = null;

        if (!isInitialized || currentMode != PlayerCombatActionMode.SelectedDeployedHero)
        {
            return false;
        }

        HeroRuntime selectedHeroRuntime = heroDeploymentSystem.SelectedHeroRuntime;
        if (selectedHeroRuntime == null)
        {
            return false;
        }

        heroRuntime = selectedHeroRuntime;
        return true;
    }

    public void DrawDeployableCells()
    {
        if (combatGrid == null || tileOverlayRenderer == null)
        {
            Debug.LogError("[PlayerCombatAction] CombatGrid and TileOverlayRenderer are required to draw deployable cells.", this);
            return;
        }

        tileOverlayRenderer.ClearLayer(TileOverlayLayer.CellState);

        foreach (var cell in combatGrid.GetAllDeployableCells())
        {
            tileOverlayRenderer.DrawCell(TileOverlayLayer.CellState, cell.CellPosition, TileOverlayType.Deployable);
        }
    }

    public void DrawHeroSelectedCell(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null)
        {
            return;
        }

        if (tileOverlayRenderer == null)
        {
            Debug.LogError("[PlayerCombatAction] TileOverlayRenderer is required to draw hero selected cell.", this);
            return;
        }

        Vector3Int cellPosition = heroRuntime.ActiveCellPosition;
        if (!combatGrid.TryGetCell(cellPosition, out var cell))
        {
            Debug.LogWarning("[PlayerCombatAction] HeroRuntime's active cell position is not valid in the combat grid.", this);
            return;
        }

        tileOverlayRenderer.DrawCell(TileOverlayLayer.CellState, cellPosition, TileOverlayType.Hover);
    }

    private void DrawPreviewAttackRange(CombatGridCell cell)
    {
        if (cell == null)
        {
            return;
        }

        if (tileOverlayRenderer == null)
        {
            Debug.LogError("[PlayerCombatAction] TileOverlayRenderer is required to draw preview attack range.", this);
            return;
        }

        tileOverlayRenderer.ClearLayer(TileOverlayLayer.Area);

        if (deployingHero == null)
        {
            return;
        }

        IReadOnlyList<Vector2Int> attackPattern = deployingHero.Definition.NormalAttackDefinition.AttackPattern;
        if (attackPattern == null)
        {
            Debug.LogError("[PlayerCombatAction] Deploying hero definition requires an attack pattern to draw preview range.", this);
            return;
        }

        List<Vector2Int> rotatedPattern = RotateAttackPattern(attackPattern, currentDeployDirection);
        foreach (var offset in rotatedPattern)
        {
            Vector3Int targetCellPosition = cell.CellPosition + new Vector3Int(offset.x, offset.y, 0);
            tileOverlayRenderer.DrawCell(TileOverlayLayer.Area, targetCellPosition, TileOverlayType.AttackArea);
        }
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
            Debug.LogError("[PlayerCombatAction] Camera and CombatGrid are required to resolve screen position to a combat cell.", this);
            return false;
        }

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;
        return combatGrid.TryWorldToCell(worldPosition, out cell);
    }

    public void ClearCurrentModeOverlays()
    {
        if (tileOverlayRenderer == null)
        {
            Debug.LogError("[PlayerCombatAction] TileOverlayRenderer is required to clear current mode overlays.", this);
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
        }
    }

    public void ResetHoverState()
    {
        hoveredCellPosition = Vector3Int.zero;
        hoveredCell = null;
        previousHoveredCellPosition = Vector3Int.zero;
        previousHoveredCell = null;
    }

    
    public void ShowDeployGhost(HeroCombatState combatState)
    {
        if (ghostHeroView == null)
        {
            Debug.LogError("[PlayerCombatAction] GhostHeroView is required to show deploy ghost.", this);
            return;
        }

        currentDeployDirection = Vector2Int.left;
        ghostHeroView.Show(combatState);
    }

    public void HideDeployGhost()
    {
        if (ghostHeroView == null)
        {
            Debug.LogError("[PlayerCombatAction] GhostHeroView is required to hide deploy ghost.", this);
            return;
        }

        ghostHeroView.Hide();
    }

    public void UpdateDeployGhost(Vector2 screenPosition)
    {
        if (ghostHeroView == null)
        {
            Debug.LogError("[PlayerCombatAction] GhostHeroView is required to update deploy ghost.", this);
            return;
        }

        if (hoveredCell == null)
        {
            return;
        }

        if (!combatGrid.TryCellToWorldBottomCenter(hoveredCell.CellPosition, out Vector3 bottomCenterPos))
        {
            return;
        }

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
            Debug.LogError("[PlayerCombatAction] GhostHeroView is required to update deploy direction.", this);
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
            Debug.LogError("[PlayerCombatAction] TileOverlayRenderer is required to clear preview attack range.", this);
            return;
        }

        tileOverlayRenderer.ClearTypeInLayer(TileOverlayLayer.Area, TileOverlayType.AttackArea);
    }

    public void ShowDetailHero(HeroCombatState combatState)
    {
        if (heroDetailView == null)
        {
            Debug.LogError("[PlayerCombatAction] HeroDetailView is required to show hero details.", this);
            return;
        }

        heroDetailView.Show(combatState);
    }

    public void ShowDetailHero(HeroRuntime heroRuntime)
    {
        if (heroDetailView == null)
        {
            Debug.LogError("[PlayerCombatAction] HeroDetailView is required to show hero details.", this);
            return;
        }

        heroDetailView.Show(heroRuntime);
    }


    public void PerformAction()
    {
        if (currentMode == PlayerCombatActionMode.SelectingDeployDirection)
        {
            if (currentDeployCell != null && deployingHero != null)
            {
                if (stageSystem != null)
                {
                    if (!stageSystem.CanDeployHero(deployingHero.DeployCost))
                    {
                        Debug.LogWarning("[PlayerCombatAction] Cannot deploy hero due to stage system restrictions.", this);
                        return;
                    }
                }

                HeroRuntime deployedHero = heroDeploymentSystem.DeploySelectedHero(currentDeployCell, currentDeployDirection);
                if (deployedHero != null)
                {
                    RegisterDeployedHeroEvents(deployedHero);
                    if (stageSystem != null)
                    {
                        stageSystem.TrySpendMeat(deployingHero.DeployCost);
                    }
                    FinishDeployHero();
                }
            }
        }
    }

    public bool StartDeployHero(HeroCombatState combatState, Vector2 screenPosition)
    {
        if (heroDeploymentSystem == null)
        {
            Debug.LogError("[PlayerCombatAction] HeroDeploymentSystem is required to start hero deployment.", this);
            return false;
        }

        if (combatState == null || !combatState.IsValid)
        {
            return false;
        }

        if (!heroDeploymentSystem.SelectHero(combatState))
        {
            return false;
        }

        deployingHero = combatState;
        currentDeployCell = null;
        currentDeployDirection = Vector2Int.left;

        ShowDeployGhost(combatState);
        UpdateHover(screenPosition);

        ChangeMode(PlayerCombatActionMode.DeployingHero);
        return true;
    }

    private void RegisterDeployedHeroEvents(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null)
        {
            return;
        }

        heroRuntime.OnSelected += HandleHeroSelected;
        heroRuntime.OnDestroyed -= HandleDeployedHeroDestroyed;
        heroRuntime.OnDestroyed += HandleDeployedHeroDestroyed;
    }

    private void UnregisterDeployedHeroEvents(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null)
        {
            return;
        }

        heroRuntime.OnSelected -= HandleHeroSelected;
        heroRuntime.OnDestroyed -= HandleDeployedHeroDestroyed;
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

    private void HandleHeroSelected(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null)
        {
            return;
        }

        HeroRuntime previousSelectedHero = heroDeploymentSystem.SelectedHeroRuntime;
        if (!heroDeploymentSystem.SelectHeroRuntime(heroRuntime))
        {
            return;
        }

        if (previousSelectedHero != null && previousSelectedHero != heroRuntime)
        {
            previousSelectedHero.HideActionHUD();
        }

        ChangeMode(PlayerCombatActionMode.SelectedDeployedHero);
        heroRuntime.ShowActionHUD(this);
        ShowDetailHero(heroRuntime);
    }

    private void HandleDeployedHeroDestroyed(UnitRuntime unitRuntime)
    {
        if (!(unitRuntime is HeroRuntime heroRuntime))
        {
            return;
        }

        UnregisterDeployedHeroEvents(heroRuntime);

        if (heroDeploymentSystem.SelectedHeroRuntime == heroRuntime)
        {
            ChangeMode(PlayerCombatActionMode.None);
        }
    }
}
