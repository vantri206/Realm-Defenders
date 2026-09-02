using System;
using System.Collections.Generic;
using UnityEngine;

public enum ProjectileHitMode
{
    Single,
    Piercing
}

public class AttackProjectile : MonoBehaviour, IPoolable
{
    public int PrefabID { get; set; }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;

    [Header("VFX")]
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;
    [SerializeField] private float hitVFXRotationOffset = 180f;
    [Header("Heal VFX")]
    [SerializeField] private ParticleVFX healVFXPrefab;

    [Header("Movement")]
    [SerializeField] private ProjectileMode projectileMode = ProjectileMode.Linear;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float maxRange = 20f;
    [SerializeField, Range(0f, 180f)] private float maxTurnAngle = 90f;

    private GameObject attacker;
    private TeamIdentity attackerTeam;
    private Hurtbox target;
    private TargetSide targetSide;
    private AttackEffect attackEffect;
    private UnitAttackType attackType;
    private float rawEffectValue;
    private AttackDamageType damageType;
    private AttackVFXData attackVFXData;
    private CombatTimeController combatTime;
    
    private Action<HitData, HitResult> onHitResolved;
    private Action onAttackFinished;

    private ProjectileMode currentMode;
    private Vector2 spawnPosition;
    private Vector2 moveDirection;
    private CountdownTimer lifetimeTimer;

    private readonly HashSet<Hurtbox> hitTargets = new HashSet<Hurtbox>();
    private ProjectileHitMode hitMode;
    private int maxPiercingTargets;
    private bool hasHitTarget;

    private bool isInitialized;
    private bool isReturningToPool;

    private void Awake()
    {
        CacheReferences();
    }

    private void FixedUpdate()
    {
        if (!isInitialized || hasHitTarget || isReturningToPool)
        {
            return;
        }

        float combatFixedDeltaTime = combatTime.CombatFixedDeltaTime;
        lifetimeTimer.Tick(combatFixedDeltaTime);
        if (lifetimeTimer.IsFinished || CheckReachMaxRange())
        {
            ReturnToPool();
            return;
        }

        UpdateMoveDirection();
        Move(combatFixedDeltaTime);
    }

    public void Initialize(AttackExecutionData executionData, Hurtbox target, AttackVFXData vfxData, CombatTimeController combatTime,
                           Action<HitData, HitResult> onHitResolved = null, Action onAttackFinished = null,
                           ProjectileHitMode hitMode = ProjectileHitMode.Single, int maxPiercingTargets = 1)
    {
        this.attacker = executionData.Attacker;
        this.attackerTeam = executionData.AttackerTeam;
        this.target = target;
        this.targetSide = executionData.TargetSide;
        this.attackEffect = executionData.AttackEffect;
        this.attackType = executionData.AttackType;
        this.rawEffectValue = Mathf.Max(0f, executionData.RawEffectValue);
        this.damageType = executionData.DamageType;
        this.attackVFXData = vfxData;
        this.combatTime = combatTime;
        this.onHitResolved = onHitResolved;
        this.onAttackFinished = onAttackFinished;
        this.hitMode = hitMode;
        this.maxPiercingTargets = Mathf.Max(1, maxPiercingTargets);

        isInitialized = true;
    }

