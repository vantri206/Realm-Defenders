using System;
using System.Collections.Generic;
using UnityEngine;

public class HeroRuntime : UnitRuntime
{
    private const float normalAttackStateDuration = 0.3f;

    // Hero Identity
    private HeroCombatState combatState;
    private HeroDefinition heroDefinition;

    private CombatGridCell anchorCell;
    private Vector2Int initialFacingDirection = Vector2Int.left;

    private IReadOnlyList<Vector2Int> defaultAttackPattern = new List<Vector2Int>();
    private IReadOnlyList<Vector2Int> resolvedAttackPattern = new List<Vector2Int>();

    // Hero Components
    [SerializeField] private HeroBlocker heroBlocker;
    [SerializeField] private HeroActionHUD heroActionHUD;    
    [SerializeField] protected TargetScanner targetScanner;
    [SerializeField] protected TargetSelector targetSelector;
    [SerializeField] protected NormalAttackController normalAttackController;

    private bool hasBlocker;

    // Hero Stats
    public int BlockCount => Stats.BlockCount;
    public int CurrentBlock => heroBlocker != null ? heroBlocker.CurrentBlockCount : 0;

    // Getters
    public HeroCombatState CombatState => combatState;
    public HeroDefinition Definition => heroDefinition;
    public HeroBlocker HeroBlocker => heroBlocker;
    public CombatGridCell AnchorCell => anchorCell;

    // Attack Pattern
    public NormalAttackDefinition NormalAttackDefinition => heroDefinition != null ? heroDefinition.NormalAttackDefinition : null;
    public IReadOnlyList<Vector2Int> ResolvedAttackPattern => resolvedAttackPattern;
    public HeroBlockState BlockState => heroBlocker != null ? heroBlocker.BlockState : HeroBlockState.NonBlocking;
    public override bool IsMovementBlocked => base.IsMovementBlocked || (BlockState == HeroBlockState.Blocking);

    public event Action<HeroRuntime> OnSelected;

    public void Initialize(HeroCombatState combatState, UnitCombatContext combatContext, Vector3Int currentCell)
    {
        isInitialized = false;

        if (combatState == null || !combatState.IsValid)
        {
            Debug.LogError("[HeroRuntime] A valid HeroCombatState is required to initialize hero runtime.", this);
            return;
        }

        this.combatState = combatState;
        heroDefinition = combatState.Definition;
        this.combatContext = combatContext;
        InitializeRuntimeStats(combatState);

        CacheReferences();
        HideActionHUD();

        if (!CheckCoreReferences() || !CheckHealthSystemReferences() || !CheckMovementSystemReferences() || !CheckAttackSystemReferences() || !CheckBlockSystemReferences())
        {
            return;
        }

        SetupVisuals(heroDefinition.HeroDefaultSprite, heroDefinition.AnimatorController);
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
    
        SetActiveCell(combatContext.CombatGrid.TryGetCell(currentCell, out CombatGridCell activeCell) ? activeCell : null);
        SetAnchorCell(combatContext.CombatGrid.TryGetCell(currentCell, out CombatGridCell anchorCell) ? anchorCell : null);

        defaultAttackPattern = new List<Vector2Int>(NormalAttackDefinition.AttackPattern);
        initialFacingDirection = facingDirection;
        SetFacingDirection(facingDirection);

        if (hasBlocker)
        {
            heroBlocker.Initialize(this, Stats);
        }

        if (normalAttackController != null)
        {
            normalAttackController.OnAttack += HandleNormalAttack;
        }

        isInitialized = true;
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
        if (heroBlocker != null)
        {
            heroBlocker.ClearBlocks();
        }

        ClearAnchorCell();

        if (normalAttackController != null)
        {
            normalAttackController.OnAttack -= HandleNormalAttack;
        }

        base.OnDisable();
    }

    private void InitializeRuntimeStats(HeroCombatState combatState)
    {
        runtimeStats = new UnitStats(combatState.Stats);
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

    private void FixedUpdate()
    {
        if (!isInitialized)
        {
            return;
        }

        RefreshActiveCell();
        TickBlock();
        FixedTickMovement();
    }

    protected bool InitializeAttackSystems()
    {
        if (normalAttackController == null || targetScanner == null || targetSelector == null || combatContext.CombatTime == null)
        {
            Debug.LogError("[HeroRuntime] NormalAttackController is required to initialize attack systems.", this);
            return false;
        }

        targetScanner.Initialize(combatContext.CombatGrid, this);
        targetSelector.Initialize(this);

        if (!normalAttackController.Initialize(battleTeam, Stats, NormalAttackDefinition, targetScanner, targetSelector, combatContext.CombatTime))
        {
            Debug.LogError("[HeroRuntime] Failed to initialize NormalAttackController.", this);
            return false;
        }

        return true;
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


    private void RefreshActiveCell()
    {
        if (combatContext.CombatGrid.TryWorldToCell(CenterPosition, out CombatGridCell cell))
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
        if (hasBlocker)
        {
            heroBlocker.FixedTick();
        }
    }

    private void FixedTickMovement()
    {
        unitMovement.FixedTick(combatContext.CombatTime.CombatFixedDeltaTime);
    }

    private void StopGuardMovement()
    {
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
        if (normalAttackController.HasCurrentTarget)
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

    private bool CheckBlockSystemReferences()
    {
        hasBlocker = BlockCount > 0;

        if (hasBlocker && heroBlocker == null)
        {
            Debug.LogError("[HeroRuntime] Block system requires HeroBlocker when BlockCount is greater than zero.", this);
            return false;
        }

        return true;
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

    private void SetMovementDirection(Vector2 direction)
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

    public void HandleSelection()
    {
        if (!isInitialized)
        {
            return;
        }

        OnSelected?.Invoke(this);
    }

    public void ShowActionHUD(PlayerCombatAction playerCombatAction)
    {
        if (heroActionHUD == null)
        {
            Debug.LogWarning("[HeroRuntime] HeroActionHUD is not assigned.", this);
            return;
        }

        heroActionHUD.Show(playerCombatAction);
    }

    public void HideActionHUD()
    {
        if (heroActionHUD != null)
        {
            heroActionHUD.Hide();
        }
    }

    protected bool CheckAttackSystemReferences()
    {
        if (normalAttackController == null || targetScanner == null || targetSelector == null)
        {
            Debug.LogError("[HeroRuntime] Attack system requires missing components.", this);
            return false;
        }

        return true;
    }

    protected override void CacheReferences()
    {
        base.CacheReferences();

        if (heroBlocker == null)
        {
            heroBlocker = GetComponent<HeroBlocker>();
        }

        if (heroActionHUD == null)
        {
            heroActionHUD = GetComponentInChildren<HeroActionHUD>(true);
        }

        if (targetScanner == null)
        {
            targetScanner = GetComponentInChildren<TargetScanner>(true);
        }

        if (targetSelector == null)
        {
            targetSelector = GetComponentInChildren<TargetSelector>(true);
        }

        if (normalAttackController == null)
        {
            normalAttackController = GetComponentInChildren<NormalAttackController>(true);
        }
    }
}
