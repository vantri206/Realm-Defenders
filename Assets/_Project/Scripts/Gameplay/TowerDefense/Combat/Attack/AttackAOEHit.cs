using System;
using System.Collections.Generic;
using UnityEngine;

public class AttackAOEHit : MonoBehaviour, IPoolable
{
    public int PrefabID { get; set; }

    [Header("References")]
    [SerializeField] private Collider2D col;

    [Header("VFX")]
    [SerializeField] private SimpleSpriteAnimatorVFX areaVFXPrefab;

    [Header("Settings")]
    [SerializeField] private float aoeTime = 0.2f;

    private readonly HashSet<Hurtbox> hitHurtboxes = new HashSet<Hurtbox>();
    private readonly List<Collider2D> overlapBuffer = new List<Collider2D>();
    private readonly List<Hurtbox> aoeTargets = new List<Hurtbox>();

    private GameObject attacker;
    private TeamIdentity attackerTeam;
    private TargetSide targetSide;
    private AttackEffect attackEffect;
    private UnitAttackType attackType;
    private float rawEffectValue;
    private AttackDamageType damageType;
    private CombatTimeController combatTime;
    private AttackVFXData attackVFXData;
    private CountdownTimer aoeTimer;

    private Action onAttackFinished;
    private Action<HitData, HitResult> onHitResolved;
    private Action<int> onTargetsCollected;

    private bool countTargetsBeforeHit;
    private bool hasCountedTargets;

    private bool isInitialized;
    private bool isReturningToPool;

    private void Awake()
    {
        CacheReferences();
    }

    private void FixedUpdate()
    {
        if (!isInitialized || isReturningToPool || aoeTimer == null)
        {
            return;
        }

        if (countTargetsBeforeHit && !hasCountedTargets)
        {
            CollectAOETargetsAndResolveHits();
        }

        aoeTimer.Tick(combatTime.CombatFixedDeltaTime);
        if (aoeTimer.IsFinished)
        {
            ReturnToPool();
        }
    }

    public void Initialize(AttackExecutionData executionData, AttackVFXData vfxData, CombatTimeController combatTime,
                           Action onFinished = null, Action<HitData, HitResult> onHitResolved = null,
                           Action<int> onTargetsCollected = null, bool countTargetsBeforeHit = false)
    {
        this.attacker = executionData.Attacker;
        this.attackerTeam = executionData.AttackerTeam;
        this.targetSide = executionData.TargetSide;
        this.attackEffect = executionData.AttackEffect;
        this.attackType = executionData.AttackType;
        this.rawEffectValue = Mathf.Max(0f, executionData.RawEffectValue);
        this.damageType = executionData.DamageType;
        this.attackVFXData = vfxData;
        this.combatTime = combatTime;
        this.onAttackFinished = onFinished;
        this.onHitResolved = onHitResolved;
        this.onTargetsCollected = onTargetsCollected;
        this.countTargetsBeforeHit = countTargetsBeforeHit;

        isInitialized = true;
    }

    public void OnSpawn()
    {
        isReturningToPool = false;
        hitHurtboxes.Clear();
        overlapBuffer.Clear();
        aoeTargets.Clear();
        hasCountedTargets = false;

        CacheReferences();

        if (!CanStart())
        {
            Debug.LogError("[AttackAOEHit] AOE hit is missing required data before spawning.", this);
            ReturnToPool();
            return;
        }

        aoeTimer = new CountdownTimer(aoeTime);
        aoeTimer.StartTimer();

        if (areaVFXPrefab != null)
        {
            CombatVFXSpawner.SpawnSimpleSpriteVFX(areaVFXPrefab, transform.position, transform.rotation);
        }
    }

