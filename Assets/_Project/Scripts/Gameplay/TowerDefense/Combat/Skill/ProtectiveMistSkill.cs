using System;
using UnityEngine;

[Serializable]
public class ProtectiveMistSkill : BaseSkill
{
    private const string ProtectiveMistStatusId = "SK10_ProtectiveMist";
    private const string DefenseModifierId = "SK10_Defense";
    private const string SpecialDefenseModifierId = "SK10_SpecialDefense";

    [Header("Protective Mist")]
    [SerializeField] private float defenseBonus = 0.12f;
    [SerializeField] private float specialDefenseBonus = 0.12f;
    [SerializeField] private float buffDuration = 3f;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        Owner.NormalAttackController.OnNormalAttackHitResolved += HandleNormalAttackHitResolved;
        Owner.SkillAttackController.OnSkillAttackHitResolved += HandleSkillAttackHitResolved;
    }

    public override bool CanActivate()
    {
        return false;
    }

    public override void Activate()
    {
    }

    public override void ClearData()
    {
        if (Owner != null)
        {
            if (Owner.NormalAttackController != null)
            {
                Owner.NormalAttackController.OnNormalAttackHitResolved -= HandleNormalAttackHitResolved;
            }

            if (Owner.SkillAttackController != null)
            {
                Owner.SkillAttackController.OnSkillAttackHitResolved -= HandleSkillAttackHitResolved;
            }
        }

        base.ClearData();
    }

    private void HandleAttackHitResolved(HitData hitData, HitResult hitResult)
    {
        if (Owner == null || hitData.Effect != AttackEffect.Heal || hitResult.HealthRestored <= 0f || hitData.TargetHurtbox == null)
        {
            return;
        }

        UnitRuntime healedTarget = hitData.TargetHurtbox.OwnerRuntime;
        if (healedTarget == null || healedTarget.IsDead)
        {
            return;
        }

        UnitStatModifier[] defensiveModifiers =
        {
            new UnitStatModifier(UnitStatType.Defense, UnitStatModifierType.AdditivePercent, defenseBonus, DefenseModifierId),
            new UnitStatModifier(UnitStatType.SpecialDefense, UnitStatModifierType.AdditivePercent, specialDefenseBonus, SpecialDefenseModifierId)
        };

        healedTarget.ApplyTemporaryStatModifiers
        (
            ProtectiveMistStatusId,
            Owner.gameObject,
            defensiveModifiers,
            Mathf.Max(0f, buffDuration)
        );
    }

    private void HandleNormalAttackHitResolved(int attackId, HitData hitData, HitResult hitResult)
    {
        HandleAttackHitResolved(hitData, hitResult);
    }

    private void HandleSkillAttackHitResolved(HitData hitData, HitResult hitResult)
    {
        HandleAttackHitResolved(hitData, hitResult);
    }
}
