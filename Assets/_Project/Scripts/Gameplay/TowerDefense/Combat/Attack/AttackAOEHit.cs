using System;
using System.Collections.Generic;
using UnityEngine;

public enum AttackAOEHitMode
{
    OneHit,
    Continuous
}

public class AttackAOEHit : MonoBehaviour, IPoolable
{
    public int PrefabID { get; set; }

    [Header("References")]
    [SerializeField] private Collider2D col;

    [Header("VFX")]
    [SerializeField] private SimpleSpriteAnimatorVFX areaVFXPrefab;
    [SerializeField] private TriggeredSpriteAnimatorVFX triggeredAreaVFXPrefab;

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
    private CountdownTimer durationTimer;
    private CountdownTimer tickTimer;
    private CountdownTimer effectDelayTimer;

    private AttackAOEHitMode hitMode;
    private float duration;
    private float tickInterval;
    private float effectDelay;

    private Action onAttackFinished;
    private Action<HitData, HitResult> onHitResolved;
    private Func<int, float> onTargetsTriggered;

    private bool isInitialized;
    private bool isReturningToPool;

    private void Awake()
    {
        CacheReferences();
    }

    private void FixedUpdate()
    {
        if (!isInitialized || isReturningToPool)
        {
            return;
        }

        float deltaTime = combatTime.CombatFixedDeltaTime;

        if (effectDelayTimer != null && effectDelayTimer.IsRunning)
        {
            effectDelayTimer.Tick(deltaTime);
            if (effectDelayTimer.IsRunning)
            {
                return;
            }
        }

        if (hitMode == AttackAOEHitMode.OneHit)
        {
            TriggerOneHit();
            return;
        }

        TickContinuousHit(deltaTime);

        if (durationTimer == null)
        {
            return;
        }

        durationTimer.Tick(deltaTime);
        if (durationTimer.IsFinished)
        {
            ReturnToPool();
        }
    }

    public void Initialize(AttackExecutionData executionData, AttackVFXData vfxData, CombatTimeController combatTime,
                        Action onFinished = null, Action<HitData, HitResult> onHitResolved = null, Func<int, float> onTargetsTriggered = null,
                        AttackAOEHitMode hitMode = AttackAOEHitMode.OneHit,
                        float duration = 0f, float tickInterval = 0f, float effectDelay = 0f)
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
        this.onTargetsTriggered = onTargetsTriggered;
        this.hitMode = hitMode;
        this.duration = Mathf.Max(0f, duration);
        this.tickInterval = Mathf.Max(0.01f, tickInterval);
        this.effectDelay = Mathf.Max(0f, effectDelay);

        isInitialized = true;
    }

    public void OnSpawn()
    {
        isReturningToPool = false;
        hitHurtboxes.Clear();
        overlapBuffer.Clear();
        aoeTargets.Clear();

        CacheReferences();

        if (!CanStart())
        {
            Debug.LogError("[AttackAOEHit] AOE hit is missing required data before spawning.", this);
            ReturnToPool();
            return;
        }

        if (effectDelay > 0f)
        {
            effectDelayTimer = new CountdownTimer(effectDelay);
            effectDelayTimer.StartTimer();
        }

        if (hitMode == AttackAOEHitMode.Continuous)
        {
            durationTimer = new CountdownTimer(duration);
            durationTimer.StartTimer();

            tickTimer = new CountdownTimer(tickInterval);
            tickTimer.StartTimer();
        }

        if (triggeredAreaVFXPrefab != null)
        {
            CombatVFXSpawner.SpawnTriggeredSpriteVFX(triggeredAreaVFXPrefab, transform.position, transform.rotation, combatTime);
        }
        else if (areaVFXPrefab != null)
        {
            CombatVFXSpawner.SpawnSimpleSpriteVFX(areaVFXPrefab, transform.position, transform.rotation, combatTime);
        }
    }

    public void OnDespawn()
    {
        if (durationTimer != null)
        {
            durationTimer.StopTimer();
            durationTimer = null;
        }

        if (tickTimer != null)
        {
            tickTimer.StopTimer();
            tickTimer = null;
        }

        if (effectDelayTimer != null)
        {
            effectDelayTimer.StopTimer();
            effectDelayTimer = null;
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
        onTargetsTriggered = null;
        hitMode = AttackAOEHitMode.OneHit;
        duration = 0f;
        tickInterval = 0f;
        effectDelay = 0f;

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

    private void TriggerOneHit()
    {
        CollectAOETargets();
        TriggerAOETargets();
        ReturnToPool();
    }

    private void CollectAOETargets()
    {
        overlapBuffer.Clear();
        aoeTargets.Clear();

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

            HitData hitData = CreateHitData(hurtbox);
            if (!HitProcessor.CanProcessHit(hitData, out _))
            {
                continue;
            }

            hitHurtboxes.Add(hurtbox);
            aoeTargets.Add(hurtbox);
        }
    }

    private void TriggerAOETargets()
    {
        if (onTargetsTriggered != null)
        {
            rawEffectValue = Mathf.Max(0f, onTargetsTriggered.Invoke(aoeTargets.Count));
        }
        onTargetsTriggered = null;

        for (int i = 0; i < aoeTargets.Count; i++)
        {
            ProcessTargetHit(aoeTargets[i]);
        }
    }

    private void TickContinuousHit(float deltaTime)
    {
        if (tickTimer == null)
        {
            return;
        }

        tickTimer.Tick(deltaTime);
        if (!tickTimer.IsFinished)
        {
            return;
        }

        hitHurtboxes.Clear();
        CollectAOETargets();
        TriggerAOETargets();

        tickTimer.Reset();
        tickTimer.StartTimer();
    }

    private void ProcessTargetHit(Hurtbox hurtbox)
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
                    SpawnHitVFX(hurtbox);
                    break;
            }
        }
    }

    private HitData CreateHitData(Hurtbox hurtbox)
    {
        return new HitData(attacker, hurtbox, attackerTeam, targetSide, attackEffect, attackType, rawEffectValue, damageType, hurtbox.AimPosition);
    }

    private void SpawnHealVFX(Hurtbox hurtbox, HitResult hitResult)
    {
        if (hitResult.HealthRestored <= 0f || attackVFXData.HealVFX == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnParticleVFX(attackVFXData.HealVFX, hurtbox.AimPosition, Quaternion.identity, combatTime);
    }

    private void SpawnHitVFX(Hurtbox hurtbox)
    {
        if (attackVFXData.HitVFX == null)
        {
            return;
        }

        Vector3 hitPosition = hurtbox.AimPosition;
        float hitVFXAngle = Mathf.Atan2(hitPosition.y - transform.position.y, hitPosition.x - transform.position.x) * Mathf.Rad2Deg;
        Quaternion hitVFXRotation = Quaternion.Euler(0f, 0f, hitVFXAngle);
        CombatVFXSpawner.SpawnSimpleSpriteVFX(attackVFXData.HitVFX, hurtbox, hitVFXRotation, combatTime);
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
        CacheReferences();
    }
#endif
}
