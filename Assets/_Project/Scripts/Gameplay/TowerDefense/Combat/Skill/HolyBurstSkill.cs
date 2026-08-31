using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HolyBurstSkill : AutoActiveSkill
{
    [Header("Holy Burst")]
    [SerializeField] private float damageMultiplier = 1.2f;
    [SerializeField] private float shieldMultiplier = 0.15f;
    [SerializeField] private UnitAttackType attackType = UnitAttackType.Melee;
    [SerializeField] private List<Vector2Int> areaPattern = new List<Vector2Int>
    {
        Vector2Int.zero,
    };
    [SerializeField] private AttackAOEHit aoeHitPrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    public override bool CanActivate()
    {
        return CanCastSkill && Owner.HasSkillTarget(areaPattern, attackType);
    }

    public override void Activate()
    {
        float shieldValue = DamageCalculator.CalculateBaseEffectValue(Owner.MaxHealth, shieldMultiplier);

        if (Owner.Shield != null)
        {
            Owner.Shield.AddShield(shieldValue);
        }
        else
        {
            Debug.LogWarning($"Owner {Owner.name} does not have a Shield component.");
        }

        float rawDamage = DamageCalculator.CalculateBaseDamage(Owner.Attack, damageMultiplier);

        AttackExecutionData executionData = new AttackExecutionData
        (
            Owner.gameObject,
            Owner.BattleTeam,
            TargetSide.Enemy,
            AttackEffect.Damage,
            attackType,
            rawDamage,
            AttackDamageType.PhysicalDamage
        );

        AttackVFXData vfxData = new AttackVFXData(hitVFXPrefab);
        AttackAOEHit aoeHit = ObjectPoolingHelper.Spawn
        (
            aoeHitPrefab,
            Owner.CenterPosition,
            Quaternion.identity,
            spawnedAOEHit => spawnedAOEHit.Initialize(executionData, vfxData, Owner.CombatTime, FinishSkill, Owner.SkillAttackController.NotifySkillAttackHitResolved)
        );

        if (aoeHit == null)
        {
            FinishSkill();
        }
    }
}
