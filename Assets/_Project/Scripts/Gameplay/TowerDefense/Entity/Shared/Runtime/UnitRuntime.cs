using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitRuntime : MonoBehaviour
{
    private const float normalAttackStateDuration = 0.3f;
    private const float deathStateDuration = 0.2f;

    protected List<Vector2Int> defaultAttackPattern = new List<Vector2Int>();    // Left-facing default attack pattern
    protected List<Vector2Int> resolvedAttackPattern = new List<Vector2Int>();
    protected CombatGrid combatGrid;
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
    public virtual UnitStats Stats => new UnitStats(); // To be overridden in derived classes
    public float MaxHealth => health != null ? health.MaxHealth : 0f;
    public float CurrentHealth => health != null ? health.CurrentHealth : 0f;
    public float Attack => Stats != null ? Stats.Attack : 0f;
    public float AttackInterval => Stats != null ? Stats.AttackInterval : 0f;
    public float Defense => Stats != null ? Stats.Defense : 0f;
    public float SpecialDefense => Stats != null ? Stats.SpecialDefense : 0f;

    // Properties
    public virtual UnitMovementType MovementType => UnitMovementType.Ground;
    public virtual UnitAttackType AttackType => UnitAttackType.Melee; // Default attack type, can be overridden in derived classes

    // Getters
    public UnitVisual Visual => unitVisual;
    public CombatGridCell ActiveCell => activeCell;
    public CombatGrid CombatGrid => combatGrid;
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
    public bool IsDead => currentState == UnitRuntimeState.Dead || (health != null && health.IsDead);
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
    public event Action OnDestoryed;

    protected virtual void OnDisable()
    {
        ClearActiveCell();
    }

    public void RemoveCombat()
    {
        if (hasNotifiedDestroyed)
        {
            return;
        }

        hasNotifiedDestroyed = true;
        OnDestoryed?.Invoke();
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

    public bool TryStartSkillCasting(float duration)
    {
        return TryStartActionState(UnitRuntimeState.SkillCasting, duration);
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        facingDirection = direction;
        if (unitVisual == null)
        {
            Debug.LogError("[UnitRuntime] UnitVisual component is required to set facing direction.", this);
        }
        else
        {
            unitVisual.SetDirection(direction);
        }

        resolvedAttackPattern = AttackPatternResolver.RefreshAttackPattern(defaultAttackPattern, facingDirection);
    }

    protected virtual void InitializeStats()
    {
        
    }

    protected void InitializeHealth()
    {
        if (Stats == null)
        {
            Debug.LogError("[UnitRuntime] UnitStats are required to initialize Health.", this);
            return;
        }

        if (health == null)
        {
            Debug.LogError("[UnitRuntime] Health component is required to initialize unit health.", this);
            return;
        }

        health.Initialize(Stats.MaxHealth, Stats.Defense, Stats.SpecialDefense);
        health.OnDied -= HandleDied;
        health.OnDied += HandleDied;
    }

    protected void InitializeAttackSystems(TargetPriorityMode targetPriorityMode)
    {
        if (Stats == null)
        {
            Debug.LogError("[UnitRuntime] UnitStats are required to initialize attack systems.", this);
            return;
        }

        if (targetScanner == null)
        {
            Debug.LogError("[UnitRuntime] TargetScanner component is required to initialize attack systems.", this);
            return;
        }

        if (combatGrid == null)
        {
            Debug.LogError("[UnitRuntime] CombatGrid is required to initialize attack systems.", this);
            return;
        }

        if (teamIdentity == null)
        {
            Debug.LogError("[UnitRuntime] TeamIdentity component is required to initialize attack systems.", this);
            return;
        }

        if (targetSelector == null)
        {
            Debug.LogError("[UnitRuntime] TargetSelector component is required to initialize attack systems.", this);
            return;
        }

        if (normalAttackController == null)
        {
            Debug.LogError("[UnitRuntime] NormalAttackController component is required to initialize attack systems.", this);
            return;
        }

        targetScanner.Initialize(combatGrid, this);
        targetSelector.Initialize(this, targetPriorityMode);

        normalAttackController.Initialize(Stats, NormalAttackEffectMultiplier, AttackEffect,
                                           TargetSide, AttackType, AttackDamageType,
                                           AttackMethod, NormalAttackProjectilePrefab,
                                           NormalAttackAOEHitPrefab, NormalAttackHitVFXPrefab,
                                           NormalAttackHealVFXPrefab,
                                           targetScanner, targetSelector, unitVisual);
        normalAttackController.OnAttack -= HandleNormalAttack;
        normalAttackController.OnAttack += HandleNormalAttack;
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

    protected void HandleNormalAttack(Hurtbox target)
    {
        TryStartActionState(UnitRuntimeState.Attacking, normalAttackStateDuration);
    }

    protected void HandleDied()
    {
        if (currentState == UnitRuntimeState.Dead)
        {
            return;
        }

        ChangeState(UnitRuntimeState.Dead);

        StartStateTimer(deathStateDuration);

        if (unitMovement != null)
        {
            unitMovement.SetMoveDirection(Vector2.zero);
        }

        if (unitVisual != null)
        {
            unitVisual.SetIsMoving(false);
            unitVisual.TriggerDie();
        }
    }

    protected void SetupVisuals(Sprite sprite, RuntimeAnimatorController animatorController)
    {
        if (unitVisual == null)
        {
            Debug.LogError("[UnitRuntime] UnitVisual component is required to setup unit visuals.", this);
            return;
        }

        unitVisual.Initialize(sprite, animatorController);
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

        if (unitVisual == null)
        {
            unitVisual = GetComponentInChildren<UnitVisual>();
        }
    }
}
