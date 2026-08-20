using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRuntime : UnitRuntime, IBlockable
{
    // Enemy Identity
    private EnemyInstance enemyInstance;
    private EnemyDefinition enemyDefinition;

    // Enemy Components
    [SerializeField] private EnemyPathfindingController enemyPathfindingController;
    private EnemyDepthSorter enemyDepthSorter;
    private EnemyRouteGraph routeGraph;
    private IBlocker currentBlocker;

    // Enemy Stats
    public override UnitStats Stats => enemyInstance.Stats;
    public UnitSpeed Speed => enemyInstance.Speed;
    public override UnitMovementType MovementType => enemyDefinition.MovementType;
    public override UnitAttackType AttackType => enemyDefinition.AttackType;
    // Getters
    public EnemyInstance Instance => enemyInstance;
    public EnemyDefinition Definition => enemyDefinition;
    public EnemyPathfindingController PathfindingController => enemyPathfindingController;
    public float PathProgressScore => enemyPathfindingController.PathProgressScore;
    public override Vector2 CenterOffset => enemyDefinition.CenterOffset;
    public bool CanBeBlocked => IsInitialized && !IsDead && MovementType != UnitMovementType.Flying;
    public bool IsBlocked => currentBlocker != null;
    public UnitRuntime Owner => this;
    public IBlocker CurrentBlocker => currentBlocker;
    public override bool IsMovementBlocked => base.IsMovementBlocked || IsBlocked;

    public override TargetSide TargetSide => enemyDefinition.TargetSide;
    public override AttackEffect AttackEffect => enemyDefinition.AttackEffect;
    public override AttackMethod AttackMethod => enemyDefinition.AttackMethod;
    public override AttackDamageType AttackDamageType => enemyDefinition.AttackDamageType;
    public override float NormalAttackEffectMultiplier => enemyDefinition.NormalAttackEffectMultiplier;
    public override AttackProjectile NormalAttackProjectilePrefab => enemyDefinition.NormalAttackProjectilePrefab;
    public override AttackAOEHit NormalAttackAOEHitPrefab => enemyDefinition.NormalAttackAOEHitPrefab;
    public override SimpleSpriteAnimatorVFX NormalAttackHitVFXPrefab => enemyDefinition.NormalAttackHitVFXPrefab;

    public event Action<EnemyRuntime> OnEscaped;

    private void Awake()
    {
        CacheReferences();
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

        base.OnDisable();
    }

    public void Initialize(EnemyInstance enemyInstance, UnitCombatContext combatContext, EnemyRouteGraph routeGraph,
                         Vector3Int currentCell, string routeId, EnemyDepthSorter enemyDepthSorter)
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

        CacheReferences();

        if (!CheckCoreReferences() ||
            !CheckHealthSystemReferences() ||
            !CheckMovementSystemReferences(Speed) ||
            !CheckAttackSystemReferences() ||
            !CheckPathfindingSystemReferences())
        {
            return;
        }

        SetupVisuals(Definition.EnemySprite, Definition.AnimatorController);
        if (!InitializeMovementSystem(Speed, MovementType))
        {
            return;
        }

        if (!InitializeHealth())
        {
            return;
        }

        if (!InitializeAttackSystems(Definition.TargetPriorityMode))
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
        
        defaultAttackPattern = new List<Vector2Int>(Definition.AttackPattern);
        SetFacingDirection(facingDirection);

        centerOffset = Definition.CenterOffset;

        bool isPathfindingInitialized = enemyPathfindingController.Initialize(routeGraph, combatContext.UnitPathfindingSystem, routeId, () => OnEscaped?.Invoke(this));

        isInitialized = isPathfindingInitialized;
    }

    public void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        float combatDeltaTime = combatContext.CombatTime.CombatDeltaTime;
        TickState(combatDeltaTime);
        normalAttackController.Tick(combatDeltaTime, resolvedAttackPattern, CanUseNormalAttack);
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

    private bool CheckPathfindingSystemReferences()
    {
        if (enemyPathfindingController != null && routeGraph != null && combatContext.UnitPathfindingSystem != null)
        {
            return true;
        }

        Debug.LogError("[EnemyRuntime] Pathfinding system requires EnemyPathfindingController, EnemyRouteGraph, and UnitPathfindingSystem.", this);
        return false;
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
    Color centerGizmoColor = Color.purple;
    float centerGizmoRadius = 0.1f;
    Gizmos.color = centerGizmoColor;
    Gizmos.DrawSphere(transform.position + (Vector3)centerOffset, centerGizmoRadius);
}
#endif
}
