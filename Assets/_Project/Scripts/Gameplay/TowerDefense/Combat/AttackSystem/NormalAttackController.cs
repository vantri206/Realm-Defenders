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
    private UnitVisual unitVisual;
    private CountdownTimer attackTimer;
    private Hurtbox currentTarget;

    // Attack properties
    private float attack;
    private float attackInterval;
    private float damageMultiplier;
    private AttackDamageType damageType;
    private AttackMethod attackMethod;

    private bool isInitialized;

    public Hurtbox CurrentTarget => currentTarget;
    public bool IsReadyAttack => isInitialized && attackTimer.IsFinished;

    public event Action<Hurtbox> OnAttack;

    private void Awake()
    {
        CacheReferences();
        attackTimer = new CountdownTimer(0f);
    }

    public void Initialize(float attack, float attackInterval, float damageMultiplier, AttackDamageType damageType,
                           AttackMethod attackMethod, TargetScanner targetScanner, TargetSelector targetSelector, UnitVisual unitVisual)
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

        this.attack = Mathf.Max(0f, attack);
        this.attackInterval = Mathf.Max(0f, attackInterval);
        this.damageMultiplier = Mathf.Max(0f, damageMultiplier);
        this.damageType = damageType;
        this.attackMethod = attackMethod;

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

        ExecuteNormalAttack(target);
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

        if (targetSelector.TrySelectLockedBlockingTarget(out Hurtbox lockedTarget))
        {
            return lockedTarget;
        }

        Vector2 attackPosition = GetAttackPosition();
        targetScanner.Scan(attackPosition, patternOffsets, validTargets);

        return targetSelector.SelectTarget(validTargets, attackPosition);
    }

    private bool ExecuteNormalAttack(Hurtbox target)
    {
        currentTarget = target;

        if (unitVisual == null)
        {
            Debug.LogError("[NormalAttackController] UnitVisual is required to trigger attack animation.", this);
        }
        else
        {
            unitVisual.TriggerAttack();
        }

        bool isAtackExecuted = false;

        if (target == null)
        {
            Debug.LogWarning("[NormalAttackController] No valid target selected for attack.", this);
            return false;
        }

        switch (attackMethod)
        {
            case AttackMethod.DirectTarget:
                isAtackExecuted = ExecuteDirectTargetAttack(target);
                break;

            case AttackMethod.Projectile:
                isAtackExecuted = ExecuteProjectileAttack(target);
                break;

            case AttackMethod.Hitbox:
                isAtackExecuted = ExecuteHitboxAttack(target);
                break;

            default:
                Debug.LogError($"[NormalAttackController] Unsupported attack method: {attackMethod}.", this);
                isAtackExecuted = false;
                break;
        }

        if (isAtackExecuted)
        {
            OnAttack?.Invoke(target);
        }

        return isAtackExecuted;
    }

    private bool ExecuteDirectTargetAttack(Hurtbox target)
    {
        if (target == null)
        {
            Debug.LogWarning("[NormalAttackController] Direct target is null, cannot execute attack.", this);
            return false;
        }

        IDamageable damageable = target.GetDamageable();
        if (damageable == null || damageable.IsDead)
        {
            Debug.LogWarning("[NormalAttackController] Direct target does not resolve to a living IDamageable.", this);
            return false;
        }

        float baseDamage = DamageCalculator.CalculateBaseDamage(attack, damageMultiplier);
        if (baseDamage <= 0f)
        {
            return false;
        }

        DamageRequest request = new DamageRequest(gameObject, damageable, baseDamage, damageType, target.CenterPosition);
        DamageSystem.ApplyDamage(request);
        return true;
    }

    private bool ExecuteProjectileAttack(Hurtbox target)
    {
        if (target == null)
        {
            Debug.LogWarning("[NormalAttackController] Projectile target is null, cannot execute attack.", this);
            return false;
        }

        Debug.LogWarning("[NormalAttackController] Projectile attacks are not implemented yet.", this);
        return false;
    }

    private bool ExecuteHitboxAttack(Hurtbox target)
    {
        if (target == null)
        {
            Debug.LogWarning("[NormalAttackController] Hitbox target is null, cannot execute attack.", this);
            return false;
        }

        Debug.LogWarning("[NormalAttackController] Hitbox attacks are not implemented yet.", this);
        return false;
    }

    private Vector2 GetAttackPosition()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
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

        if (unitVisual == null)
        {
            unitVisual = GetComponentInChildren<UnitVisual>();
        }
    }
}
