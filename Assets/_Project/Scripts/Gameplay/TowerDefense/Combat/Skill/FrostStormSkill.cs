using System;
using UnityEngine;

[Serializable]
public class FrostStormSkill : NormalAttackOverrideSkill
{
    private const string SlowStatusId = "SK13_FrostStormSlow";
    private const string MoveSpeedModifierId = "SK13_MoveSpeed";

    [Header("Frost Storm")]
    [SerializeField] private float damageMultiplier = 0.7f;
    [SerializeField] private float moveSpeedReduction = 0.35f;
    [SerializeField] private float slowDuration = 5f;
    [SerializeField] private float effectDelay;
    [SerializeField] private AttackAOEHit aoeHitPrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    protected override bool ExecuteOverrideAttack()
    {
        if (aoeHitPrefab == null || OverrideTargets.Count == 0)
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
            AttackDamageType.MagicalDamage
        );
        AttackVFXData vfxData = new AttackVFXData(hitVFXPrefab);

        AttackAOEHit aoeHit = ObjectPoolingHelper.Spawn
        (
            aoeHitPrefab,
            target.AimPosition,
            Quaternion.identity,
            spawnedAOEHit => spawnedAOEHit.Initialize
            (
                executionData,
                vfxData,
                Owner.CombatTime,
                FinishOverrideAttack,
                HandleFrostStormHitResolved,
                null,
                AttackAOEHitMode.OneHit,
                effectDelay: effectDelay
            )
        );

        return aoeHit != null;
    }

    private void HandleFrostStormHitResolved(HitData hitData, HitResult hitResult)
    {
        if (Owner == null)
        {
            return;
        }

        UnitRuntime targetRuntime = hitData.TargetHurtbox != null ? hitData.TargetHurtbox.OwnerRuntime : null;
        if (targetRuntime != null && hitResult.DamageTaken > 0f)
        {
            UnitStatModifier[] modifiers =
            {
                new UnitStatModifier(UnitStatType.MoveSpeed, UnitStatModifierType.AdditivePercent,
                                     -Mathf.Abs(moveSpeedReduction), MoveSpeedModifierId)
            };

            targetRuntime.ApplyTemporaryStatModifiers(SlowStatusId, Owner.gameObject, modifiers, Mathf.Max(0f, slowDuration));
        }

        Owner.SkillAttackController.NotifySkillAttackHitResolved(hitData, hitResult);
    }
}
