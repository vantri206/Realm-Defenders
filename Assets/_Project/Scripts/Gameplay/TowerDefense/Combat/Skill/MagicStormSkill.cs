using System;
using UnityEngine;

[Serializable]
public class MagicStormSkill : AutoActiveSkill
{
    [Header("Magic Storm")]
    [SerializeField] private float duration = 4f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private float damageMultiplierPerTick = 0.5f;
    [SerializeField] private AttackAOEHit aoeHitPrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    [NonSerialized] private Hurtbox activationTarget;

    public override bool CanActivate()
    {
        if (!CanCastSkill || aoeHitPrefab == null || Owner.NormalAttackController == null)
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
        if (activationTarget == null)
        {
            FinishSkill();
            return;
        }

        Vector3 stormPosition = activationTarget.AimPosition;
        float rawDamagePerTick = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplierPerTick));

        Owner.FacePosition(stormPosition);
        activationTarget = null;
        AttackExecutionData executionData = new AttackExecutionData
        (
            Owner.gameObject,
            Owner.BattleTeam,
            TargetSide.Enemy,
            AttackEffect.Damage,
            Owner.NormalAttackDefinition.AttackType,
            rawDamagePerTick,
            AttackDamageType.MagicalDamage
        );
        AttackVFXData vfxData = new AttackVFXData(hitVFXPrefab);

        AttackAOEHit aoeHit = ObjectPoolingHelper.Spawn
        (
            aoeHitPrefab,
            stormPosition,
            Quaternion.identity,
            spawnedAOEHit => spawnedAOEHit.Initialize
            (
                executionData,
                vfxData,
                Owner.CombatTime,
                FinishSkill,
                HandleStormHitResolved,
                null,
                AttackAOEHitMode.Continuous,
                duration,
                tickInterval
            )
        );

        if (aoeHit == null)
        {
            FinishSkill();
        }
    }

    private void HandleStormHitResolved(HitData hitData, HitResult hitResult)
    {
        if (Owner != null)
        {
            Owner.SkillAttackController.NotifySkillAttackHitResolved(hitData, hitResult);
        }
    }

    public override void ClearData()
    {
        activationTarget = null;
        base.ClearData();
    }
}
