using System.Collections.Generic;
using UnityEngine;

public class HeroRuntime : UnitRuntime
{

    // Hero Identity
    private HeroInstance heroInstance;
    [SerializeField] private HeroDefinition heroDefinition;

    // Hero Components
    [SerializeField] private HeroBlocker heroBlocker;
    private CombatGridCell anchorCell;

    // Hero Stats
    public override UnitStats Stats => heroInstance != null ? heroInstance.Stats : null;
    public HeroBlock Blocker => heroInstance != null ? heroInstance.Blocker : null;
    public int BlockCount => Blocker != null ? Blocker.BlockCount : 0;
    public int CurrentBlock => Blocker != null ? Blocker.CurrentBlock : 0;
    public override UnitAttackType AttackType => heroDefinition != null ? heroDefinition.AttackType : base.AttackType;

    // Getters
    public HeroInstance Instance => heroInstance;
    public HeroDefinition Definition => heroDefinition;
    public HeroBlocker HeroBlocker => heroBlocker;
    public CombatGridCell AnchorCell => anchorCell;

    public void Initialize(HeroInstance heroInstance, CombatGrid combatGrid, Vector3Int currentCell)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogError("[HeroRuntime] A valid HeroInstance is required to initialize hero runtime.", this);
            return;
        }

        this.heroInstance = heroInstance;
        heroDefinition = heroInstance.Definition;
        this.combatGrid = combatGrid;

        CacheReferences();
        SetupVisuals(heroDefinition.HeroSprite, heroDefinition.AnimatorController);
        InitializeStats();
        InitializeAttackSystems(heroDefinition.TargetPriorityMode);
    
        SetActiveCell(combatGrid.TryGetCell(currentCell, out CombatGridCell activeCell) ? activeCell : null);
        SetAnchorCell(combatGrid.TryGetCell(currentCell, out CombatGridCell anchorCell) ? anchorCell : null);

        defaultAttackPattern = new List<Vector2Int>(heroDefinition.AttackPattern);
        SetFacingDirection(facingDirection);

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

        normalAttackController.Tick(Time.deltaTime, resolvedAttackPattern);
    }

    protected override void InitializeStats()
    {
        InitializeHealth();
    }

    public void SetAnchorCell(CombatGridCell cell)
    {
        anchorCell = cell;
    }

    public void ClearAnchorCell()
    {
        anchorCell = null;
    }

    protected override void CacheReferences()
    {
        base.CacheReferences();

        if (heroBlocker == null)
        {
            heroBlocker = GetComponent<HeroBlocker>();
        }
    }
}
