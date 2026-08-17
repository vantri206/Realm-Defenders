using System.Collections.Generic;
using UnityEngine;

public class EnemyRuntime : UnitRuntime, IBlockable
{
    // Enemy Identity
    private EnemyInstance enemyInstance;
    private EnemyDefinition enemyDefinition => enemyInstance != null ? enemyInstance.Definition : null;

    // Enemy Components
    [SerializeField] private EnemyPathfindingController enemyPathfindingController;
    private EnemyDepthSorter enemyDepthSorter;
    private IBlocker currentBlocker;

    // Enemy Stats
    public override UnitStats Stats => enemyInstance != null ? enemyInstance.Stats : base.Stats;
    public UnitSpeed Speed => enemyInstance != null ? enemyInstance.Speed : null;
    public override UnitMovementType MovementType => enemyDefinition != null ? enemyDefinition.MovementType : base.MovementType;
    public override UnitAttackType AttackType => enemyDefinition != null ? enemyDefinition.AttackType : base.AttackType;
    // Getters
    public EnemyInstance Instance => enemyInstance;
    public EnemyDefinition Definition => enemyDefinition;
    public EnemyPathfindingController PathfindingController => enemyPathfindingController;
    public float PathProgressScore => enemyPathfindingController != null ? enemyPathfindingController.PathProgressScore : 0f;
    public override Vector2 CenterOffset => enemyDefinition != null ? enemyDefinition.CenterOffset : base.CenterOffset;
    public bool CanBeBlocked => IsInitialized && !IsDead && MovementType != UnitMovementType.Flying;
    public bool IsBlocked => currentBlocker != null;
    public UnitRuntime Owner => this;
    public IBlocker CurrentBlocker => currentBlocker;
    public override bool IsMovementBlocked => base.IsMovementBlocked || IsBlocked;

    public override TargetSide TargetSide => enemyDefinition != null ? enemyDefinition.TargetSide : base.TargetSide;
    public override AttackEffect AttackEffect => enemyDefinition != null ? enemyDefinition.AttackEffect : base.AttackEffect;
    public override AttackMethod AttackMethod => enemyDefinition != null ? enemyDefinition.AttackMethod : base.AttackMethod;
    public override AttackDamageType AttackDamageType => enemyDefinition != null ? enemyDefinition.AttackDamageType : base.AttackDamageType;
    public override float NormalAttackEffectMultiplier => enemyDefinition != null ? enemyDefinition.NormalAttackEffectMultiplier : base.NormalAttackEffectMultiplier;
    public override AttackProjectile NormalAttackProjectilePrefab => enemyDefinition != null ? enemyDefinition.NormalAttackProjectilePrefab : base.NormalAttackProjectilePrefab;
    public override AttackAOEHit NormalAttackAOEHitPrefab => enemyDefinition != null ? enemyDefinition.NormalAttackAOEHitPrefab : base.NormalAttackAOEHitPrefab;
    public override SimpleSpriteAnimatorVFX NormalAttackHitVFXPrefab => enemyDefinition != null ? enemyDefinition.NormalAttackHitVFXPrefab : base.NormalAttackHitVFXPrefab;

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

    public void Initialize(EnemyInstance enemyInstance, CombatGrid combatGrid, Vector3Int currentCell, 
                        EnemyRouteGraph routeGraph, UnitPathfindingSystem pathfindingSystem, string routeId, EnemyDepthSorter enemyDepthSorter)
    {
        if (enemyInstance == null || !enemyInstance.IsValid)
        {
            Debug.LogError("[EnemyRuntime] A valid EnemyInstance is required to initialize enemy runtime.", this);
            return;
        }

        this.enemyInstance = enemyInstance;
        this.combatGrid = combatGrid;

        CacheReferences();
        SetupVisuals(Definition.EnemySprite, Definition.AnimatorController);
        InitializeStats();
        InitializeAttackSystems(Definition.TargetPriorityMode);
        SetDepthSorter(enemyDepthSorter);
        
        if (combatGrid == null)
        {
            Debug.LogError("[EnemyRuntime] CombatGrid is required to initialize enemy runtime.", this);
            return;
        }
        
        if (combatGrid.TryGetCell(currentCell, out CombatGridCell cell))
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

        if (enemyPathfindingController == null)
        {
            Debug.LogError("[EnemyRuntime] EnemyPathfindingController component is required to initialize enemy pathfinding.", this);
            return;
        }

        bool isPathfindingInitialized = enemyPathfindingController.Initialize(routeGraph, pathfindingSystem, routeId);

        isInitialized = isPathfindingInitialized;
    }

