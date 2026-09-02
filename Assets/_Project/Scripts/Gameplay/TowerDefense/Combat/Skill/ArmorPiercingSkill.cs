using System;
using UnityEngine;

[Serializable]
public class ArmorPiercingSkill : BaseSkill
{
    private const string DefenseReductionStatusId = "SK20_ArmorPiercing";
    private const string DefenseReductionModifierId = "SK20_Defense";

    [Header("Armor Piercing")]
    [SerializeField] private float defenseReductionPerStack = 0.08f;
    [SerializeField] private float duration = 4f;
    [SerializeField] private int maxStackCount = 3;

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
        if (Owner == null || hitData.Effect != AttackEffect.Damage || hitResult.DamageTaken <= 0f || hitData.TargetHurtbox == null)
        {
            return;
        }

        UnitRuntime targetRuntime = hitData.TargetHurtbox.OwnerRuntime;
        if (targetRuntime == null || targetRuntime.IsDead)
        {
            return;
        }

        targetRuntime.ApplyDefenseReduction
        (
            DefenseReductionStatusId,
            Owner.gameObject,
            Mathf.Max(0f, defenseReductionPerStack),
            Mathf.Max(0f, duration),
            Mathf.Max(1, maxStackCount),
            DefenseReductionModifierId
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
