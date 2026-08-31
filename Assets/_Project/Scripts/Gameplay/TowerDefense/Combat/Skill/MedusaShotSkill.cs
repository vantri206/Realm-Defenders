using System;
using UnityEngine;

[Serializable]
public class MedusaShotSkill : NormalAttackOverrideSkill
{
    private const string StunStatusId = "SK05_Stun";

    [Header("Medusa Shot")]
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float damageMultiplierPerHit = 0.6f;
    [SerializeField] private float stunDuration = 1f;
    [SerializeField] private AttackProjectile projectilePrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    [NonSerialized] private int pendingProjectileCount;

    protected override bool ExecuteOverrideAttack(Hurtbox target)
    {
        if (projectilePrefab == null || target.OwnerRuntime == null || target.OwnerRuntime.IsDead)
        {
            return false;
        }

        int resolvedProjectileCount = Mathf.Max(1, projectileCount);
        float rawDamage = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplierPerHit));
        AttackExecutionData executionData = new AttackExecutionData
        (
            Owner.gameObject,
            Owner.BattleTeam,
            TargetSide.Enemy,
            AttackEffect.Damage,
            Owner.NormalAttackDefinition.AttackType,
            rawDamage,
            AttackDamageType.PhysicalDamage
        );
        AttackVFXData vfxData = new AttackVFXData(hitVFXPrefab);

        pendingProjectileCount = resolvedProjectileCount;
        Owner.FacePosition(target.AimPosition);

        for (int i = 0; i < resolvedProjectileCount; i++)
        {
            AttackProjectile projectile = ObjectPoolingHelper.Spawn
            (
                projectilePrefab,
                Owner.NormalAttackController.AttackOrigin,
                Quaternion.identity,
                spawnedProjectile => spawnedProjectile.Initialize
                (
                    executionData,
                    target,
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

        return true;
    }

    public override void ClearData()
    {
        pendingProjectileCount = 0;
        base.ClearData();
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
