using System;
using System.Collections.Generic;
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
    [SerializeField] private UnitAttackType attackType = UnitAttackType.Ranged;
    [SerializeField] private List<Vector2Int> areaPattern = new List<Vector2Int> { Vector2Int.zero };
    [SerializeField] private ParticleVFX healVFXPrefab;

    [NonSerialized] private List<Hurtbox> targets;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        targets = new List<Hurtbox>();
    }

protected override bool CanUseOverrideAttack(out Hurtbox target)
{
    target = null;

    if (Owner == null || Owner.NormalAttackController == null || !Owner.NormalAttackController.IsReadyAttack)
    {
        return false;
    }

    return TrySelectBattleWishTarget(out target);
}

    protected override bool TryUseOverrideAttack(out Hurtbox target)
    {
        if (!TrySelectBattleWishTarget(out target))
        {
            return false;
        }

        return Owner.NormalAttackController.TryUseOverrideAttack(target);
    }

    private bool TrySelectBattleWishTarget(out Hurtbox target)
    {
        target = null;
        if (Owner == null)
        {
            return false;
        }

        IReadOnlyList<Vector2Int> resolvedPattern = AttackPatternResolver.RefreshAttackPattern(areaPattern, Owner.FacingDirection);
        Owner.CollectSkillTargets(resolvedPattern, TargetSide.Ally, AttackEffect.Damage, attackType, targets);
        if (targets.Count == 0)
        {
            return false;
        }

        target = targets[0];
        return target != null;
    }

    protected override bool ExecuteOverrideAttack(Hurtbox target)
    {
        IReadOnlyList<Vector2Int> resolvedPattern = AttackPatternResolver.RefreshAttackPattern(areaPattern, Owner.FacingDirection);
        Owner.CollectSkillTargets(resolvedPattern, TargetSide.Ally, AttackEffect.Damage, attackType, targets);
        if (targets.Count == 0)
        {
            return false;
        }

        float rawHeal = DamageCalculator.CalculateBaseEffectValue(Owner.Attack, Mathf.Max(0f, healMultiplier));
        UnitStatModifier[] attackModifiers =
        {
            new UnitStatModifier(UnitStatType.Attack, UnitStatModifierType.AdditivePercent, attackBonus, AttackModifierId)
        };

        for (int i = 0; i < targets.Count; i++)
        {
            Hurtbox ally = targets[i];
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
                attackType,
                rawHeal,
                AttackDamageType.TrueDamage,
                ally.AimPosition
            );

            if (Owner.SkillAttackController.TryProcessHit(hitData, out HitResult hitResult))
            {
                if (healVFXPrefab != null)
                {
                    CombatVFXSpawner.SpawnParticleVFX(healVFXPrefab, ally.AimPosition, Quaternion.identity);
                }
            }
        }

        FinishOverrideAttack();
        return true;
    }

    public override void ClearData()
    {
        targets?.Clear();
        base.ClearData();
    }
}
