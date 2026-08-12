using System.Collections.Generic;
using UnityEngine;

public class UnitRuntime : MonoBehaviour
{
    protected List<Vector2Int> defaultAttackPattern = new List<Vector2Int>();    // Left-facing default attack pattern
    protected List<Vector2Int> resolvedAttackPattern = new List<Vector2Int>();
    protected CombatGridCell activeCell;
    
    // Unit Components
    [SerializeField] protected Health health;
    [SerializeField] protected UnitVisual unitVisual;
    [SerializeField] protected CombatGrid combatGrid;

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
    public virtual UnitAttackType AttackType => UnitAttackType.Melee; // Default attack type, can be overridden in derived classes

    // Getters
    public CombatGridCell ActiveCell => activeCell;
    public Vector3 WorldPosition => transform.position;

    public Vector3Int ActiveCellPosition => activeCell != null ? activeCell.CellPosition : Vector3Int.zero;
    public Vector2Int FacingDirection => facingDirection;
    public IReadOnlyList<Vector2Int> ResolvedAttackPattern => resolvedAttackPattern;
    public TeamIdentity TeamIdentity => teamIdentity;
    public virtual Vector2 CenterOffset => centerOffset;
    public bool IsInitialized => isInitialized;

    public void SetActiveCell(CombatGridCell cell)
    {
        activeCell = cell;
    }
    
    public void ClearActiveCell()
    {
        activeCell = null;
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

        health.Initialize(Stats.MaxHealth);
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

        targetScanner.Initialize(combatGrid, teamIdentity);
        targetSelector.Initialize(targetPriorityMode);

        normalAttackController.Initialize(Stats.Attack, Stats.AttackInterval, targetScanner, targetSelector, unitVisual);
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