    public void OnDespawn()
    {
        if (aoeTimer != null)
        {
            aoeTimer.StopTimer();
            aoeTimer = null;
        }

        hitHurtboxes.Clear();
        overlapBuffer.Clear();
        aoeTargets.Clear();

        attacker = null;
        attackerTeam = null;
        targetSide = default;
        attackEffect = default;
        attackType = default;
        rawEffectValue = 0f;
        damageType = default;
        combatTime = null;
        onAttackFinished = null;
        onHitResolved = null;
        onTargetsCollected = null;
        countTargetsBeforeHit = false;
        hasCountedTargets = false;

        isInitialized = false;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryProcessAOEHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryProcessAOEHit(other);
    }

    private void TryProcessAOEHit(Collider2D other)
    {
        if (!isInitialized || isReturningToPool || countTargetsBeforeHit)
        {
            return;
        }

        if (!other.TryGetComponent(out Hurtbox hurtbox) || hitHurtboxes.Contains(hurtbox))
        {
            return;
        }

        if (col == null || !col.OverlapPoint(hurtbox.AimPosition))
        {
            return;
        }

        ProcessHurtbox(hurtbox);
    }

    private void CollectAOETargetsAndResolveHits()
    {
        hasCountedTargets = true;

        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(GameLayer.HurtboxMask);
        contactFilter.useTriggers = true;

        Physics2D.OverlapCollider(col, contactFilter, overlapBuffer);
        for (int i = 0; i < overlapBuffer.Count; i++)
        {
            Collider2D targetCollider = overlapBuffer[i];
            if (targetCollider == null || !targetCollider.TryGetComponent(out Hurtbox hurtbox) || hitHurtboxes.Contains(hurtbox))
            {
                continue;
            }

            if (!col.OverlapPoint(hurtbox.AimPosition))
            {
                continue;
            }

            HitData hitData = CreateHitData(hurtbox);
            if (!HitProcessor.CanProcessHit(hitData, out _))
            {
                continue;
            }

            hitHurtboxes.Add(hurtbox);
            aoeTargets.Add(hurtbox);
        }

        onTargetsCollected?.Invoke(aoeTargets.Count);
        onTargetsCollected = null;

        for (int i = 0; i < aoeTargets.Count; i++)
        {
            ProcessHurtbox(aoeTargets[i]);
        }
    }

    private void ProcessHurtbox(Hurtbox hurtbox)
    {
        HitData hitData = CreateHitData(hurtbox);

        if (HitProcessor.TryProcessHit(hitData, out HitResult hitResult))
        {
            onHitResolved?.Invoke(hitData, hitResult);

            switch(attackEffect)
            {
                case AttackEffect.Heal:
                    SpawnHealVFX(hurtbox, hitResult);
                    break;
                case AttackEffect.Damage:
                    SpawnHitVFX(hurtbox.AimPosition);
                    break;
            }
        }
    }

    private HitData CreateHitData(Hurtbox hurtbox)
    {
        return new HitData(attacker, hurtbox, attackerTeam, targetSide, attackEffect, attackType,
                           rawEffectValue, damageType, hurtbox.AimPosition);
    }

    private void SpawnHealVFX(Hurtbox hurtbox, HitResult hitResult)
    {
        if (hitResult.HealthRestored <= 0f || attackVFXData.HealVFX == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnParticleVFX(attackVFXData.HealVFX, hurtbox.AimPosition, Quaternion.identity);
    }

    private void SpawnHitVFX(Vector3 hitPosition)
    {
        if (attackVFXData.HitVFX == null)
        {
            return;
        }

        float hitVFXAngle = Mathf.Atan2(hitPosition.y - transform.position.y, hitPosition.x - transform.position.x) * Mathf.Rad2Deg;
        Quaternion hitVFXRotation = Quaternion.Euler(0f, 0f, hitVFXAngle);
        CombatVFXSpawner.SpawnSimpleSpriteVFX(attackVFXData.HitVFX, hitPosition, hitVFXRotation);
    }
    
    private bool CanStart()
    {
        return isInitialized && col != null && attackerTeam != null && rawEffectValue > 0f && combatTime != null;
    }

    private void CacheReferences()
    {
        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        aoeTime = Mathf.Max(0f, aoeTime);
        CacheReferences();
    }
#endif
}
