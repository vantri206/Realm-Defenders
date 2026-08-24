using System.Collections.Generic;
using System;
using UnityEngine;

public struct AttackExecutionData
{
    public GameObject Attacker { get; }
    public TeamIdentity AttackerTeam { get; }
    public TargetSide TargetSide { get; }
    public AttackEffect AttackEffect { get; }
    public UnitAttackType AttackType { get; }
    public float RawEffectValue { get; }
    public AttackDamageType DamageType { get; }

    public AttackExecutionData(GameObject attacker, TeamIdentity attackerTeam, TargetSide targetSide, AttackEffect attackEffect, UnitAttackType attackType, float rawEffectValue, AttackDamageType damageType)
    {
        Attacker = attacker;
        AttackerTeam = attackerTeam;
        TargetSide = targetSide;
        AttackEffect = attackEffect;
        AttackType = attackType;
        RawEffectValue = rawEffectValue;
        DamageType = damageType;
    }
}

public struct AttackVFXData
{
    public SimpleSpriteAnimatorVFX HitVFX { get; }
    public ParticleVFX HealVFX { get; }

    public AttackVFXData(SimpleSpriteAnimatorVFX hitVFX, ParticleVFX healVFX)
    {
        HitVFX = hitVFX;
        HealVFX = healVFX;
    }

    public AttackVFXData(SimpleSpriteAnimatorVFX hitVFX)
    {
        HitVFX = hitVFX;
        HealVFX = null;
    }

    public AttackVFXData(ParticleVFX healVFX)
    {
        HitVFX = null;
        HealVFX = healVFX;
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

    private UnitStats stats;
    private NormalAttackDefinition normalAttack;

    private bool isInitialized;

    public bool IsReadyAttack => isInitialized && attackTimer.IsFinished;
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

    public event Action<Hurtbox> OnAttack;

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
            attackTimer.Tick(deltaTime);
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

        attackTimer.Reset(stats.AttackInterval);
        attackTimer.StartTimer();
    }

    private Hurtbox SelectTarget(IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (patternOffsets == null)
        {
            Debug.LogError("[NormalAttackController] Attack pattern is required to select a target.", this);
            return null;
        }

        if (normalAttack.AttackEffect == AttackEffect.Damage && normalAttack.TargetSide == TargetSide.Enemy &&
            targetSelector.TrySelectLockedBlockingTarget(normalAttack.AttackType, out Hurtbox lockedTarget))     //logic lock 1 enemy blocked
        {
            return lockedTarget;
        }

        Vector2 attackPosition = GetAttackPosition();
        targetScanner.Scan(attackPosition, patternOffsets, normalAttack.TargetSide, normalAttack.AttackEffect, normalAttack.AttackType, validTargets);

        return targetSelector.SelectTarget(validTargets, attackPosition, normalAttack.TargetPriorityMode, normalAttack.AttackType);
    }

    private bool ExecuteNormalAttack(Hurtbox target)
    {
        currentTarget = target;

        bool isAttackExecuted = false;

        switch (normalAttack.AttackMethod)
        {
            case AttackMethod.DirectTarget:
                isAttackExecuted = ExecuteDirectTargetAttack(target);
                break;

            case AttackMethod.Projectile:
                isAttackExecuted = ExecuteProjectileAttack(target);
                break;

            case AttackMethod.AOEHit:
                isAttackExecuted = ExecuteAOEHitAttack(target);
                break;

            default:
                Debug.LogError($"[NormalAttackController] Unsupported attack method: {normalAttack.AttackMethod}.", this);
                isAttackExecuted = false;
                break;
        }

        if (!isAttackExecuted)
        {
            return false;
        }

        OnAttack?.Invoke(target);
        return true;
    }

    private bool ExecuteDirectTargetAttack(Hurtbox target)
    {
        float baseEffectValue = CalculateRawEffectValue();

        HitData hitData = new HitData(gameObject, target, attackerTeam, normalAttack.TargetSide,  normalAttack.AttackEffect, 
                                    normalAttack.AttackType, baseEffectValue, normalAttack.AttackDamageType, target.AimPosition);

        if (HitProcessor.TryProcessHit(hitData, out HitResult hitResult))
        {
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

        return true;
    }

    private bool ExecuteProjectileAttack(Hurtbox target)
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
            combatTime
        ));

        return projectile != null;
    }

    private bool ExecuteAOEHitAttack(Hurtbox target)
    {
        float rawEffectValue = CalculateRawEffectValue();

        AttackExecutionData executionData = new AttackExecutionData(gameObject, attackerTeam, normalAttack.TargetSide, normalAttack.AttackEffect, normalAttack.AttackType, rawEffectValue, normalAttack.AttackDamageType);

        AttackVFXData vfxData = normalAttack.AttackEffect == AttackEffect.Heal ? new AttackVFXData(normalAttack.NormalAttackHealVFXPrefab) :  new AttackVFXData(normalAttack.NormalAttackHitVFXPrefab);

        AttackAOEHit aoeHit = ObjectPoolingHelper.Spawn(normalAttack.NormalAttackAOEHitPrefab, target.AimPosition, Quaternion.identity, spawnedAOEHit =>
        spawnedAOEHit.Initialize
        (
            executionData,
            vfxData,
            combatTime
        ));

        return aoeHit != null;
    }

    private float CalculateRawEffectValue()
    {
        return DamageCalculator.CalculateBaseEffectValue(stats.Attack, Mathf.Max(0f, normalAttack.NormalAttackEffectMultiplier));
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
