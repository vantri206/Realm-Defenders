using System;
using System.Collections.Generic;
using UnityEngine;

public class HeroRuntime : UnitRuntime
{
    // Hero Identity
    private HeroInstance heroInstance;
    private HeroDefinition heroDefinition;

    private CombatGridCell anchorCell;
    private Vector2Int initialFacingDirection = Vector2Int.left;

    // Hero Components
    [SerializeField] private HeroBlocker heroBlocker;
    [SerializeField] private HeroPathfindingController heroPathfindingController;
    private bool hasBlocker;
    private bool hasGuardMovement;

    // Hero Stats
    public override UnitStats Stats => heroInstance.Stats;
    public UnitBlock Blocker => heroInstance.Block;
    public UnitSpeed Speed => heroInstance.Speed;
    public int BlockCount => Blocker.BlockCount;
    public int CurrentBlock => Blocker.CurrentBlock;
    public HeroBlockState BlockState => Blocker.IsBlocked ? HeroBlockState.Blocking : HeroBlockState.NonBlocking;
    public override UnitAttackType AttackType => heroDefinition.AttackType;
    public bool CanGuard => heroDefinition.CanGuard;
    public override bool IsMovementBlocked => base.IsMovementBlocked || Blocker.IsBlocked;

    // Getters
    public HeroInstance Instance => heroInstance;
    public HeroDefinition Definition => heroDefinition;
    public HeroBlocker HeroBlocker => heroBlocker;
    public CombatGridCell AnchorCell => anchorCell;

    public override TargetSide TargetSide => heroDefinition.TargetSide;
    public override AttackEffect AttackEffect => heroDefinition.AttackEffect;
    public override AttackMethod AttackMethod => heroDefinition.AttackMethod;
    public override AttackDamageType AttackDamageType => heroDefinition.AttackDamageType;
    public override float NormalAttackEffectMultiplier => heroDefinition.NormalAttackEffectMultiplier;
    public override AttackProjectile NormalAttackProjectilePrefab => heroDefinition.NormalAttackProjectilePrefab;
    public override AttackAOEHit NormalAttackAOEHitPrefab => heroDefinition.NormalAttackAOEHitPrefab;
    public override SimpleSpriteAnimatorVFX NormalAttackHitVFXPrefab => heroDefinition.NormalAttackHitVFXPrefab;
    public override ParticleVFX NormalAttackHealVFXPrefab => heroDefinition.NormalAttackHealVFXPrefab;

    public event Action<HeroRuntime> OnSelected;

    public void Initialize(HeroInstance heroInstance, UnitCombatContext combatContext, Vector3Int currentCell)
    {
        isInitialized = false;

        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogError("[HeroRuntime] A valid HeroInstance is required to initialize hero runtime.", this);
            return;
        }

        this.heroInstance = heroInstance;
        heroDefinition = heroInstance.Definition;
        this.combatContext = combatContext;

        CacheReferences();

        if (!CheckCoreReferences() ||
            !CheckHealthSystemReferences() ||
            !CheckMovementSystemReferences(Speed) ||
            !CheckAttackSystemReferences() ||
            !CheckBlockSystemReferences() ||
            !CheckGuardSystemReferences())
        {
            return;
        }

        SetupVisuals(heroDefinition.HeroSprite, heroDefinition.AnimatorController);
        if (!InitializeMovementSystem(Speed, MovementType))
        {
            return;
        }

        if (!InitializeHealth())
        {
            return;
        }

        if (!InitializeAttackSystems(heroDefinition.TargetPriorityMode))
        {
            return;
        }
    
        SetActiveCell(combatContext.CombatGrid.TryGetCell(currentCell, out CombatGridCell activeCell) ? activeCell : null);
        SetAnchorCell(combatContext.CombatGrid.TryGetCell(currentCell, out CombatGridCell anchorCell) ? anchorCell : null);

        defaultAttackPattern = new List<Vector2Int>(heroDefinition.AttackPattern);
        initialFacingDirection = facingDirection;
        SetFacingDirection(facingDirection);

        if (hasGuardMovement)
        {
            if (!heroPathfindingController.Initialize(combatContext.CombatGrid, combatContext.UnitPathfindingSystem, teamIdentity))
            {
                return;
            }
        }

        if (hasBlocker)
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

        float combatDeltaTime = combatContext.CombatTime.CombatDeltaTime;
        TickState(combatDeltaTime);
        normalAttackController.Tick(combatDeltaTime, resolvedAttackPattern, CanUseNormalAttack);
    }

    private void FixedUpdate()
    {
        if (!isInitialized)
        {
            return;
        }

        RefreshActiveCell();
        TickBlock();
        TickGuardMovement();
        FixedTickMovement();
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

    private void TickGuardMovement()
    {
        if (!hasGuardMovement)
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
        unitMovement.FixedTick(combatContext.CombatTime.CombatFixedDeltaTime);
    }

    private void StopGuardMovement()
    {
        heroPathfindingController.ResetMoveTarget();
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

    private bool CheckGuardSystemReferences()
    {
        hasGuardMovement = CanGuard;

        if (hasGuardMovement && (heroPathfindingController == null || combatContext.UnitPathfindingSystem == null))
        {
            Debug.LogError("[HeroRuntime] Guard system requires HeroPathfindingController and UnitPathfindingSystem.", this);
            return false;
        }

        return true;
    }

    protected override void HandleNormalAttack(Hurtbox target)
    {
        if (target != null)
        {
            FacePosition(target.CenterPosition);
        }

        base.HandleNormalAttack(target);
    }

    public override void RemoveCombat()
    {
        base.RemoveCombat();

        heroInstance.StartRedeployCountdown();
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

    protected override void OnDisable()
    {
        if (heroBlocker != null)
        {
            heroBlocker.ClearBlocks();
        }

        ClearAnchorCell();

        base.OnDisable();
    }

    public void HandleSelection()
    {
        if (!isInitialized)
        {
            return;
        }

        OnSelected?.Invoke(this);
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
