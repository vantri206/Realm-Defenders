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
    private readonly List<Hurtbox> skillTargets = new List<Hurtbox>();

    private BaseSkill passiveSkill;
    private AutoActiveSkill activeSkill;
    private SkillDefinition passiveSkillRuntimeDefinition;
    private SkillDefinition activeSkillRuntimeDefinition;
    private SkillAttackController skillAttackController;

    // Hero Components
    [SerializeField] private HeroBlocker heroBlocker;
    [SerializeField] protected TargetScanner targetScanner;
    [SerializeField] protected TargetSelector targetSelector;
    [SerializeField] protected NormalAttackController normalAttackController;
    [SerializeField] private CombatStatHUD combatStatHUD;

    private bool hasBlocker;

    // Hero Stats
    public int BlockCount => Stats.BlockCount;
    public int CurrentBlock => heroBlocker != null ? heroBlocker.CurrentBlockCount : 0;

    // Getters
    public HeroCombatState CombatState => combatState;
    public HeroDefinition Definition => heroDefinition;
    public HeroBlocker HeroBlocker => heroBlocker;
    public CombatGridCell AnchorCell => anchorCell;
    public BaseSkill PassiveSkill => passiveSkill;
    public AutoActiveSkill ActiveSkill => activeSkill;
    public NormalAttackController NormalAttackController => normalAttackController;
    public SkillAttackController SkillAttackController => skillAttackController;

    // Attack Pattern
    public NormalAttackDefinition NormalAttackDefinition => heroDefinition != null ? heroDefinition.NormalAttackDefinition : null;
    public IReadOnlyList<Vector2Int> ResolvedAttackPattern => resolvedAttackPattern;
    public HeroBlockState BlockState => heroBlocker != null ? heroBlocker.BlockState : HeroBlockState.NonBlocking;
    public override bool IsMovementBlocked => base.IsMovementBlocked || BlockState == HeroBlockState.Blocking || (activeSkill != null && activeSkill.IsActiving);
    public override bool CanUseNormalAttack => base.CanUseNormalAttack && (activeSkill == null || !activeSkill.IsActiving);

    public event Action<HeroRuntime> OnSelected;

    public void Initialize(HeroCombatState combatState, UnitCombatContext combatContext, Vector3Int currentCell)
    {
        isInitialized = false;
        ClearSkills();

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

        if (!CheckCoreReferences() || !CheckHealthSystemReferences() || !CheckMovementSystemReferences() || !CheckAttackSystemReferences() || !CheckBlockSystemReferences())
        {
            return;
        }

        SetupVisuals(heroDefinition.HeroDefaultSprite, heroDefinition.AnimatorController);
        if (!InitializeMovementSystem(Stats, MovementType))
        {
            return;
        }

        if (!InitializeHealthAndStatus())
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
            normalAttackController.OnNormalAttackFired -= HandleNormalAttackFired;
            normalAttackController.OnNormalAttackFired += HandleNormalAttackFired;
        }

        skillAttackController = new SkillAttackController();
        isInitialized = true;
        InitializeSkills();
        RefreshSkillCharge();
    }

    protected void OnDestroy()
    {
        ClearSkills();

        if (normalAttackController != null)
        {
            normalAttackController.OnNormalAttackFired -= HandleNormalAttackFired;
        }
    }

    protected override void OnDisable()
    {
        ClearSkills();

        if (heroBlocker != null)
        {
            heroBlocker.ClearBlocks();
        }

        ClearAnchorCell();

        if (normalAttackController != null)
        {
            normalAttackController.OnNormalAttackFired -= HandleNormalAttackFired;
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
        TickRuntime(combatDeltaTime);
        TickSkills(combatDeltaTime);

        if (IsMovementBlocked && unitMovement.CurrentMoveDirection != Vector2.zero)
        {
            SetMovementDirection(Vector2.zero);
        }

        normalAttackController.Tick(combatDeltaTime, ResolvedAttackPattern, CanUseNormalAttack);
        RefreshSkillCharge();
        ResetFacingDirection(unitMovement.CurrentMoveDirection);
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

    public bool HasSkillTarget(IReadOnlyList<Vector2Int> pattern, UnitAttackType attackType)
    {
        if (targetScanner == null || !targetScanner.IsInitialized || pattern == null)
        {
            return false;
        }

        targetScanner.Scan(CenterPosition, pattern, TargetSide.Enemy, AttackEffect.Damage, attackType, skillTargets);
        return skillTargets.Count > 0;
    }

    public bool TrySelectSkillTarget(IReadOnlyList<Vector2Int> pattern, TargetSide targetSide, AttackEffect attackEffect,
                                     UnitAttackType attackType, out Hurtbox target)
    {
        target = null;
        if (targetScanner == null || !targetScanner.IsInitialized || targetSelector == null || pattern == null)
        {
            return false;
        }

        targetScanner.Scan(CenterPosition, pattern, targetSide, attackEffect, attackType, skillTargets);
        target = targetSelector.SelectTarget(skillTargets, CenterPosition, NormalAttackDefinition.TargetPriorityMode, attackType);
        return target != null;
    }

    public void CollectSkillTargets(IReadOnlyList<Vector2Int> pattern, TargetSide targetSide, AttackEffect attackEffect,
                                    UnitAttackType attackType, List<Hurtbox> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        if (targetScanner == null || !targetScanner.IsInitialized || pattern == null)
        {
            return;
        }

        targetScanner.Scan(CenterPosition, pattern, targetSide, attackEffect, attackType, results);
    }

    public void TriggerSkillAttackAnimation()
    {
        unitVisual.TriggerAttack();
    }

    private void InitializeSkills()
    {
        if (heroDefinition.PassiveSkill != null)
        {
            passiveSkillRuntimeDefinition = Instantiate(heroDefinition.PassiveSkill);
            passiveSkillRuntimeDefinition.hideFlags = HideFlags.DontSave;
            passiveSkill = passiveSkillRuntimeDefinition.Skill;

            if (passiveSkill is AutoActiveSkill)
            {
                Debug.LogError("[HeroRuntime] Passive skill definition cannot contain an AutoActiveSkill.", heroDefinition.PassiveSkill);
                passiveSkill = null;
            }
            else if (passiveSkill != null)
            {
                passiveSkill.Initialize(this, passiveSkillRuntimeDefinition);
            }
        }

        if (heroDefinition.ActiveSkill != null)
        {
            activeSkillRuntimeDefinition = Instantiate(heroDefinition.ActiveSkill);
            activeSkillRuntimeDefinition.hideFlags = HideFlags.DontSave;

            BaseSkill activeSkillRuntime = activeSkillRuntimeDefinition.Skill;
            activeSkill = activeSkillRuntime as AutoActiveSkill;

            if (activeSkillRuntime != null && activeSkill == null)
            {
                Debug.LogError("[HeroRuntime] Active skill definition must contain an AutoActiveSkill.", heroDefinition.ActiveSkill);
            }
            else if (activeSkill != null)
            {
                activeSkill.Initialize(this, activeSkillRuntimeDefinition);
            }
        }
    }

    private void TickSkills(float deltaTime)
    {
        if (passiveSkill != null)
        {
            passiveSkill.Tick(deltaTime);
        }

        if (activeSkill != null)
        {
            activeSkill.Tick(deltaTime);
        }
    }

    private void ClearSkills()
    {
        if (passiveSkill != null)
        {
            passiveSkill.ClearData();
            passiveSkill = null;
        }

        if (activeSkill != null)
        {
            activeSkill.ClearData();
            activeSkill = null;
        }

        if (passiveSkillRuntimeDefinition != null)
        {
            Destroy(passiveSkillRuntimeDefinition);
            passiveSkillRuntimeDefinition = null;
        }

        if (activeSkillRuntimeDefinition != null)
        {
            Destroy(activeSkillRuntimeDefinition);
            activeSkillRuntimeDefinition = null;
        }

        if (skillAttackController != null)
        {
            skillAttackController.Clear();
            skillAttackController = null;
        }

        if (combatStatHUD != null)
        {
            combatStatHUD.SetSkillCharge(0f, 0f);
        }
    }

    private void RefreshSkillCharge()
    {
        if (combatStatHUD == null)
        {
            return;
        }

        if (activeSkill != null)
        {
            combatStatHUD.SetSkillCharge(activeSkill.CooldownRemaining, activeSkill.CooldownTime);
        }
        else
        {
            combatStatHUD.SetSkillCharge(0f, 0f);
        }
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
        if (BlockState == HeroBlockState.Blocking)
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

    private void HandleNormalAttackFired(NormalAttackFiredData firedData)
    {
        Hurtbox target = firedData.Target;
        if (IsBlockingTarget(target))
        {
            FacePosition(target.AimPosition);
        }

        TryStartActionState(UnitRuntimeState.Attacking, normalAttackStateDuration);

        unitVisual.TriggerAttack();
    }

    private bool IsBlockingTarget(Hurtbox target)
    {
        if (!hasBlocker || heroBlocker == null || target == null || target.OwnerRuntime == null)
        {
            return false;
        }

        IReadOnlyList<IBlockable> blockedTargets = heroBlocker.BlockedTargets;
        for (int i = 0; i < blockedTargets.Count; i++)
        {
            IBlockable blockedTarget = blockedTargets[i];
            if (blockedTarget != null && blockedTarget.Owner == target.OwnerRuntime)
            {
                return true;
            }
        }

        return false;
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

        if (combatStatHUD == null)
        {
            combatStatHUD = GetComponentInChildren<CombatStatHUD>(true);
        }
    }
}
