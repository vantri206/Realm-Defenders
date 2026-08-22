using System.Collections.Generic;
using UnityEngine;

public class AttackAOEHit : MonoBehaviour, IPoolable
{
    public int PrefabID { get; set; }

    [Header("References")]
    [SerializeField] private Collider2D col;

    [Header("VFX")]
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    [Header("Settings")]
    [SerializeField] private float aoeTime = 0.2f;

    private readonly HashSet<Hurtbox> hitHurtboxes = new HashSet<Hurtbox>();

    private GameObject attacker;
    private TeamIdentity attackerTeam;
    private TargetSide targetSide;
    private AttackEffect attackEffect;
    private UnitAttackType attackType;
    private ParticleVFX healVFXPrefab;
    private float baseEffectValue;
    private AttackDamageType damageType;
    private CombatTimeController combatTime;
    private CountdownTimer aoeTimer;

    private bool isInitialized;
    private bool isReturningToPool;

    protected virtual SimpleSpriteAnimatorVFX HitVFXPrefab => hitVFXPrefab;

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

    public void Initialize(GameObject attacker, TeamIdentity attackerTeam, TargetSide targetSide,
                           AttackEffect attackEffect, UnitAttackType attackType, ParticleVFX healVFXPrefab,
                           float baseEffectValue,
                           AttackDamageType damageType, CombatTimeController combatTime)
    {
        this.attacker = attacker;
        this.attackerTeam = attackerTeam;
        this.targetSide = targetSide;
        this.attackEffect = attackEffect;
        this.attackType = attackType;
        this.healVFXPrefab = healVFXPrefab;
        this.baseEffectValue = Mathf.Max(0f, baseEffectValue);
        this.damageType = damageType;
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

        if (HitVFXPrefab != null)
        {
            CombatVFXSpawner.SpawnSimpleSpriteVFX(HitVFXPrefab, transform.position, transform.rotation);
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
        healVFXPrefab = null;
        baseEffectValue = 0f;
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

        HitData hitData = new HitData(attacker, hurtbox, attackerTeam, targetSide, attackEffect, attackType,
                                      baseEffectValue, damageType, hurtbox.AimPosition);

        if (HitProcessor.TryProcessHit(hitData, out HitResult hitResult))
        {
            switch(attackEffect)
            {
                case AttackEffect.Heal:
                    SpawnHealVFX(hurtbox, hitResult);
                    break;
            }
        }
    }

    private void SpawnHealVFX(Hurtbox targetHurtbox, in HitResult hitResult)
    {
        if (attackEffect != AttackEffect.Heal || hitResult.HealthRestored <= 0f ||
            healVFXPrefab == null || targetHurtbox == null)
        {
            return;
        }

        CombatVFXSpawner.SpawnParticleVFX(healVFXPrefab, targetHurtbox);
    }

    private bool CanStart()
    {
        return isInitialized && col != null && attackerTeam != null && baseEffectValue > 0f && combatTime != null;
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
