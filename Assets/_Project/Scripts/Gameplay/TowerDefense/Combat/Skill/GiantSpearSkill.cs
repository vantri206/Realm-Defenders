using System;
using UnityEngine;

[Serializable]
public class GiantSpearSkill : NormalAttackOverrideSkill
{
    [Header("Giant Spear")]
    [SerializeField] private float damageMultiplier = 1.8f;
    [SerializeField] private int maxPiercingTargets = 3;
    [SerializeField] private AttackProjectile projectilePrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    protected override bool ExecuteOverrideAttack()
    {
        if (projectilePrefab == null || OverrideTargets.Count == 0)
        {
            return false;
        }

        Hurtbox target = OverrideTargets[0];
        float rawDamage = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplier));
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
                FinishOverrideAttack,
                ProjectileHitMode.Piercing,
                Mathf.Max(1, maxPiercingTargets)
            )
        );

        return projectile != null;
    }

    private void HandleProjectileHitResolved(HitData hitData, HitResult hitResult)
    {
        if (Owner != null)
        {
            Owner.SkillAttackController.NotifySkillAttackHitResolved(hitData, hitResult);
        }
    }
}
