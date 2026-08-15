using System.Collections.Generic;
using UnityEngine;

public class HeroRuntime : UnitRuntime
{
    // Hero Identity
    private HeroInstance heroInstance;
    private HeroDefinition heroDefinition => heroInstance != null ? heroInstance.Definition : null;

    private CombatGridCell anchorCell;
    private Vector2Int initialFacingDirection = Vector2Int.left;

    // Hero Components
    [SerializeField] private HeroBlocker heroBlocker;
    [SerializeField] private HeroPathfindingController heroPathfindingController;

    // Hero Stats
    public override UnitStats Stats => heroInstance != null ? heroInstance.Stats : null;
    public UnitBlock Blocker => heroInstance != null ? heroInstance.Block : null;
    public UnitSpeed Speed => heroInstance != null ? heroInstance.Speed : null;
    public int BlockCount => Blocker != null ? Blocker.BlockCount : 0;
    public int CurrentBlock => Blocker != null ? Blocker.CurrentBlock : 0;
    public HeroBlockState BlockState => Blocker != null && Blocker.IsBlocked ? HeroBlockState.Blocking : HeroBlockState.NonBlocking;
    public override UnitAttackType AttackType => heroDefinition != null ? heroDefinition.AttackType : base.AttackType;
    public bool CanGuard => heroDefinition != null ? heroDefinition.CanGuard : false;
    public override bool IsMovementBlocked => base.IsMovementBlocked || (Blocker != null && Blocker.IsBlocked);

    // Getters
    public HeroInstance Instance => heroInstance;
    public HeroDefinition Definition => heroDefinition;
    public HeroBlocker HeroBlocker => heroBlocker;
    public CombatGridCell AnchorCell => anchorCell;

    public override AttackMethod AttackMethod => heroDefinition != null ? heroDefinition.AttackMethod : base.AttackMethod;
    public override AttackDamageType AttackDamageType => heroDefinition != null ? heroDefinition.AttackDamageType : base.AttackDamageType;
    public override float NormalAttackDamageMultiplier => heroDefinition != null ? heroDefinition.NormalAttackDamageMultiplier : base.NormalAttackDamageMultiplier;

    public void Initialize(HeroInstance heroInstance, CombatGrid combatGrid, Vector3Int currentCell, UnitPathfindingSystem unitPathfindingSystem)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogError("[HeroRuntime] A valid HeroInstance is required to initialize hero runtime.", this);
            return;
        }

        this.heroInstance = heroInstance;
        this.combatGrid = combatGrid;

        CacheReferences();
        SetupVisuals(heroDefinition.HeroSprite, heroDefinition.AnimatorController);
        InitializeStats();
        InitializeAttackSystems(heroDefinition.TargetPriorityMode);
    
        SetActiveCell(combatGrid.TryGetCell(currentCell, out CombatGridCell activeCell) ? activeCell : null);
        SetAnchorCell(combatGrid.TryGetCell(currentCell, out CombatGridCell anchorCell) ? anchorCell : null);

        defaultAttackPattern = new List<Vector2Int>(heroDefinition.AttackPattern);
        initialFacingDirection = facingDirection;
        SetFacingDirection(facingDirection);

        if (heroPathfindingController != null)
        {
            heroPathfindingController.Initialize(combatGrid, unitPathfindingSystem, teamIdentity);
        }

        if (heroBlocker != null)
        {
            heroBlocker.Initialize(this, heroInstance.Block);
        }

        isInitialized = true;
    }

    public void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (normalAttackController == null)
        {
            Debug.LogError("[HeroRuntime] NormalAttackController component is required to update hero attacks.", this);
            return;
        }

        TickState(Time.deltaTime);
        normalAttackController.Tick(Time.deltaTime, resolvedAttackPattern, CanUseNormalAttack);
    }

    private void FixedUpdate()
    {
        if (!isInitialized)
        {
            return;
        }

        if (combatGrid == null)
        {
            Debug.LogError("[HeroRuntime] CombatGrid is required to update hero runtime.", this);
            return;
        }

        RefreshActiveCell();
        TickBlock();
        TickGuardMovement();
        FixedTickMovement();
    }

    private void RefreshActiveCell()
    {
        if (combatGrid.TryWorldToCell(CenterPosition, out CombatGridCell cell))
        {
            SetActiveCell(cell);
        }
        else
        {
            SetActiveCell(null);
        }
    }

    private void TickBlock()
    {
        if (heroBlocker != null)
        {
            heroBlocker.FixedTick();
        }
    }

    private void TickGuardMovement()
    {
        if (unitMovement == null || heroPathfindingController == null)
        {
            SetMovementDirection(Vector2.zero);
            return;
        }

        if (!CanMove || !CanGuard || anchorCell == null || activeCell == null)
        {
            StopGuardMovement();
            return;
        }

        Vector2 moveDirection = heroPathfindingController.GetCurrentMoveDirection(this,activeCell, anchorCell, CenterPosition);
        SetMovementDirection(moveDirection);
        ResetFacingDirection(moveDirection);
    }

    private void FixedTickMovement()
    {
        if (unitMovement == null)
        {
            return;
        }

        unitMovement.FixedTick(Time.fixedDeltaTime);
    }

    private void StopGuardMovement()
    {
        heroPathfindingController?.ResetMoveTarget();
        SetMovementDirection(Vector2.zero);
    }

    public void SetInitialFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        initialFacingDirection = direction;
        SetFacingDirection(direction);
    }

    private void ResetFacingDirection(Vector2 moveDirection)
    {
        if (heroPathfindingController.HasGuardTarget)
        {
            return;
        }

        if (moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            return;
        }

        if (activeCell == null || anchorCell == null)
        {
            return;
        }

        if  (activeCell != anchorCell || CurrentState != UnitRuntimeState.Idle)
        {
            return;
        }

        if (facingDirection != initialFacingDirection)
        {
            SetFacingDirection(initialFacingDirection);
        }
    }

    protected override void InitializeStats()
    {
        InitializeHealth();
        InitializeMovement();
    }

    private void InitializeMovement()
    {
        if (Speed == null || unitMovement == null)
        {
            return;
        }

        unitMovement.Initialize(Speed);
    }

    private void SetMovementDirection(Vector2 direction)
    {
        if (unitMovement == null)
        {
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
            unitVisual.SetIsMoving(false);
            SetMovementState(false);
            return;
        }

        SetFacingDirection(Vector2Int.RoundToInt(direction));
        unitMovement.SetMoveDirection(direction);
        unitVisual.SetIsMoving(true);
        SetMovementState(true);
    }

    public void SetAnchorCell(CombatGridCell cell)
    {
        if (cell != null)
        {
            anchorCell = cell;
            anchorCell.SetAchoredHero(this);
        }
    }

    public void ClearAnchorCell()
    {
        if (anchorCell != null)
        {
            anchorCell.ClearAnchoredHero();
            anchorCell = null;
        }
    }

    protected override void OnDisable()
    {
        if (heroBlocker != null)
        {
            heroBlocker.ClearBlocks();
        }

        ClearAnchorCell();

        base.OnDisable();
    }

    protected override void CacheReferences()
    {
        base.CacheReferences();

        if (heroBlocker == null)
        {
            heroBlocker = GetComponent<HeroBlocker>();
        }

        if (heroPathfindingController == null)
        {
            heroPathfindingController = GetComponent<HeroPathfindingController>();
        }
    }
}
