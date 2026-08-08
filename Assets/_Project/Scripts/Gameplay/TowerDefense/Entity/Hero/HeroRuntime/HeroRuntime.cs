using System.Collections.Generic;
using UnityEngine;

public class HeroRuntime : MonoBehaviour
{
    private List<Vector2Int> defaultAttackPattern = new List<Vector2Int>();    // Left-facing default attack pattern
    private List<Vector2Int> resolvedAttackPattern = new List<Vector2Int>();

    private CombatGrid combatGrid;

    // Hero Identity
    private HeroInstance heroInstance;
    private HeroDefinition heroDefinition;

    // Hero Stats
    [SerializeField] private Health health;
    [SerializeField] private UnitVisual unitVisual;
    [SerializeField] private UnitGridPosition heroGridPosition;

    // Attacker and Skills
    [SerializeField] private TeamIdentity teamIdentity;
    [SerializeField] private TargetScanner targetScanner;
    [SerializeField] private TargetSelector targetSelector;
    [SerializeField] private NormalAttackController normalAttackController;

    private Vector2Int facingDirection = Vector2Int.left;

    private bool isInitialized;

    // Stats
    public UnitStats Stats => heroInstance != null ? heroInstance.Stats : null;
    public HeroBlocker Blocker => heroInstance != null ? heroInstance.Blocker : null;
    public float MaxHealth => health != null ? health.MaxHealth : 0f;
    public float CurrentHealth => health != null ? health.CurrentHealth : 0f;
    public float Attack => Stats != null ? Stats.Attack : 0f;
    public float AttackInterval => Stats != null ? Stats.AttackInterval : 0f;
    public float Defense => Stats != null ? Stats.Defense : 0f;
    public float SpecialDefense => Stats != null ? Stats.SpecialDefense : 0f;
    public int BlockCount => Blocker != null ? Blocker.BlockCount : 0;
    public int CurrentBlock => Blocker != null ? Blocker.CurrentBlock : 0;

    public HeroInstance Instance => heroInstance;
    public HeroDefinition Definition => heroDefinition;
    public UnitGridPosition GridPosition => heroGridPosition;
    public Vector2Int FacingDirection => facingDirection;
    public HeroAttackType AttackType => heroDefinition.AttackType;
    public IReadOnlyList<Vector2Int> ResolvedAttackPattern => resolvedAttackPattern;
    public TeamIdentity TeamIdentity => teamIdentity;
    public bool IsInitialized => isInitialized;

    public void Initialize(HeroInstance heroInstance, CombatGrid combatGrid, Vector3Int currentCell)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return;
        }

        this.heroInstance = heroInstance;
        heroDefinition = heroInstance.Definition;
        this.combatGrid = combatGrid;

        CacheReferences();
        SetupVisuals();
        InitializeStats();
        InitializeComponents();
        
        SetCurrentCell(currentCell);
        
        defaultAttackPattern = new List<Vector2Int>(heroDefinition.AttackPattern);

        isInitialized = true;
    }

    public void Update()
    {
        if (!isInitialized)
        {
            return;
        }
        
        normalAttackController?.Tick(Time.deltaTime, heroGridPosition.CurrentCell, resolvedAttackPattern);
    }

    public void SetCurrentCell(Vector3Int cellPosition)
    {
        heroGridPosition?.SetCell(cellPosition);
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        facingDirection = direction;
        unitVisual?.SetDirection(direction);

        resolvedAttackPattern = AttackPatternResolver.RefreshAttackPattern(defaultAttackPattern, facingDirection);
    }

    private void SetupVisuals()
    {
        if (unitVisual == null || heroDefinition == null)
        {
            return;
        }

        unitVisual.Initialize(heroDefinition.HeroSprite, heroDefinition.AnimatorController);
    }

    private void InitializeStats()
    {
        if (heroInstance == null)
        {
            return;
        }

        health.Initialize(heroInstance.Stats.MaxHealth);
    }

    private void InitializeComponents()
    {
        if (heroDefinition == null)
        {
            return;
        }

        targetScanner?.Initialize(combatGrid, teamIdentity);
        targetSelector?.Initialize(heroDefinition.TargetPriorityMode);

        normalAttackController?.Initialize(Stats.Attack, Stats.AttackInterval, targetScanner, targetSelector, unitVisual);
    }
    
    private void CacheReferences()
    {
        if (heroGridPosition == null)
        {
            heroGridPosition = GetComponent<UnitGridPosition>();
        }

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
