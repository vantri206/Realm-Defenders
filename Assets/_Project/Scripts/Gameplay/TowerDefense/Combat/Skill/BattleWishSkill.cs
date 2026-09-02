using System;
using UnityEngine;

[Serializable]
public class BattleWishSkill : NormalAttackOverrideSkill
{
    private const string AttackBuffStatusId = "SK09_BattleWish";
    private const string AttackModifierId = "SK09_Attack";

    [Header("Battle Wish")]
    [SerializeField] private float healMultiplier = 1.2f;
    [SerializeField] private float attackBonus = 0.15f;
    [SerializeField] private float buffDuration = 4f;
    [SerializeField] private ParticleVFX healVFXPrefab;

    protected override int MaxTargetCount => int.MaxValue;
    protected override TargetSide OverrideTargetSide => TargetSide.Ally;
    protected override AttackEffect OverrideAttackEffect => AttackEffect.Damage;

    protected override bool ExecuteOverrideAttack()
    {
        float rawHeal = DamageCalculator.CalculateBaseEffectValue(Owner.Attack, Mathf.Max(0f, healMultiplier));
        UnitStatModifier[] attackModifiers =
        {
            new UnitStatModifier(UnitStatType.Attack, UnitStatModifierType.AdditivePercent, attackBonus, AttackModifierId)
        };

        for (int i = 0; i < OverrideTargets.Count; i++)
        {
            Hurtbox ally = OverrideTargets[i];
            if (ally == null || ally.OwnerRuntime == null || ally.OwnerRuntime.IsDead)
            {
                continue;
            }

            ally.OwnerRuntime.ApplyTemporaryStatModifiers
            (
                AttackBuffStatusId,
                Owner.gameObject,
                attackModifiers,
                Mathf.Max(0f, buffDuration)
            );

            HitData hitData = new HitData
            (
                Owner.gameObject,
                ally,
                Owner.BattleTeam,
                TargetSide.Ally,
                AttackEffect.Heal,
                Owner.NormalAttackDefinition.AttackType,
                rawHeal,
                AttackDamageType.TrueDamage,
                ally.AimPosition
            );

            if (Owner.SkillAttackController.TryProcessHit(hitData, out _))
            {
                if (healVFXPrefab != null)
                {
                    CombatVFXSpawner.SpawnParticleVFX(healVFXPrefab, ally.AimPosition, Quaternion.identity, Owner.CombatTime);
                }
            }
        }

        FinishOverrideAttack();
        return true;
    }
}
