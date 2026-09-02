using System;
using UnityEngine;

[Serializable]
public class FrozenBladesSkill : AutoActiveSkill
{
    private const string AttackIntervalStatusId = "SK15_FrozenBladesHaste";
    private const string AttackIntervalModifierId = "SK15_AttackInterval";

    [Header("Frozen Blades")]
    [SerializeField] private int projectileCount = 2;
    [SerializeField] private float damageMultiplierPerProjectile = 1.1f;
    [SerializeField] private float fireInterval = 0.15f;
    [SerializeField] private float attackIntervalReduction = 0.2f;
    [SerializeField] private float buffDuration = 5f;
    [SerializeField] private AttackProjectile projectilePrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    [NonSerialized] private Hurtbox activationTarget;
    [NonSerialized] private CountdownTimer fireTimer;
    [NonSerialized] private int firedProjectileCount;
    [NonSerialized] private float rawDamagePerProjectile;

    protected override bool InterruptsNormalAttack => false;

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (fireTimer == null || !fireTimer.IsRunning)
        {
            return;
        }

        fireTimer.Tick(deltaTime);
        if (fireTimer.IsFinished)
        {
            SpawnNextProjectile();
        }
    }

    public override bool CanActivate()
    {
        if (!CanCastSkill || projectilePrefab == null || Owner.NormalAttackController == null)
        {
            return false;
        }

        return Owner.NormalAttackController.TrySelectTarget
        (
            Owner.ResolvedAttackPattern,
            TargetSide.Enemy,
            AttackEffect.Damage,
            out activationTarget
        );
    }

    public override void Activate()
    {
        if (!IsTargetValid(activationTarget))
        {
            activationTarget = null;
            FinishSkill();
            return;
        }

        firedProjectileCount = 0;
        rawDamagePerProjectile = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplierPerProjectile));
        fireTimer = new CountdownTimer(Mathf.Max(0f, fireInterval));
        SpawnNextProjectile();
    }

    public override void ClearData()
    {
        StopFireTimer();
        activationTarget = null;
        firedProjectileCount = 0;
        rawDamagePerProjectile = 0f;
        base.ClearData();
    }

    private void SpawnNextProjectile()
    {
        int resolvedProjectileCount = Mathf.Max(1, projectileCount);
        if (firedProjectileCount >= resolvedProjectileCount)
        {
            FinishFiring();
            return;
        }

        if (IsTargetValid(activationTarget))
        {
            AttackExecutionData executionData = new AttackExecutionData
            (
                Owner.gameObject,
                Owner.BattleTeam,
                TargetSide.Enemy,
                AttackEffect.Damage,
                Owner.NormalAttackDefinition.AttackType,
                rawDamagePerProjectile,
                AttackDamageType.PhysicalDamage
            );
            AttackVFXData vfxData = new AttackVFXData(hitVFXPrefab);

            ObjectPoolingHelper.Spawn
            (
                projectilePrefab,
                Owner.NormalAttackController.AttackOrigin,
                Quaternion.identity,
                spawnedProjectile => spawnedProjectile.Initialize
                (
                    executionData,
                    activationTarget,
                    vfxData,
                    Owner.CombatTime,
                    HandleProjectileHitResolved
                )
            );
        }

        firedProjectileCount++;
        if (firedProjectileCount >= resolvedProjectileCount)
        {
            FinishFiring();
            return;
        }

        if (fireTimer.TotalTime <= 0f)
        {
            SpawnNextProjectile();
            return;
        }

        fireTimer.Reset();
        fireTimer.StartTimer();
    }

    private void FinishFiring()
    {
        StopFireTimer();

        UnitStatModifier[] modifiers =
        {
            new UnitStatModifier(UnitStatType.AttackInterval, UnitStatModifierType.AdditivePercent,
                                 -Mathf.Abs(attackIntervalReduction), AttackIntervalModifierId)
        };

        Owner.ApplyTemporaryStatModifiers(AttackIntervalStatusId, Owner.gameObject, modifiers, Mathf.Max(0f, buffDuration));
        activationTarget = null;
        FinishSkill();
    }

    private void HandleProjectileHitResolved(HitData hitData, HitResult hitResult)
    {
        if (Owner != null)
        {
            Owner.SkillAttackController.NotifySkillAttackHitResolved(hitData, hitResult);
        }
    }

    private void StopFireTimer()
    {
        if (fireTimer != null)
        {
            fireTimer.StopTimer();
            fireTimer = null;
        }
    }

    private static bool IsTargetValid(Hurtbox target)
    {
        return target != null && target.OwnerRuntime != null && !target.OwnerRuntime.IsDead;
    }
}
