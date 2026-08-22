using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitRuntime : MonoBehaviour
{
    private const float deathStateDuration = 0.2f;

    protected UnitCombatContext combatContext;
    protected CombatGridCell activeCell;

    protected UnitStats runtimeStats;

    // States
    protected UnitRuntimeState currentState = UnitRuntimeState.Idle;
    private CountdownTimer actionStateTimer = new CountdownTimer(0f);
    private bool hasNotifiedDestroyed;
    
    // Unit Components
    [SerializeField] protected Health health;
    [SerializeField] protected UnitVisual unitVisual;
    [SerializeField] protected UnitMovement unitMovement;
    [SerializeField] protected Hurtbox hurtbox;

    // Unit Battle System
    [SerializeField] protected TeamIdentity battleTeam;

    // Facing Direction
    protected Vector2Int facingDirection = Vector2Int.left;

    // Unit Offset Customization
    protected Vector2 centerOffset = new Vector2(0f, 0.5f);

    // Initialization State
    protected bool isInitialized;

    // Stats
    public UnitStats Stats => runtimeStats; // Must be provided by a concrete runtime.
    public float MaxHealth => Stats != null ? Stats.MaxHealth : 0f;
    public float CurrentHealth => health.CurrentHealth;
    public float Attack => Stats != null ? Stats.Attack : 0f;
    public float AttackInterval => Stats != null ? Stats.AttackInterval : UnitStats.MinAttackInterval;
    public float Defense => Stats != null ? Stats.Defense : 0f;
    public float SpecialDefense => Stats != null ? Stats.SpecialDefense : 0f;
    
    // Movement System
    public virtual UnitMovementType MovementType => UnitMovementType.Ground;

    // Getters
    public Health Health => health;
    public UnitVisual Visual => unitVisual;
    public UnitMovement Movement => unitMovement;
    public Hurtbox Hurtbox => hurtbox;
    public CombatGridCell ActiveCell => activeCell;
    public CombatGrid CombatGrid => combatContext?.CombatGrid;
    public Vector3 WorldPosition => transform.position;

    public Vector3Int ActiveCellPosition => activeCell != null ? activeCell.CellPosition : Vector3Int.zero;
    public Vector2Int FacingDirection => facingDirection;
    public TeamIdentity BattleTeam => battleTeam;
    public virtual Vector2 CenterOffset => centerOffset;
    public Vector3 CenterPosition => transform.position + (Vector3)CenterOffset;

    public UnitRuntimeState CurrentState => currentState;

    // State Checks
    public bool IsDead => currentState == UnitRuntimeState.Dead || health.IsDead;
    public virtual bool IsMovementBlocked => false;
    public bool CanMove => !IsDead && !IsMovementBlocked && (currentState == UnitRuntimeState.Idle || currentState == UnitRuntimeState.Moving);
    public bool CanUseNormalAttack => !IsDead && currentState == UnitRuntimeState.Idle;

    public event Action<UnitRuntime, UnitRuntimeState, UnitRuntimeState> OnStateChanged;
    public event Action<UnitRuntime> OnDestroyed;

    public bool IsInitialized => isInitialized;

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

    protected virtual void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        facingDirection = direction;
        unitVisual.SetDirection(direction);
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
        health.Initialize(Stats);
        health.OnDied -= HandleDied;
        health.OnDied += HandleDied;
        return true;
    }

    protected bool InitializeMovementSystem(UnitStats combatStats, UnitMovementType movementType)
    {
        return unitMovement.Initialize(combatStats, movementType);
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
        if (combatContext != null && combatContext.IsValid && combatContext.CombatGrid.Grid != null && unitVisual != null && battleTeam != null && hurtbox != null)
        {
            return true;
        }

        Debug.LogError("[UnitRuntime] Core runtime requires a valid CombatReferencesContext, CombatGrid with Grid, UnitVisual, TeamIdentity, and primary Hurtbox.", this);
        return false;
    }

    protected bool CheckHealthSystemReferences()
    {
        if (Stats != null && health != null)
        {
            return true;
        }

        Debug.LogError("[UnitRuntime] Health system requires UnitCombatStats and Health.", this);
        return false;
    }

    protected bool CheckMovementSystemReferences()
    {
        if (Stats != null && unitMovement != null)
        {
            return true;
        }

        Debug.LogError("[UnitRuntime] Movement system requires UnitCombatStats and UnitMovement.", this);
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
        if (battleTeam == null)
        {
            battleTeam = GetComponent<TeamIdentity>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (unitMovement == null)
        {
            unitMovement = GetComponent<UnitMovement>();
        }

        if (unitVisual == null)
        {
            unitVisual = GetComponentInChildren<UnitVisual>();
        }

        if (hurtbox == null)
        {
            hurtbox = GetComponentInChildren<Hurtbox>(true);
        }
    }
}
