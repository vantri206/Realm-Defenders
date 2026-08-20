using System.Collections.Generic;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class NormalAttackController : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;

    // References
    private readonly List<Hurtbox> validTargets = new List<Hurtbox>();
    private TargetScanner targetScanner;
    private TargetSelector targetSelector;
    private TeamIdentity attackerTeam;
    private UnitVisual unitVisual;
    private CombatTimeController combatTime;
    private CountdownTimer attackTimer = new CountdownTimer(0f);
    private Hurtbox currentTarget;

    // Attack properties
    private UnitStats stats;
    private float attackInterval;
    private float effectMultiplier;
    private AttackEffect attackEffect;
    private TargetSide targetSide;
    private UnitAttackType attackType;
    private AttackDamageType damageType;
    private AttackMethod attackMethod;
    private AttackProjectile normalAttackProjectilePrefab;
    private AttackAOEHit normalAttackAOEHitPrefab;
    private SimpleSpriteAnimatorVFX normalAttackHitVFXPrefab;
    private ParticleVFX normalAttackHealVFXPrefab;

    private bool isInitialized;

    public bool IsReadyAttack => isInitialized && attackTimer.IsFinished;

    public event Action<Hurtbox> OnAttack;

    private void Awake()
    {
        CacheReferences();
    }

    public bool Initialize(UnitStats stats, float effectMultiplier, AttackEffect attackEffect, TargetSide targetSide,
                        UnitAttackType attackType, AttackDamageType damageType, AttackMethod attackMethod,
                        AttackProjectile normalAttackProjectilePrefab, AttackAOEHit normalAttackAOEHitPrefab,
                        SimpleSpriteAnimatorVFX normalAttackHitVFXPrefab, ParticleVFX normalAttackHealVFXPrefab,
                        TargetScanner targetScanner, TargetSelector targetSelector, UnitVisual unitVisual,
                        CombatTimeController combatTime)
    {
        isInitialized = false;
        CacheReferences();

        if (!CheckRequiredReferences(stats, attackMethod, normalAttackProjectilePrefab, normalAttackAOEHitPrefab,
                                     targetScanner, targetSelector, unitVisual, combatTime))
        {
            return false;
        }

        this.stats = stats;
        attackInterval = Mathf.Max(0f, stats.AttackInterval);
        this.effectMultiplier = Mathf.Max(0f, effectMultiplier);
        this.attackEffect = attackEffect;
        this.targetSide = targetSide;
        this.attackType = attackType;
        this.damageType = damageType;
        this.attackMethod = attackMethod;
        this.normalAttackProjectilePrefab = normalAttackProjectilePrefab;
        this.normalAttackAOEHitPrefab = normalAttackAOEHitPrefab;
        this.normalAttackHitVFXPrefab = normalAttackHitVFXPrefab;
        this.normalAttackHealVFXPrefab = normalAttackHealVFXPrefab;

        this.targetScanner = targetScanner;
        this.targetSelector = targetSelector;
        this.unitVisual = unitVisual;
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

        attackTimer.Reset(attackInterval);
        attackTimer.StartTimer();
    }

    private Hurtbox SelectTarget(IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (patternOffsets == null)
        {
            Debug.LogError("[NormalAttackController] Attack pattern is required to select a target.", this);
            return null;
        }

        if (attackEffect == AttackEffect.Damage && targetSide == TargetSide.Enemy &&
            targetSelector.TrySelectLockedBlockingTarget(out Hurtbox lockedTarget))
        {
            return lockedTarget;
        }

        Vector2 attackPosition = GetAttackPosition();
        targetScanner.Scan(attackPosition, patternOffsets, targetSide, attackEffect, validTargets);

        return targetSelector.SelectTarget(validTargets, attackPosition);
    }

    private bool ExecuteNormalAttack(Hurtbox target)
    {
        currentTarget = target;

        bool isAttackExecuted = false;

        switch (attackMethod)
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
                Debug.LogError($"[NormalAttackController] Unsupported attack method: {attackMethod}.", this);
                isAttackExecuted = false;
                break;
        }

        if (!isAttackExecuted)
        {
            return false;
        }

        OnAttack?.Invoke(target);
        unitVisual.TriggerAttack();
        return true;
    }

    private bool ExecuteDirectTargetAttack(Hurtbox target)
    {
        float baseEffectValue = CalculateBaseEffectValue();

        HitData hitData = new HitData(gameObject, target, attackerTeam, targetSide, attackEffect, attackType, 
                                    baseEffectValue, damageType, target.CenterPosition);

        if (HitProcessor.TryProcessHit(hitData, out HitResult hitResult))
        {
            switch(attackEffect)
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

        float baseEffectValue = CalculateBaseEffectValue();

        AttackProjectile projectile = ObjectPoolingHelper.Spawn(normalAttackProjectilePrefab, GetAttackPosition(), Quaternion.identity, spawnedProjectile =>
        spawnedProjectile.Initialize
        (
            gameObject,
            attackerTeam,
            targetRuntime,
            targetSide,
            attackEffect,
            attackType,
            normalAttackHealVFXPrefab,
            baseEffectValue,
            damageType,
            combatTime
        ));

        return projectile != null;
    }

    private bool ExecuteAOEHitAttack(Hurtbox target)
    {
        float baseEffectValue = CalculateBaseEffectValue();

        AttackAOEHit aoeHit = ObjectPoolingHelper.Spawn(normalAttackAOEHitPrefab, target.CenterPosition, Quaternion.identity, spawnedAOEHit =>
        spawnedAOEHit.Initialize
        (
            gameObject,
            attackerTeam,
            targetSide,
            attackEffect,
            attackType,
            normalAttackHealVFXPrefab,
            baseEffectValue,
            damageType,
            combatTime
        ));

        return aoeHit != null;
    }

    private float CalculateBaseEffectValue()
    {
        return DamageCalculator.CalculateBaseEffectValue(stats.Attack, effectMultiplier);
    }

    private Vector2 GetAttackPosition()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
    }

    private void SpawnHitVFX(Vector3 hitPosition)
    {
        if (normalAttackHitVFXPrefab == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnSimpleSpriteVFX(normalAttackHitVFXPrefab, hitPosition);
    }

    private void SpawnHealVFX(Hurtbox target, in HitResult hitResult)
    {
        if (normalAttackHealVFXPrefab == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnParticleVFX(normalAttackHealVFXPrefab, target);
    }

    private bool CheckRequiredReferences(UnitStats stats, AttackMethod method,
                                        AttackProjectile projectilePrefab, AttackAOEHit aoeHitPrefab,
                                        TargetScanner scanner, TargetSelector selector, UnitVisual visual,
                                        CombatTimeController combatTime)
    {
        if (stats == null || scanner == null || !scanner.IsInitialized || selector == null ||
            !selector.IsInitialized || visual == null || attackerTeam == null || combatTime == null)
        {
            Debug.LogError("[NormalAttackController] Missing required references.", this);
            return false;
        }

        if (method == AttackMethod.Projectile && projectilePrefab == null)
        {
            Debug.LogError("[NormalAttackController] Projectile attacks require an AttackProjectile prefab.", this);
            return false;
        }

        if (method == AttackMethod.AOEHit && aoeHitPrefab == null)
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
            attackerTeam = GetComponent<TeamIdentity>();
        }

        if (unitVisual == null)
        {
            unitVisual = GetComponentInChildren<UnitVisual>();
        }
    }
}
