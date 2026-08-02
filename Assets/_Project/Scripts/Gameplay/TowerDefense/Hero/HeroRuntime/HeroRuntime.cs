using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HeroGridPosition))]
public class HeroRuntime : MonoBehaviour
{
    private readonly List<Vector2Int> resolvedAttackPattern = new List<Vector2Int>();

    private HeroInstance heroInstance;
    private HeroDefinition heroDefinition;
    private CombatGrid combatGrid;
    private Vector2Int facingDirection = Vector2Int.left;

    private HeroGridPosition heroGridPosition;
    private TeamIdentity teamIdentity;
    private Health health;
    private TargetScanner targetScanner;
    private TargetSelector targetSelector;
    private NormalAttackController normalAttackController;
    private UnitVisual unitVisual;

    private bool isInitialized;

    public HeroInstance Instance => heroInstance;
    public HeroDefinition Definition => heroDefinition;
    public CombatGrid CombatGrid => combatGrid;
    public HeroGridPosition GridPosition => heroGridPosition;
    public Vector2Int FacingDirection => facingDirection;
    public IReadOnlyList<Vector2Int> ResolvedAttackPattern => resolvedAttackPattern;
    public TeamIdentity TeamIdentity => teamIdentity;
    public Health Health => health;
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
        heroGridPosition?.Initialize(combatGrid, currentCell);
        InitializeComponents();
        RefreshAttackPattern();

        isInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }
        
        normalAttackController?.Tick(deltaTime, GridPosition.CurrentCell, resolvedAttackPattern);
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
        RefreshAttackPattern();
    }

    private void InitializeComponents()
    {
        if (heroDefinition == null)
        {
            return;
        }

        targetScanner?.Initialize(combatGrid, teamIdentity);
        targetSelector?.Initialize(heroDefinition.TargetPriorityMode);
        normalAttackController?.Initialize(heroDefinition.Attack, heroDefinition.AttackInterval);
        health?.Initialize(heroDefinition.MaxHealth);
    }

    private void RefreshAttackPattern()
    {
        resolvedAttackPattern.Clear();

        if (heroDefinition == null)
        {
            return;
        }

        IReadOnlyList<Vector2Int> patternOffsets = heroDefinition.AttackPattern;
        for (int i = 0; i < patternOffsets.Count; i++)
        {
            resolvedAttackPattern.Add(patternOffsets[i]);
        }
    }

    private void CacheReferences()
    {
        if (heroGridPosition == null)
        {
            heroGridPosition = GetComponent<HeroGridPosition>();
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
