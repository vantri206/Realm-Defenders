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

        aoeTimer.Tick(combatTime.CombatFixedDeltaTime);
        if (aoeTimer.IsFinished)
        {
            ReturnToPool();
        }
    }

    public void Initialize(AttackExecutionData executionData, AttackVFXData vfxData, CombatTimeController combatTime)
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

        isInitialized = true;
    }

    public void OnSpawn()
    {
        isReturningToPool = false;
        hitHurtboxes.Clear();

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

        attacker = null;
        attackerTeam = null;
        targetSide = default;
        attackEffect = default;
        attackType = default;
        rawEffectValue = 0f;
        damageType = default;
        combatTime = null;

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
        if (!isInitialized || isReturningToPool)
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

        hitHurtboxes.Add(hurtbox);

        HitData hitData = new HitData(attacker, hurtbox, attackerTeam, targetSide, attackEffect, attackType,  rawEffectValue, damageType, hurtbox.AimPosition);

        if (HitProcessor.TryProcessHit(hitData, out HitResult hitResult))
        {
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
