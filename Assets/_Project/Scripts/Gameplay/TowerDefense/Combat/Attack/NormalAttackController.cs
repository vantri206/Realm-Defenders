using System.Collections.Generic;
using System;
using UnityEngine;

public readonly struct NormalAttackFiredData
{
    public int AttackId { get; }
    public Hurtbox Target { get; }
    public int UniqueTargetCount { get; }
    public float AttackSnapshot { get; }

    public NormalAttackFiredData(int attackId, Hurtbox target, int uniqueTargetCount, float attackSnapshot)
    {
        AttackId = attackId;
        Target = target;
        UniqueTargetCount = uniqueTargetCount;
        AttackSnapshot = attackSnapshot;
    }
}

public class NormalAttackController : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;

    // References
    private readonly List<Hurtbox> validTargets = new List<Hurtbox>();
    private TargetScanner targetScanner;
    private TargetSelector targetSelector;
    private TeamIdentity attackerTeam;
    private CombatTimeController combatTime;
    private CountdownTimer attackTimer = new CountdownTimer(0f);
    private Hurtbox currentTarget;
    private int nextNormalAttackCount = 1;
    private bool skipNextAttackTimerTick;

    private UnitStats stats;
    private NormalAttackDefinition normalAttack;

    private bool isInitialized;

    public bool IsReadyAttack => isInitialized && attackTimer.IsFinished;
    public Vector3 AttackOrigin => GetAttackPosition();
    public bool HasCurrentTarget
    {
        get
        {
            if (currentTarget == null)
            {
                return false;
            }

            IDamageable damageable = currentTarget.GetDamageable();
            if (damageable == null || damageable.IsDead)
            {
                return false;
            }

            return (normalAttack.AttackEffect != AttackEffect.Heal || damageable.CurrentHealth < damageable.MaxHealth);
        }
    }

    public event Action<NormalAttackFiredData> OnNormalAttackFired;
    public event Action<int, HitData, HitResult> OnNormalAttackHitResolved;
    public event Action<int> OnNormalAttackFinished;

    private void Awake()
    {
        CacheReferences();
    }

    public bool Initialize(TeamIdentity team, UnitStats stats, NormalAttackDefinition normalAttackDefinition, TargetScanner targetScanner, TargetSelector targetSelector, CombatTimeController combatTime)
    {
        isInitialized = false;
        CacheReferences();

        if (!CheckRequiredReferences(team, stats, normalAttackDefinition, targetScanner, targetSelector, combatTime))
        {
            return false;
        }

        attackerTeam = team;
        this.stats = stats;
        this.normalAttack = normalAttackDefinition;

        this.targetScanner = targetScanner;
        this.targetSelector = targetSelector;
        this.combatTime = combatTime;

        nextNormalAttackCount = 1;
        skipNextAttackTimerTick = false;
        isInitialized = true;
        return true;
    }

    public void Tick(float deltaTime, IReadOnlyList<Vector2Int> patternOffsets, bool canTriggerAttack)
    {
        if (!isInitialized)
        {
            return;
        }

        if (attackTimer.IsRunning)
        {
            if (skipNextAttackTimerTick)
            {
                skipNextAttackTimerTick = false;
            }
            else
            {
                attackTimer.Tick(deltaTime);
            }
        }

        if (!canTriggerAttack || !IsReadyAttack)
        {
            return;
        }

        TriggerAttack(patternOffsets);
    }

    public void TriggerAttack(IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (!isInitialized)
        {
            return;
        }

        Hurtbox target = SelectTarget(patternOffsets);
        if (target == null)
        {
            currentTarget = null;
            return;
        }

        if (!ExecuteNormalAttack(target))
        {
            return;
        }

        StartAttackTimer(false);
    }

    private Hurtbox SelectTarget(IReadOnlyList<Vector2Int> patternOffsets)
    {
        validTargets.Clear();

        if (patternOffsets == null)
        {
            Debug.LogError("[NormalAttackController] Attack pattern is required to select a target.", this);
            return null;
        }

        if (normalAttack.AttackEffect == AttackEffect.Damage && normalAttack.TargetSide == TargetSide.Enemy &&
            targetSelector.TrySelectLockedBlockingTarget(normalAttack.AttackType, out Hurtbox lockedTarget))     //logic lock 1 enemy blocked
        {
            validTargets.Add(lockedTarget);
            return lockedTarget;
        }

        Vector2 attackPosition = GetAttackPosition();
        targetScanner.Scan(attackPosition, patternOffsets, normalAttack.TargetSide, normalAttack.AttackEffect, normalAttack.AttackType, validTargets);

        return targetSelector.SelectTarget(validTargets, attackPosition, normalAttack.TargetPriorityMode, normalAttack.AttackType);
    }

    private bool ExecuteNormalAttack(Hurtbox target)
    {
        currentTarget = target;
        int attackId = nextNormalAttackCount++;
        float attackSnapshot = stats.Attack;

        if (normalAttack.AttackMethod != AttackMethod.AOEHit)
        {
            OnNormalAttackFired?.Invoke(new NormalAttackFiredData(attackId, target, 1, attackSnapshot));
        }

        bool isAttackExecuted = false;

        switch (normalAttack.AttackMethod)
        {
            case AttackMethod.DirectTarget:
                isAttackExecuted = ExecuteDirectTargetAttack(target, attackId);
                break;

            case AttackMethod.Projectile:
                isAttackExecuted = ExecuteProjectileAttack(target, attackId);
                break;

            case AttackMethod.AOEHit:
                isAttackExecuted = ExecuteAOEHitAttack(target, attackId, attackSnapshot);
                break;

            default:
                Debug.LogError($"[NormalAttackController] Unsupported attack method: {normalAttack.AttackMethod}.", this);
                isAttackExecuted = false;
                break;
        }

        if (!isAttackExecuted)
        {
            OnNormalAttackFinished?.Invoke(attackId);
            return false;
        }

        return true;
    }

    private bool ExecuteDirectTargetAttack(Hurtbox target, int attackId)
    {
        float baseEffectValue = CalculateRawEffectValue();

        HitData hitData = new HitData(gameObject, target, attackerTeam, normalAttack.TargetSide,  normalAttack.AttackEffect, 
                                    normalAttack.AttackType, baseEffectValue, normalAttack.AttackDamageType, target.AimPosition);

        if (HitProcessor.TryProcessHit(hitData, out HitResult hitResult))
        {
            HandleNormalAttackHitResolved(attackId, hitData, hitResult);

            switch(normalAttack.AttackEffect)
            {
                case AttackEffect.Heal:
                    SpawnHealVFX(target, hitResult);
                    break;
                case AttackEffect.Damage:
                default:
                    SpawnHitVFX(hitData.HitPosition);
                    break;
            }
        }

        OnNormalAttackFinished?.Invoke(attackId);

        return true;
    }

    private bool ExecuteProjectileAttack(Hurtbox target, int attackId)
    {
        UnitRuntime targetRuntime = target.OwnerRuntime;

        if (targetRuntime == null || targetRuntime.IsDead)
        {
            Debug.LogWarning("[NormalAttackController] Projectile target does not resolve to a living UnitRuntime.", this);
            return false;
        }

        float rawEffectValue = CalculateRawEffectValue();

        AttackExecutionData executionData = new AttackExecutionData(gameObject, attackerTeam, normalAttack.TargetSide, normalAttack.AttackEffect, normalAttack.AttackType, rawEffectValue, normalAttack.AttackDamageType);

        AttackVFXData vfxData = normalAttack.AttackEffect == AttackEffect.Heal ? new AttackVFXData(normalAttack.NormalAttackHealVFXPrefab) : new AttackVFXData(normalAttack.NormalAttackHitVFXPrefab);

        AttackProjectile projectile = ObjectPoolingHelper.Spawn(normalAttack.NormalAttackProjectilePrefab, GetAttackPosition(), Quaternion.identity, spawnedProjectile =>
        spawnedProjectile.Initialize
        (
            executionData,
            target,
            vfxData,
            combatTime,
            (hitData, hitResult) => HandleNormalAttackHitResolved(attackId, hitData, hitResult),
            () => OnNormalAttackFinished?.Invoke(attackId)
        ));

        return projectile != null;
    }

    private bool ExecuteAOEHitAttack(Hurtbox target, int attackId, float attackSnapshot)
    {
        float rawEffectValue = CalculateRawEffectValue();

        AttackExecutionData executionData = new AttackExecutionData(gameObject, attackerTeam, normalAttack.TargetSide, normalAttack.AttackEffect, normalAttack.AttackType, rawEffectValue, normalAttack.AttackDamageType);

        AttackVFXData vfxData = normalAttack.AttackEffect == AttackEffect.Heal ? new AttackVFXData(normalAttack.NormalAttackHealVFXPrefab) :  new AttackVFXData(normalAttack.NormalAttackHitVFXPrefab);

        AttackAOEHit aoeHit = ObjectPoolingHelper.Spawn(normalAttack.NormalAttackAOEHitPrefab, target.AimPosition, Quaternion.identity, spawnedAOEHit =>
        spawnedAOEHit.Initialize
        (
            executionData,
            vfxData,
            combatTime,
            () => OnNormalAttackFinished?.Invoke(attackId),
            (hitData, hitResult) => HandleNormalAttackHitResolved(attackId, hitData, hitResult),
            uniqueTargetCount => OnNormalAttackFired?.Invoke(new NormalAttackFiredData(attackId, target, uniqueTargetCount, attackSnapshot)),
            true
        ));

        return aoeHit != null;
    }

    private float CalculateRawEffectValue()
    {
        return DamageCalculator.CalculateBaseEffectValue(stats.Attack, Mathf.Max(0f, normalAttack.NormalAttackEffectMultiplier));
    }

    public bool CanUseOverrideAttack(IReadOnlyList<Vector2Int> patternOffsets, out Hurtbox target)
    {
        target = IsReadyAttack ? SelectTarget(patternOffsets) : null;
        return target != null;
    }

    public bool TryUseOverrideAttack(IReadOnlyList<Vector2Int> patternOffsets, out Hurtbox target)
    {
        if (!CanUseOverrideAttack(patternOffsets, out target))
        {
            return false;
        }

        currentTarget = target;
        StartAttackTimer(true);
        return true;
    }

    public bool TryUseOverrideAttack(Hurtbox target)
    {
        if (!IsReadyAttack || target == null || target.OwnerRuntime == null || target.OwnerRuntime.IsDead)
        {
            return false;
        }

        currentTarget = target;
        StartAttackTimer(true);
        return true;
    }

    private void StartAttackTimer(bool isExternalConsumption)
    {
        attackTimer.Reset(stats.AttackInterval);
        attackTimer.StartTimer();
        skipNextAttackTimerTick = isExternalConsumption;
    }

    private void HandleNormalAttackHitResolved(int attackId, HitData hitData, HitResult hitResult)
    {
        OnNormalAttackHitResolved?.Invoke(attackId, hitData, hitResult);
    }

    private Vector2 GetAttackPosition()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
    }

    private void SpawnHitVFX(Vector3 hitPosition)
    {
        if (normalAttack.NormalAttackHitVFXPrefab == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnSimpleSpriteVFX(normalAttack.NormalAttackHitVFXPrefab, hitPosition);
    }

    private void SpawnHealVFX(Hurtbox target, in HitResult hitResult)
    {
        if (normalAttack.NormalAttackHealVFXPrefab == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnParticleVFX(normalAttack.NormalAttackHealVFXPrefab, target);
    }

    private bool CheckRequiredReferences(TeamIdentity team, UnitStats stats, NormalAttackDefinition definition, TargetScanner scanner, TargetSelector selector, CombatTimeController combatTime)
    {
        if (team == null || stats == null || definition == null || scanner == null || !scanner.IsInitialized || selector == null || !selector.IsInitialized || combatTime == null)
        {
            Debug.LogError("[NormalAttackController] Missing required references.", this);
            return false;
        }

        if (definition.AttackMethod == AttackMethod.Projectile && definition.NormalAttackProjectilePrefab == null)
        {
            Debug.LogError("[NormalAttackController] Projectile attacks require an AttackProjectile prefab.", this);
            return false;
        }

        if (definition.AttackMethod == AttackMethod.AOEHit && definition.NormalAttackAOEHitPrefab == null)
        {
            Debug.LogError("[NormalAttackController] AOE attacks require an AttackAOEHit prefab.", this);
            return false;
        }

        return true;
    }

    private void CacheReferences()
    {
        if (targetScanner == null)
        {
            targetScanner = GetComponent<TargetScanner>();
        }

        if (targetSelector == null)
        {
            targetSelector = GetComponent<TargetSelector>();
        }

        if (attackerTeam == null)
        {
            attackerTeam = GetComponentInParent<TeamIdentity>();
        }
    }
}