    public void OnSpawn()
    {
        isReturningToPool = false;
        hasHitTarget = false;
        hitTargets.Clear();
        currentMode = hitMode == ProjectileHitMode.Piercing ? ProjectileMode.Linear : projectileMode;

        CacheReferences();

        if (!CanStart())
        {
            Debug.LogError("[AttackProjectile] Projectile missing data are required before spawning.", this);
            ReturnToPool();
            return;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        spawnPosition = rb.position;
        moveDirection = GetDirectionToTarget();

        if (moveDirection == Vector2.zero)
        {
            ReturnToPool();
            return;
        }

        if (currentMode == ProjectileMode.Linear)
        {
            target = null;
        }

        lifetimeTimer = new CountdownTimer(lifetime);
        lifetimeTimer.StartTimer();
        UpdateRotation();
    }

    public void OnDespawn()
    {
        if (lifetimeTimer != null)
        {
            lifetimeTimer.StopTimer();
            lifetimeTimer = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        attacker = null;
        attackerTeam = null;
        target = null;
        targetSide = default;
        attackEffect = default;
        attackType = default;
        rawEffectValue = 0f;
        damageType = default;
        combatTime = null;
        onHitResolved = null;
        onAttackFinished = null;

        currentMode = projectileMode;
        spawnPosition = Vector2.zero;
        moveDirection = Vector2.zero;
        hitTargets.Clear();
        hitMode = ProjectileHitMode.Single;
        maxPiercingTargets = 1;

        isInitialized = false;
        hasHitTarget = false;
        isReturningToPool = true;
    }

    public void ReturnToPool()
    {
        if (isReturningToPool)
        {
            return;
        }

        isReturningToPool = true;
        onAttackFinished?.Invoke();
        onAttackFinished = null;
        ObjectPoolingHelper.Release(this);
    }

    private void UpdateMoveDirection()
    {
        if (currentMode != ProjectileMode.Chase)
        {
            return;
        }

        if (target == null || target.OwnerRuntime == null || target.OwnerRuntime.IsDead)
        {
            ChangeToLinearMode();
            return;
        }

        Vector2 desiredDirection = GetDirectionToTarget();
        if (desiredDirection == Vector2.zero)
        {
            return;
        }

        float turnAngle = Vector2.Angle(moveDirection, desiredDirection);
        if (turnAngle > maxTurnAngle)
        {
            ChangeToLinearMode();
            return;
        }

        moveDirection = desiredDirection;
    }

    private void ChangeToLinearMode()
    {
        currentMode = ProjectileMode.Linear;
        target = null;
    }

    private void Move(float deltaTime)
    {
        Vector2 movement = moveDirection * moveSpeed * deltaTime;
        rb.MovePosition(rb.position + movement);
        UpdateRotation();
    }

    private bool CheckReachMaxRange()
    {
        return Vector2.Distance(spawnPosition, rb.position) >= maxRange;
    }

    private Vector2 GetDirectionToTarget()
    {
        if (target == null)
        {
            return Vector2.zero;
        }

        Vector2 direction = (Vector2)target.AimPosition - rb.position;
        return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.zero;
    }

    private void UpdateRotation()
    {
        if (moveDirection == Vector2.zero)
        {
            return;
        }

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        rb.SetRotation(angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || hasHitTarget || isReturningToPool)
        {
            return;
        }

        if (!other.TryGetComponent(out Hurtbox hurtbox) || hitTargets.Contains(hurtbox))
        {
            return;
        }

        Vector3 hitPosition = GetHitPosition(other, hurtbox);
        HitData hitData = new HitData(attacker, hurtbox, attackerTeam, targetSide, attackEffect, attackType, rawEffectValue, damageType, hitPosition);
        if (!HitProcessor.TryProcessHit(hitData, out HitResult hitResult))
        {
            return;
        }

        hitTargets.Add(hurtbox);
        onHitResolved?.Invoke(hitData, hitResult);

        if (attackVFXData.HitVFX != null)
        {
            SpawnHitVFX(hurtbox);
        }
        else if (attackVFXData.HealVFX != null && hitResult.HealthRestored > 0f)
        {
            CombatVFXSpawner.SpawnParticleVFX(attackVFXData.HealVFX, hurtbox.AimPosition, Quaternion.identity, combatTime);
        }

        if (hitMode == ProjectileHitMode.Single || hitTargets.Count >= maxPiercingTargets)
        {
            hasHitTarget = true;
            ReturnToPool();
        }
    }

    private void SpawnHitVFX(Hurtbox hurtbox)
    {
        float hitVFXAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + hitVFXRotationOffset;
        Quaternion hitVFXRotation = Quaternion.Euler(0f, 0f, hitVFXAngle);
        CombatVFXSpawner.SpawnSimpleSpriteVFX(attackVFXData.HitVFX, hurtbox, hitVFXRotation, combatTime);
    }

    private Vector3 GetHitPosition(Collider2D otherCollider, Hurtbox hurtbox)
    {
        if (col == null || otherCollider == null)
        {
            return hurtbox.AimPosition;
        }

        ColliderDistance2D colliderDistance = col.Distance(otherCollider);
        if (!colliderDistance.isValid)
        {
            return hurtbox.AimPosition;
        }

        Vector2 contactPosition = (colliderDistance.pointA + colliderDistance.pointB) * 0.5f;
        return new Vector3(contactPosition.x, contactPosition.y, transform.position.z);
    }

    private bool CanStart()
    {
        return isInitialized && rb != null && col != null && attackerTeam != null && target != null &&
               target.OwnerRuntime != null && !target.OwnerRuntime.IsDead && rawEffectValue > 0f && combatTime != null;
    }

    private void CacheReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        lifetime = Mathf.Max(0f, lifetime);
        maxRange = Mathf.Max(0f, maxRange);
        maxTurnAngle = Mathf.Clamp(maxTurnAngle, 0f, 360f);
        CacheReferences();
    }
#endif
}
