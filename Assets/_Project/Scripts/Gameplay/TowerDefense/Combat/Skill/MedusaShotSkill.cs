using System;
using UnityEngine;

[Serializable]
public class MedusaShotSkill : NormalAttackOverrideSkill
{
    private const string StunStatusId = "SK05_Stun";

    [Header("Medusa Shot")]
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float damageMultiplierPerHit = 0.6f;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float stunDuration = 1f;
    [SerializeField] private AttackProjectile projectilePrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    [NonSerialized] private CountdownTimer fireTimer;
    [NonSerialized] private int nextProjectileIndex;
    [NonSerialized] private int pendingProjectileCount;
    [NonSerialized] private float rawDamagePerHit;

    protected override int MaxTargetCount => Mathf.Max(1, projectileCount);

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

    protected override bool ExecuteOverrideAttack()
    {
        if (projectilePrefab == null)
        {
            return false;
        }

        rawDamagePerHit = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplierPerHit));
        fireTimer = new CountdownTimer(Mathf.Max(0f, fireRate));
        nextProjectileIndex = 0;
        pendingProjectileCount = MaxTargetCount;
        SpawnNextProjectile();
        return true;
    }

    public override void ClearData()
    {
        StopFireTimer();
        nextProjectileIndex = 0;
        pendingProjectileCount = 0;
        rawDamagePerHit = 0f;
        base.ClearData();
    }

    private void SpawnNextProjectile()
    {
        if (Owner == null || nextProjectileIndex >= MaxTargetCount)
        {
            StopFireTimer();
            return;
        }

        Hurtbox projectileTarget = ResolveProjectileTarget(nextProjectileIndex);
        nextProjectileIndex++;

        if (projectileTarget != null)
        {
            AttackExecutionData executionData = new AttackExecutionData
            (
                Owner.gameObject,
                Owner.BattleTeam,
                TargetSide.Enemy,
                AttackEffect.Damage,
                Owner.NormalAttackDefinition.AttackType,
                rawDamagePerHit,
                AttackDamageType.PhysicalDamage
            );
            AttackVFXData vfxData = new AttackVFXData(hitVFXPrefab);

            AttackProjectile projectile = ObjectPoolingHelper.Spawn
            (
                projectilePrefab,
                Owner.NormalAttackController.AttackOrigin,
                Quaternion.identity,
                spawnedProjectile => spawnedProjectile.Initialize
                (
                    executionData,
                    projectileTarget,
                    vfxData,
                    Owner.CombatTime,
                    HandleProjectileHitResolved,
                    HandleProjectileFinished
                )
            );

            if (projectile == null)
            {
                HandleProjectileFinished();
            }
        }
        else
        {
            HandleProjectileFinished();
        }

        if (nextProjectileIndex >= MaxTargetCount)
        {
            StopFireTimer();
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

    private Hurtbox ResolveProjectileTarget(int targetIndex)
    {
        for (int i = 0; i < OverrideTargets.Count; i++)
        {
            Hurtbox target = OverrideTargets[(targetIndex + i) % OverrideTargets.Count];
            if (IsTargetValid(target))
            {
                return target;
            }
        }

        return null;
    }

    private void HandleProjectileHitResolved(HitData hitData, HitResult hitResult)
    {
        if (Owner == null)
        {
            return;
        }

        UnitRuntime targetRuntime = hitData.TargetHurtbox != null ? hitData.TargetHurtbox.OwnerRuntime : null;
        if (targetRuntime != null)
        {
            targetRuntime.ApplyStun(StunStatusId, Owner.gameObject, Mathf.Max(0f, stunDuration));
        }

        Owner.SkillAttackController.NotifySkillAttackHitResolved(hitData, hitResult);
    }

    private void HandleProjectileFinished()
    {
        if (pendingProjectileCount <= 0)
        {
            return;
        }

        pendingProjectileCount--;
        if (pendingProjectileCount == 0)
        {
            FinishOverrideAttack();
        }
    }
}
