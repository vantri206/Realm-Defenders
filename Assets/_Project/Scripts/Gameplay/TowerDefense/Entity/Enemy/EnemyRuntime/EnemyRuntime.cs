using System.Collections.Generic;
using UnityEngine;

public class EnemyRuntime : UnitRuntime
{
    // Enemy Identity
    private EnemyInstance enemyInstance;
    private EnemyDefinition enemyDefinition => enemyInstance != null ? enemyInstance.Definition : null;

    // Enemy Components
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyPathfindingController enemyPathfindingController;

    // Enemy Stats
    public override UnitStats Stats => enemyInstance != null ? enemyInstance.Stats : base.Stats;
    public EnemySpeed Speed => enemyInstance != null ? enemyInstance.Speed : null;
    public override UnitAttackType AttackType => enemyDefinition != null ? enemyDefinition.AttackType : base.AttackType;
    // Getters
    public EnemyInstance Instance => enemyInstance;
    public EnemyDefinition Definition => enemyDefinition;
    public override Vector2 CenterOffset => enemyDefinition != null ? enemyDefinition.CenterOffset : base.CenterOffset;
    public Vector3 CenterPosition => transform.position + (Vector3)CenterOffset;

    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(EnemyInstance enemyInstance, CombatGrid combatGrid, Vector3Int currentCell, 
                        EnemyRouteGraph routeGraph, UnitPathfindingSystem pathfindingSystem, string routeId)
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

        normalAttackController.Tick(Time.deltaTime, resolvedAttackPattern);
    }

    public void FixedUpdate()
    {
        if (!isInitialized)
        {
            return;
        }

        if (enemyPathfindingController == null)
        {
            Debug.LogError("[EnemyRuntime] EnemyPathfindingController component is required to update enemy movement direction.", this);
            return;
        }

        if (enemyMovement == null)
        {
            Debug.LogError("[EnemyRuntime] EnemyMovement component is required to update enemy movement.", this);
            return;
        }

        if (combatGrid == null)
        {
            Debug.LogError("[EnemyRuntime] CombatGrid is required to update enemy active cell.", this);
            return;
        }
        
        SetActiveCell(combatGrid.TryWorldToCell(CenterPosition, out CombatGridCell cell) ? cell : null);

        if (activeCell != null)
        {
            if (combatGrid.TryCellToWorldCenter(activeCell.CellPosition, out Vector3 activeCellWorldCenter))
            {
                SetMovementDirection(enemyPathfindingController.GetCurrentMoveDirection(activeCell.CellPosition, activeCellWorldCenter, CenterPosition));
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

        enemyMovement.FixedTick(Time.fixedDeltaTime);
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
            Debug.LogError("[EnemyRuntime] EnemySpeed is required to initialize enemy movement.", this);
            return;
        }

        if (enemyMovement == null)
        {
            Debug.LogError("[EnemyRuntime] EnemyMovement component is required to initialize movement.", this);
            return;
        }

        enemyMovement.Initialize(Speed);
    }

    protected void SetMovementDirection(Vector2 direction)
    {
        if (enemyMovement == null)
        {
            Debug.LogError("[EnemyRuntime] EnemyMovement component is required to set movement direction.", this);
            return;
        }

        if (direction == Vector2.zero)
        {
            enemyMovement.SetMoveDirection(Vector2.zero);
            if (unitVisual == null)
            {
                Debug.LogError("[EnemyRuntime] UnitVisual component is required to update movement animation state.", this);
            }
            else
            {
                unitVisual.SetIsMoving(false);
            }

            return;
        }

        SetFacingDirection(Vector2Int.RoundToInt(direction));
        enemyMovement.SetMoveDirection(direction);
        if (unitVisual == null)
        {
            Debug.LogError("[EnemyRuntime] UnitVisual component is required to update movement animation state.", this);
        }
        else
        {
            unitVisual.SetIsMoving(true);
        }
    }

    protected override void CacheReferences()
    {
        base.CacheReferences();

        if (enemyMovement == null)
        {
            enemyMovement = GetComponent<EnemyMovement>();
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
