using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class NormalAttackFiredData
{
    public int AttackId { get; }
    public Hurtbox Target { get; }
    public int UniqueTargetCount { get; private set; }
    public float AttackSnapshot { get; }
    public float RawEffectValue { get; set; }
    public AttackMethod AttackMethod { get; set; }

    public NormalAttackFiredData(int attackId, Hurtbox target, int uniqueTargetCount, float attackSnapshot,
                                 float rawEffectValue, AttackMethod attackMethod)
    {
        AttackId = attackId;
        Target = target;
        UniqueTargetCount = uniqueTargetCount;
        AttackSnapshot = attackSnapshot;
        RawEffectValue = rawEffectValue;
        AttackMethod = attackMethod;
    }

    public void SetUniqueTargetCount(int value)
    {
        UniqueTargetCount = Mathf.Max(0, value);
    }
}

public class NormalAttackController : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;

    // References
    private readonly List<Hurtbox> validTargets = new List<Hurtbox>();
    private readonly List<Hurtbox> selectedTargets = new List<Hurtbox>();
    private TargetScanner targetScanner;
    private TargetSelector targetSelector;
    private TeamIdentity attackerTeam;
    private CombatTimeController combatTime;
    private CountdownTimer attackTimer = new CountdownTimer(0f);
    private Hurtbox currentTarget;
    private int nextNormalAttackCount = 1;
    private bool skipAttackerTimer;

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
        skipAttackerTimer = false;
        isInitialized = true;
        return true;
    }

    public void TickAttackTimer(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        if (attackTimer.IsRunning)
        {
            if (skipAttackerTimer)
            {
                skipAttackerTimer = false;
            }
            else
            {
                attackTimer.Tick(deltaTime);
            }
        }
    }

    public void TryTriggerAttack(IReadOnlyList<Vector2Int> patternOffsets, bool canTriggerAttack)
    {
        if (!isInitialized)
        {
            return;
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
        SelectTargets
        (
            patternOffsets,
            normalAttack.TargetSide,
            normalAttack.AttackEffect,
            1,
            selectedTargets
        );

        return selectedTargets.Count > 0 ? selectedTargets[0] : null;
    }

    private void SelectTargets(IReadOnlyList<Vector2Int> patternOffsets, TargetSide targetSide, AttackEffect attackEffect,
                               int maxTargetCount, List<Hurtbox> results)
    {
        results.Clear();
        validTargets.Clear();

        if (patternOffsets == null)
        {
            Debug.LogError("[NormalAttackController] Attack pattern is required to select a target.", this);
            return;
        }

        if (maxTargetCount <= 0)
        {
            return;
        }

        if (attackEffect == AttackEffect.Damage && targetSide == TargetSide.Enemy &&
            targetSelector.TrySelectLockedBlockingTarget(normalAttack.AttackType, out Hurtbox lockedTarget))     //logic lock 1 enemy blocked
        {
            results.Add(lockedTarget);
            if (results.Count >= maxTargetCount)
            {
                return;
            }
        }

        Vector2 attackPosition = GetAttackPosition();
        targetScanner.Scan(attackPosition, patternOffsets, targetSide, attackEffect, normalAttack.AttackType, validTargets);

        if (results.Count > 0)
        {
            validTargets.Remove(results[0]);
        }

        while (results.Count < maxTargetCount && validTargets.Count > 0)
        {
            Hurtbox target = targetSelector.SelectTarget(validTargets, attackPosition, normalAttack.TargetPriorityMode, normalAttack.AttackType);
            if (target == null)
            {
                return;
            }

            results.Add(target);
            validTargets.Remove(target);
        }
    }

    private bool ExecuteNormalAttack(Hurtbox target)
    {
        currentTarget = target;
        int attackId = nextNormalAttackCount++;
        float attackSnapshot = stats.Attack;
        float rawEffectValue = CalculateRawEffectValue();
        NormalAttackFiredData firedData = new NormalAttackFiredData
        (
            attackId,
            target,
            normalAttack.AttackMethod == AttackMethod.AOEHit ? 0 : 1,
            attackSnapshot,
            rawEffectValue,
            normalAttack.AttackMethod
        );

        if (normalAttack.AttackMethod != AttackMethod.AOEHit)
        {
            OnNormalAttackFired?.Invoke(firedData);
        }

        bool isAttackExecuted = false;

        switch (firedData.AttackMethod)
        {
            case AttackMethod.DirectTarget:
                isAttackExecuted = ExecuteDirectTargetAttack(target, attackId, firedData.RawEffectValue);
                break;

            case AttackMethod.Projectile:
                isAttackExecuted = ExecuteProjectileAttack(target, attackId, firedData.RawEffectValue);
                break;

            case AttackMethod.AOEHit:
                isAttackExecuted = ExecuteAOEHitAttack(target, firedData);
                break;

            default:
                Debug.LogError($"[NormalAttackController] Unsupported attack method: {firedData.AttackMethod}.", this);
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

    private bool ExecuteDirectTargetAttack(Hurtbox target, int attackId, float rawEffectValue)
    {
        HitData hitData = new HitData(gameObject, target, attackerTeam, normalAttack.TargetSide,  normalAttack.AttackEffect, 
                                    normalAttack.AttackType, Mathf.Max(0f, rawEffectValue), normalAttack.AttackDamageType, target.AimPosition);

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
                    SpawnHitVFX(target);
                    break;
            }
        }

        OnNormalAttackFinished?.Invoke(attackId);

        return true;
    }

    private bool ExecuteProjectileAttack(Hurtbox target, int attackId, float rawEffectValue)
    {
        UnitRuntime targetRuntime = target.OwnerRuntime;

        if (targetRuntime == null || targetRuntime.IsDead)
        {
            Debug.LogWarning("[NormalAttackController] Projectile target does not resolve to a living UnitRuntime.", this);
            return false;
        }

        AttackExecutionData executionData = new AttackExecutionData(gameObject, attackerTeam, normalAttack.TargetSide, normalAttack.AttackEffect,
                                                                    normalAttack.AttackType, Mathf.Max(0f, rawEffectValue), normalAttack.AttackDamageType);

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

    private bool ExecuteAOEHitAttack(Hurtbox target, NormalAttackFiredData firedData)
    {
        AttackExecutionData executionData = new AttackExecutionData(gameObject, attackerTeam, normalAttack.TargetSide, normalAttack.AttackEffect,
                                                                    normalAttack.AttackType, firedData.RawEffectValue, normalAttack.AttackDamageType);

        AttackVFXData vfxData = normalAttack.AttackEffect == AttackEffect.Heal ? new AttackVFXData(normalAttack.NormalAttackHealVFXPrefab) :  new AttackVFXData(normalAttack.NormalAttackHitVFXPrefab);

        AttackAOEHit aoeHit = ObjectPoolingHelper.Spawn(normalAttack.NormalAttackAOEHitPrefab, target.AimPosition, Quaternion.identity, spawnedAOEHit =>
        spawnedAOEHit.Initialize
        (
            executionData,
            vfxData,
            combatTime,
            () => OnNormalAttackFinished?.Invoke(firedData.AttackId),
            (hitData, hitResult) => HandleNormalAttackHitResolved(firedData.AttackId, hitData, hitResult),
            uniqueTargetCount =>
            {
                firedData.SetUniqueTargetCount(uniqueTargetCount);
                OnNormalAttackFired?.Invoke(firedData);
                return firedData.RawEffectValue;
            }
        ));

        return aoeHit != null;
    }

    private float CalculateRawEffectValue()
    {
        return DamageCalculator.CalculateBaseEffectValue(stats.Attack, Mathf.Max(0f, normalAttack.NormalAttackEffectMultiplier));
    }

    public bool TrySelectTarget(IReadOnlyList<Vector2Int> patternOffsets, TargetSide targetSide, AttackEffect attackEffect, out Hurtbox target)
    {
        target = null;
        if (!isInitialized)
        {
            return false;
        }

        SelectTargets(patternOffsets, targetSide, attackEffect, 1, selectedTargets);
        target = selectedTargets.Count > 0 ? selectedTargets[0] : null;
        return target != null;
    }

    public bool TrySetupOverrideAttack(IReadOnlyList<Vector2Int> patternOffsets, TargetSide targetSide, AttackEffect attackEffect,
                                       int maxTargetCount, List<Hurtbox> targets)
    {
        if (!IsReadyAttack || targets == null)
        {
            return false;
        }

        SelectTargets(patternOffsets, targetSide, attackEffect, maxTargetCount, targets);
        if (targets.Count == 0)
        {
            return false;
        }

        currentTarget = targets[0];
        return true;
    }

    public bool TryConsumeOverrideAttack()
    {
        if (!isInitialized || !IsReadyAttack)
        {
            return false;
        }

        StartAttackTimer(false);
        return true;
    }

    public void StopNormalAttack()
    {
        if (!isInitialized)
        {
            return;
        }

        attackTimer.Pause();
        skipAttackerTimer = false;
    }

    public void ResumeNormalAttack()
    {
        if (!isInitialized)
        {
            return;
        }

        StartAttackTimer(true);
    }

    private void StartAttackTimer(bool skipFirstTick)
    {
        attackTimer.Reset(stats.AttackInterval);
        attackTimer.StartTimer();
        skipAttackerTimer = skipFirstTick;
    }

    private void HandleNormalAttackHitResolved(int attackId, HitData hitData, HitResult hitResult)
    {
        OnNormalAttackHitResolved?.Invoke(attackId, hitData, hitResult);
    }

    private Vector2 GetAttackPosition()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
    }

    private void SpawnHitVFX(Hurtbox target)
    {
        if (normalAttack.NormalAttackHitVFXPrefab == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnSimpleSpriteVFX(normalAttack.NormalAttackHitVFXPrefab, target, combatTime);
    }

    private void SpawnHealVFX(Hurtbox target, in HitResult hitResult)
    {
        if (normalAttack.NormalAttackHealVFXPrefab == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnParticleVFX(normalAttack.NormalAttackHealVFXPrefab, target, combatTime);
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
