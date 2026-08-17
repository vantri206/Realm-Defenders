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
    private CountdownTimer attackTimer;
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
        attackTimer = new CountdownTimer(0f);
    }

    public void Initialize(UnitStats stats, float effectMultiplier, AttackEffect attackEffect, TargetSide targetSide,
                        UnitAttackType attackType, AttackDamageType damageType, AttackMethod attackMethod,
                        AttackProjectile normalAttackProjectilePrefab, AttackAOEHit normalAttackAOEHitPrefab,
                        SimpleSpriteAnimatorVFX normalAttackHitVFXPrefab,
                        ParticleVFX normalAttackHealVFXPrefab,
                        TargetScanner targetScanner, TargetSelector targetSelector, UnitVisual unitVisual)
    {
        if (targetScanner == null)
        {
            Debug.LogError("[NormalAttackController] TargetScanner is required to initialize attacks.", this);
            return;
        }

        if (targetSelector == null)
        {
            Debug.LogError("[NormalAttackController] TargetSelector is required to initialize attacks.", this);
            return;
        }

        if (attackerTeam == null)
        {
            Debug.LogError("[NormalAttackController] TeamIdentity is required to initialize attacks.", this);
            return;
        }

        if (stats == null)
        {
            Debug.LogError("[NormalAttackController] UnitStats are required to initialize attacks.", this);
            return;
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

        isInitialized = true;
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

    public Hurtbox SelectTarget(IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (targetScanner == null || targetSelector == null || targetScanner.CombatGrid == null || patternOffsets == null)
        {
            Debug.LogError("[NormalAttackController] Cannot select target because required attack dependencies are missing.", this);
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

        if (target == null)
        {
            Debug.LogWarning("[NormalAttackController] No valid target selected for attack.", this);
            return false;
        }

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

        if (unitVisual == null)
        {
            Debug.LogError("[NormalAttackController] UnitVisual is required to trigger attack animation.", this);
        }
        else
        {
            unitVisual.TriggerAttack();
        }

        OnAttack?.Invoke(target);
        return true;
    }

    private bool ExecuteDirectTargetAttack(Hurtbox target)
    {
        if (target == null)
        {
            Debug.LogWarning("[NormalAttackController] Direct target is null, cannot execute attack.", this);
            return false;
        }

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
        if (target == null)
        {
            Debug.LogWarning("[NormalAttackController] Projectile target is null, cannot execute attack.", this);
            return false;
        }

        if (normalAttackProjectilePrefab == null)
        {
            Debug.LogError("[NormalAttackController] Normal attack projectile prefab is required for projectile attacks.", this);
            return false;
        }

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
            damageType
        ));

        return projectile != null;
    }

    private bool ExecuteAOEHitAttack(Hurtbox target)
    {
        if (target == null)
        {
            Debug.LogWarning("[NormalAttackController] AOE hit target is null, cannot execute attack.", this);
            return false;
        }

        if (normalAttackAOEHitPrefab == null)
        {
            Debug.LogError("[NormalAttackController] Normal attack AOE hit prefab is required for AOE hit attacks.", this);
            return false;
        }

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
            damageType
        ));

        return aoeHit != null;
    }

    private float CalculateBaseEffectValue()
    {
        if (stats == null)
        {
            return 0f;
        }

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
        if (normalAttackHealVFXPrefab == null || target == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnParticleVFX(normalAttackHealVFXPrefab, target);
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
