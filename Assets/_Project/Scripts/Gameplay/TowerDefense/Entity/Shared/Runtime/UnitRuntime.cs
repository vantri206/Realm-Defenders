using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitRuntime : MonoBehaviour
{
    private const float normalAttackStateDuration = 0.3f;
    private const float deathStateDuration = 0.2f;

    protected List<Vector2Int> defaultAttackPattern = new List<Vector2Int>();    // Left-facing default attack pattern
    protected List<Vector2Int> resolvedAttackPattern = new List<Vector2Int>();
    protected UnitCombatContext combatContext;
    protected CombatGridCell activeCell;

    // States
    protected UnitRuntimeState currentState = UnitRuntimeState.Idle;
    private CountdownTimer actionStateTimer = new CountdownTimer(0f);
    private bool hasNotifiedDestroyed;
    
    // Unit Components
    [SerializeField] protected Health health;
    [SerializeField] protected UnitVisual unitVisual;
    [SerializeField] protected UnitMovement unitMovement;

    // Unit Battle System
    [SerializeField] protected TeamIdentity teamIdentity;
    [SerializeField] protected TargetScanner targetScanner;
    [SerializeField] protected TargetSelector targetSelector;
    [SerializeField] protected NormalAttackController normalAttackController;

    // Facing Direction
    protected Vector2Int facingDirection = Vector2Int.left;

    // Unit Offset Customization
    protected Vector2 centerOffset = new Vector2(0f, 0.5f);

    // Initialization State
    protected bool isInitialized;

    // Stats
    public virtual UnitStats Stats => null; // Must be provided by a concrete runtime.
    public float MaxHealth => health.MaxHealth;
    public float CurrentHealth => health.CurrentHealth;
    public float Attack => Stats.Attack;
    public float AttackInterval => Stats.AttackInterval;
    public float Defense => Stats.Defense;
    public float SpecialDefense => Stats.SpecialDefense;

    // Properties
    public virtual UnitMovementType MovementType => UnitMovementType.Ground;
    public virtual UnitAttackType AttackType => UnitAttackType.Melee; // Default attack type, can be overridden in derived classes

    // Getters
    public UnitVisual Visual => unitVisual;
    public UnitMovement Movement => unitMovement;
    public CombatGridCell ActiveCell => activeCell;
    public CombatGrid CombatGrid => combatContext?.CombatGrid;
    public Vector3 WorldPosition => transform.position;

    public Vector3Int ActiveCellPosition => activeCell != null ? activeCell.CellPosition : Vector3Int.zero;
    public Vector2Int FacingDirection => facingDirection;
    public IReadOnlyList<Vector2Int> ResolvedAttackPattern => resolvedAttackPattern;
    public TeamIdentity TeamIdentity => teamIdentity;
    public virtual Vector2 CenterOffset => centerOffset;
    public Vector3 CenterPosition => transform.position + (Vector3)CenterOffset;

    public bool IsInitialized => isInitialized;
    public UnitRuntimeState CurrentState => currentState;
    public Health Health => health;
    public bool IsDead => currentState == UnitRuntimeState.Dead || health.IsDead;
    public virtual bool IsMovementBlocked => false;
    public bool CanMove => !IsDead && !IsMovementBlocked && (currentState == UnitRuntimeState.Idle || currentState == UnitRuntimeState.Moving);
    public bool CanUseNormalAttack => !IsDead && currentState == UnitRuntimeState.Idle;

    public virtual TargetSide TargetSide => TargetSide.Enemy;
    public virtual AttackEffect AttackEffect => AttackEffect.Damage;
    public virtual AttackMethod AttackMethod => AttackMethod.DirectTarget; // Default attack method, can be overridden in derived classes
    public virtual AttackDamageType AttackDamageType => AttackDamageType.PhysicalDamage;
    public virtual float NormalAttackEffectMultiplier => 1f;
    public virtual AttackProjectile NormalAttackProjectilePrefab => null;
    public virtual AttackAOEHit NormalAttackAOEHitPrefab => null;
    public virtual SimpleSpriteAnimatorVFX NormalAttackHitVFXPrefab => null;
    public virtual ParticleVFX NormalAttackHealVFXPrefab => null;

    public event Action<UnitRuntime, UnitRuntimeState, UnitRuntimeState> OnStateChanged;
    public event Action<UnitRuntime> OnDestroyed;

    protected virtual void OnDisable()
    {
        ClearActiveCell();
    }

    public virtual void RemoveCombat()
    {
        if (hasNotifiedDestroyed)
        {
            return;
        }

        hasNotifiedDestroyed = true;
        OnDestroyed?.Invoke(this);
    }

    public void SetActiveCell(CombatGridCell cell)
    {
        if (activeCell == cell)
        {
            return; // No change in active cell
        }

        if (activeCell != null)
        {
            activeCell.RemoveUnit(this);
        }

        activeCell = cell;

        if (activeCell != null)
        {
            activeCell.AddUnit(this);
        }
    }
    
    public void ClearActiveCell()
    {
        SetActiveCell(null);
    }

    protected bool TryStartSkillCasting(float duration)
    {
        return TryStartActionState(UnitRuntimeState.SkillCasting, duration);
    }

    protected void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        facingDirection = direction;
        unitVisual.SetDirection(direction);

        resolvedAttackPattern = AttackPatternResolver.RefreshAttackPattern(defaultAttackPattern, facingDirection);
    }

    public void FacePosition(Vector2 targetPosition)
    {
        Vector2 direction = targetPosition - (Vector2)CenterPosition;
        Vector2Int resolvedDirection = ResolveFourDirection(direction);
        if (resolvedDirection != Vector2Int.zero)
        {
            SetFacingDirection(resolvedDirection);
        }
    }

    protected static Vector2Int ResolveFourDirection(Vector2 direction)
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

    protected bool InitializeHealth()
    {
        health.Initialize(Stats.MaxHealth, Stats.Defense, Stats.SpecialDefense);
        health.OnDied -= HandleDied;
        health.OnDied += HandleDied;
        return true;
    }

    protected bool InitializeAttackSystems(TargetPriorityMode targetPriorityMode)
    {
        if (!targetScanner.Initialize(combatContext.CombatGrid, this) ||
            !targetSelector.Initialize(this, targetPriorityMode) ||
            !normalAttackController.Initialize(Stats, NormalAttackEffectMultiplier, AttackEffect,
                                               TargetSide, AttackType, AttackDamageType,
                                               AttackMethod, NormalAttackProjectilePrefab,
                                               NormalAttackAOEHitPrefab, NormalAttackHitVFXPrefab,
                                               NormalAttackHealVFXPrefab,
                                               targetScanner, targetSelector, unitVisual,
                                               combatContext.CombatTime))
        {
            return false;
        }

        normalAttackController.OnAttack -= HandleNormalAttack;
        normalAttackController.OnAttack += HandleNormalAttack;
        return true;
    }

    protected bool InitializeMovementSystem(UnitSpeed speed, UnitMovementType movementType)
    {
        return unitMovement.Initialize(speed, movementType);
    }

    protected bool ChangeState(UnitRuntimeState newState)
    {
        if (currentState == UnitRuntimeState.Dead || currentState == newState)
        {
            return false;
        }

        UnitRuntimeState previousState = currentState;
        currentState = newState;
        OnStateChanged?.Invoke(this, previousState, currentState);
        return true;
    }

    protected void TickState(float deltaTime)
    {
        bool isTimerState = currentState == UnitRuntimeState.Attacking || 
                            currentState == UnitRuntimeState.SkillCasting || 
                            currentState == UnitRuntimeState.Dead;

        if (!isTimerState)  
    {
            return;
        }

        actionStateTimer.Tick(deltaTime);

        if (!actionStateTimer.IsFinished)
        {
            return;
        }

        if (currentState == UnitRuntimeState.Dead)
        {
            RemoveCombat();
            Destroy(gameObject);
            return;
        }

        ChangeState(UnitRuntimeState.Idle);
    }

    protected bool TryStartActionState(UnitRuntimeState actionState, float duration)
    {
        if (IsDead)
        {
            return false;
        }

        if (actionState == UnitRuntimeState.Attacking && !CanUseNormalAttack)
        {
            return false;
        }

        if (!ChangeState(actionState))
        {
            return false;
        }

        StartStateTimer(duration);
        return true;
    }

    protected void SetMovementState(bool isMoving)
    {
        if (IsDead)
        {
            ChangeState(UnitRuntimeState.Dead);
            return;
        }

        if (!CanMove)
        {
            if (currentState == UnitRuntimeState.Moving)
            {
                ChangeState(UnitRuntimeState.Idle);
            }
            return;
        }

        ChangeState(isMoving ? UnitRuntimeState.Moving : UnitRuntimeState.Idle);
    }

    protected virtual void HandleNormalAttack(Hurtbox target)
    {
        TryStartActionState(UnitRuntimeState.Attacking, normalAttackStateDuration);
    }

    protected virtual void HandleDied()
    {
        if (currentState == UnitRuntimeState.Dead)
        {
            return;
        }

        ChangeState(UnitRuntimeState.Dead);

        StartStateTimer(deathStateDuration);

        unitMovement.SetMoveDirection(Vector2.zero);
        unitVisual.SetIsMoving(false);
        unitVisual.TriggerDie();
    }

    protected void SetupVisuals(Sprite sprite, RuntimeAnimatorController animatorController)
    {
        unitVisual.Initialize(sprite, animatorController);
    }

    protected bool CheckCoreReferences()
    {
        if (combatContext != null && combatContext.IsValid && combatContext.CombatGrid.Grid != null && unitVisual != null && teamIdentity != null)
        {
            return true;
        }

        Debug.LogError("[UnitRuntime] Core runtime requires a valid CombatReferencesContext, CombatGrid with Grid, UnitVisual, and TeamIdentity.", this);
        return false;
    }

    protected bool CheckHealthSystemReferences()
    {
        if (Stats != null && health != null)
        {
            return true;
        }

        Debug.LogError("[UnitRuntime] Health system requires UnitStats and Health.", this);
        return false;
    }

    protected bool CheckMovementSystemReferences(UnitSpeed speed)
    {
        if (speed != null && unitMovement != null)
        {
            return true;
        }

        Debug.LogError("[UnitRuntime] Movement system requires UnitSpeed and UnitMovement.", this);
        return false;
    }

    protected bool CheckAttackSystemReferences()
    {
        if (targetScanner != null && targetSelector != null && normalAttackController != null)
        {
            return true;
        }

        Debug.LogError("[UnitRuntime] Attack system requires TargetScanner, TargetSelector, and NormalAttackController.", this);
        return false;
    }

    protected void StartStateTimer(float duration)
    {
        actionStateTimer.StopTimer();
        actionStateTimer.Reset(duration);
        actionStateTimer.StartTimer();
    }

    
    protected virtual void CacheReferences()
    {
        if (teamIdentity == null)
        {
            teamIdentity = GetComponent<TeamIdentity>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (targetScanner == null)
        {
            targetScanner = GetComponent<TargetScanner>();
        }

        if (targetSelector == null)
        {
            targetSelector = GetComponent<TargetSelector>();
        }

        if (normalAttackController == null)
        {
            normalAttackController = GetComponent<NormalAttackController>();
        }

        if (unitMovement == null)
        {
            unitMovement = GetComponent<UnitMovement>();
        }

        if (unitVisual == null)
        {
            unitVisual = GetComponentInChildren<UnitVisual>();
        }
    }
}