    public void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (normalAttackController == null)
        {
            Debug.LogError("[EnemyRuntime] NormalAttackController component is required to update enemy attacks.", this);
            return;
        }

        TickState(Time.deltaTime);
        normalAttackController.Tick(Time.deltaTime, resolvedAttackPattern, CanUseNormalAttack);
    }

    public void FixedUpdate()
    {
        if (!isInitialized)
        {
            return;
        }

        if (enemyPathfindingController == null || combatGrid == null || unitMovement == null)
        {
            Debug.LogError("[EnemyRuntime] EnemyPathfindingController, CombatGrid, and UnitMovement are required to update enemy movement.", this);
            SetMovementDirection(Vector2.zero);
            return;
        }
        
        Vector3 centerPosition = CenterPosition;
        CombatGridCell nextActiveCell = combatGrid.TryWorldToCell(centerPosition, out CombatGridCell cell) ? cell : null;
        SetActiveCell(nextActiveCell);

        if (!CanMove)
        {
            if (IsBlocked)
            {
                UpdateBlockedFacingDirection();
            }

            SetMovementDirection(Vector2.zero);
            unitMovement.FixedTick(Time.fixedDeltaTime);
            return;
        }

        if (activeCell != null)
        {
            if (combatGrid.TryCellToWorldCenter(activeCell.CellPosition, out Vector3 activeCellWorldCenter))
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

        unitMovement.FixedTick(Time.fixedDeltaTime);
    }

    protected override void InitializeStats()
    {
        InitializeHealth();

        InitializeMovement();
    }

    private void InitializeMovement()
    {
        if (Speed == null)
        {
            Debug.LogError("[EnemyRuntime] UnitSpeed is required to initialize enemy movement.", this);
            return;
        }

        if (unitMovement == null)
        {
            Debug.LogError("[EnemyRuntime] UnitMovement component is required to initialize movement.", this);
            return;
        }

        unitMovement.Initialize(Speed, MovementType);
    }

    protected void SetMovementDirection(Vector2 direction)
    {
        if (unitMovement == null)
        {
            Debug.LogError("[EnemyRuntime] UnitMovement component is required to set movement direction.", this);
            return;
        }

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
            if (unitVisual == null)
            {
                Debug.LogError("[EnemyRuntime] UnitVisual component is required to update movement animation state.", this);
            }
            else
            {
                unitVisual.SetIsMoving(false);
            }

            SetMovementState(false);
            return;
        }

        SetFacingDirection(Vector2Int.RoundToInt(direction));
        unitMovement.SetMoveDirection(direction);
        if (unitVisual == null)
        {
            Debug.LogError("[EnemyRuntime] UnitVisual component is required to update movement animation state.", this);
        }
        else
        {
            unitVisual.SetIsMoving(true);
        }

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

        currentBlocker = null;
    }

    private void UpdateBlockedFacingDirection()
    {
        if (currentBlocker == null || currentBlocker.Owner == null)
        {
            return;
        }

        Vector2 directionToBlocker = (Vector2)currentBlocker.Owner.CenterPosition - (Vector2)CenterPosition;
        Vector2Int blockerFacingDirection = GetFourDirection(directionToBlocker);

        if (blockerFacingDirection == Vector2Int.zero || blockerFacingDirection == facingDirection)
        {
            return;
        }

        SetFacingDirection(blockerFacingDirection);
    }

    private static Vector2Int GetFourDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2Int.zero;
        }

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? Vector2Int.right : Vector2Int.left;
        }

        return direction.y >= 0f ? Vector2Int.up : Vector2Int.down;
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
    Gizmos.DrawSphere(transform.position + (Vector3)CenterOffset, centerGizmoRadius);
}
#endif
}
