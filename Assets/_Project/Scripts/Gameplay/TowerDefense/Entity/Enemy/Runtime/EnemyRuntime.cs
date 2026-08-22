using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRuntime : UnitRuntime, IBlockable
{
    private const float normalAttackStateDuration = 0.3f;

    // Enemy Identity
    private EnemyInstance enemyInstance;
    private EnemyDefinition enemyDefinition;

    private IReadOnlyList<Vector2Int> defaultAttackPattern = new List<Vector2Int>();
    private IReadOnlyList<Vector2Int> resolvedAttackPattern = new List<Vector2Int>();

    // Enemy Components
    [SerializeField] private EnemyPathfindingController enemyPathfindingController;
    [SerializeField] private NormalAttackController normalAttackController;
    [SerializeField] private TargetScanner targetScanner;
    [SerializeField] private TargetSelector targetSelector;

    private EnemyDepthSorter enemyDepthSorter;
    private EnemyRouteGraph routeGraph;

    private IBlocker currentBlocker;

    // Getters
    public float PathProgressScore => enemyPathfindingController.PathProgressScore;
    public IBlocker CurrentBlocker => currentBlocker;

    public override UnitMovementType MovementType => enemyDefinition != null ? enemyDefinition.MovementType : base.MovementType;
    public override Vector2 CenterOffset => enemyDefinition != null ? enemyDefinition.NavigationOffset : Vector2.zero;
    
    public UnitRuntime Owner => this;
    public bool CanBeBlocked => IsInitialized && !IsDead && MovementType != UnitMovementType.Flying;
    public bool IsBlocked => currentBlocker != null;
    public override bool IsMovementBlocked => base.IsMovementBlocked || IsBlocked;

    // Attack
    public NormalAttackDefinition NormalAttackDefinition => enemyDefinition != null ? enemyDefinition.NormalAttackDefinition : null;
    public IReadOnlyList<Vector2Int> ResolvedAttackPattern => resolvedAttackPattern;

    public event Action<EnemyRuntime> OnEscaped;

    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(EnemyInstance enemyInstance, UnitCombatContext combatContext, EnemyRouteGraph routeGraph, Vector3Int currentCell, string routeId, EnemyDepthSorter enemyDepthSorter)
    {
        isInitialized = false;

        if (enemyInstance == null || !enemyInstance.IsValid)
        {
            Debug.LogError("[EnemyRuntime] A valid EnemyInstance is required to initialize enemy runtime.", this);
            return;
        }

        this.enemyInstance = enemyInstance;
        enemyDefinition = enemyInstance.Definition;
        this.routeGraph = routeGraph;
        this.combatContext = combatContext;
        InitializeRuntimeStats(enemyInstance);

        CacheReferences();

        if (!CheckCoreReferences() ||
            !CheckHealthSystemReferences() ||
            !CheckMovementSystemReferences() ||
            !CheckAttackSystemReferences() ||
            !CheckPathfindingSystemReferences())
        {
            return;
        }

        SetupVisuals(enemyDefinition.EnemySprite, enemyDefinition.AnimatorController);
        if (!InitializeMovementSystem(Stats, MovementType))
        {
            return;
        }

        if (!InitializeHealth())
        {
            return;
        }

        if (!InitializeAttackSystems())
        {
            return;
        }
        SetDepthSorter(enemyDepthSorter);
        
        if (combatContext.CombatGrid.TryGetCell(currentCell, out CombatGridCell cell))
        {
            SetActiveCell(cell);
        }
        else
        {
            Debug.LogError($"[EnemyRuntime] Failed to find a valid CombatGridCell at position {currentCell} for enemy initialization.", this);
            SetActiveCell(null);
        }
        
        defaultAttackPattern = new List<Vector2Int>(NormalAttackDefinition.AttackPattern);
        SetFacingDirection(facingDirection);

        centerOffset = enemyDefinition.NavigationOffset;

        bool isPathfindingInitialized = enemyPathfindingController.Initialize(routeGraph, combatContext.UnitPathfindingSystem, routeId, () => OnEscaped?.Invoke(this));

        if  (normalAttackController != null)
        {
            normalAttackController.OnAttack += HandleNormalAttack;
        }

        isInitialized = isPathfindingInitialized;
    }

    protected void OnDestroy()
    {
        if (normalAttackController != null)
        {
            normalAttackController.OnAttack -= HandleNormalAttack;
        }
    }

    protected override void OnDisable()
    {
        if (currentBlocker != null)
        {
            currentBlocker.ReleaseBlockedTarget(this);
            currentBlocker = null;
        }

        EnemyDepthSorter currentDepthSorter = enemyDepthSorter;
        enemyDepthSorter = null;
        if (currentDepthSorter != null)
        {
            currentDepthSorter.UnregisterEnemy(this);
        }

        if (normalAttackController != null)
        {
            normalAttackController.OnAttack -= HandleNormalAttack;
        }

        base.OnDisable();
    }

    public void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        float combatDeltaTime = combatContext.CombatTime.CombatDeltaTime;
        TickState(combatDeltaTime);
        normalAttackController.Tick(combatDeltaTime, ResolvedAttackPattern, CanUseNormalAttack);
    }

    public void FixedUpdate()
    {
        if (!isInitialized)
        {
            return;
        }

        Vector3 centerPosition = CenterPosition;
        CombatGridCell nextActiveCell = combatContext.CombatGrid.TryWorldToCell(centerPosition, out CombatGridCell cell) ? cell : null;
        SetActiveCell(nextActiveCell);

        if (!CanMove)
        {
            if (IsBlocked)
            {
                UpdateBlockedFacingDirection();
            }

            SetMovementDirection(Vector2.zero);
            unitMovement.FixedTick(combatContext.CombatTime.CombatFixedDeltaTime);
            return;
        }

        if (activeCell != null)
        {
            if (combatContext.CombatGrid.TryCellToWorldCenter(activeCell.CellPosition, out Vector3 activeCellWorldCenter))
            {
                SetMovementDirection(enemyPathfindingController.GetCurrentMoveDirection(this, activeCell.CellPosition, activeCellWorldCenter, centerPosition));
            }
            else
            {
                Debug.LogError($"[EnemyRuntime] Failed to get world center for active cell {activeCell.CellPosition}.", this);
                SetMovementDirection(Vector2.zero);
            }
        }
        else
        {
            SetMovementDirection(Vector2.zero);
        }

        unitMovement.FixedTick(combatContext.CombatTime.CombatFixedDeltaTime);
    }

    protected bool InitializeAttackSystems()
    {
        if (normalAttackController == null || targetScanner == null || targetSelector == null || combatContext.CombatTime == null)
        {
            Debug.LogError("[EnemyRuntime] NormalAttackController is required to initialize attack systems.", this);
            return false;
        }

        targetScanner.Initialize(combatContext.CombatGrid, this);
        targetSelector.Initialize(this);

        if (!normalAttackController.Initialize(battleTeam, Stats, NormalAttackDefinition, targetScanner, targetSelector, combatContext.CombatTime))
        {
            Debug.LogError("[EnemyRuntime] Failed to initialize NormalAttackController.", this);
            return false;
        }

        return true;
    }

    private void InitializeRuntimeStats(EnemyInstance enemyInstance)
    {
        runtimeStats = new UnitStats(enemyInstance.Stats);
    }

    protected override void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        resolvedAttackPattern = AttackPatternResolver.RefreshAttackPattern(defaultAttackPattern, direction);
        
        base.SetFacingDirection(direction);
    }


    protected void HandleNormalAttack(Hurtbox target)
    {
        if (target != null)
        {
            FacePosition(target.AimPosition);
        }

        TryStartActionState(UnitRuntimeState.Attacking, normalAttackStateDuration);

        unitVisual.TriggerAttack();
    }

    protected void SetMovementDirection(Vector2 direction)
    {
        if (!CanMove && direction != Vector2.zero)
        {
            unitMovement.SetMoveDirection(Vector2.zero);
            unitVisual.SetIsMoving(false);
            SetMovementState(false);
            return;
        }

        if (direction == Vector2.zero)
        {
            unitMovement.SetMoveDirection(Vector2.zero);
            unitVisual.SetIsMoving(false);

            SetMovementState(false);
            return;
        }

        SetFacingDirection(Vector2Int.RoundToInt(direction));
        unitMovement.SetMoveDirection(direction);
        unitVisual.SetIsMoving(true);

        SetMovementState(true);
    }

    public void SetDepthSorter(EnemyDepthSorter depthSorter)
    {
        enemyDepthSorter = depthSorter;
    }

    public void OnBlocked(IBlocker blocker)
    {
        if (blocker == null || blocker.Owner == null || currentBlocker == blocker)
        {
            return;
        }

        currentBlocker?.ReleaseBlockedTarget(this);

        unitMovement.ClearMovementOverride();
        currentBlocker = blocker;
        UpdateBlockedFacingDirection();
        SetMovementDirection(Vector2.zero);
    }

    public void ClearBlocked(IBlocker blocker)
    {
        if (currentBlocker != blocker)
        {
            return;
        }

        unitMovement.ClearMovementOverride();
        currentBlocker = null;
    }

    private void UpdateBlockedFacingDirection()
    {
        if (currentBlocker == null || currentBlocker.Owner == null)
        {
            return;
        }

        Vector2 directionToBlocker = (Vector2)currentBlocker.Owner.CenterPosition - (Vector2)CenterPosition;
        Vector2Int blockerFacingDirection = ResolveFourDirection(directionToBlocker);

        if (blockerFacingDirection == Vector2Int.zero || blockerFacingDirection == facingDirection)
        {
            return;
        }

        SetFacingDirection(blockerFacingDirection);
    }

    private bool CheckPathfindingSystemReferences()
    {
        if (enemyPathfindingController != null && routeGraph != null && combatContext.UnitPathfindingSystem != null)
        {
            return true;
        }

        Debug.LogError("[EnemyRuntime] Pathfinding system requires EnemyPathfindingController, EnemyRouteGraph, and UnitPathfindingSystem.", this);
        return false;
    }

    protected bool CheckAttackSystemReferences()
    {
        if (normalAttackController != null && targetScanner != null && targetSelector != null && combatContext.CombatTime != null)
        {
            return true;
        }

        Debug.LogError("[EnemyRuntime] Attack system requires missing references.", this);
        return false;
    }

    protected override void CacheReferences()
    {
        base.CacheReferences();

        if (unitMovement == null)
        {
            unitMovement = GetComponent<UnitMovement>();
        }

        if  (enemyPathfindingController == null)
        {
            enemyPathfindingController = GetComponent<EnemyPathfindingController>();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        const float navigationPointRadius = 0.025f;
        const float crossHalfLength = 0.06f;

        Vector3 navigationPosition = transform.position + (Vector3)centerOffset;

        Gizmos.color = Color.purple;
        Gizmos.DrawSphere(navigationPosition, navigationPointRadius);
        Gizmos.DrawLine(navigationPosition + Vector3.left * crossHalfLength,
                        navigationPosition + Vector3.right * crossHalfLength);
        Gizmos.DrawLine(navigationPosition + Vector3.down * crossHalfLength,
                        navigationPosition + Vector3.up * crossHalfLength);
    }
#endif
}
