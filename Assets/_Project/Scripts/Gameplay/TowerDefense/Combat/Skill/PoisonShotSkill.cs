using System;
using UnityEngine;

[Serializable]
public class PoisonShotSkill : BaseSkill
{
    private const string PoisonStatusId = "SK06_Poison";

    [Header("Poison Shot")]
    [SerializeField] private float poisonDuration = 4f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private float damageMultiplierPerTick = 0.1f;
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

        float damagePerTick = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplierPerTick));
        targetRuntime.ApplyPoison
        (
            PoisonStatusId,
            Owner.gameObject,
            damagePerTick,
            Mathf.Max(0f, poisonDuration),
            Mathf.Max(0f, tickInterval),
            Mathf.Max(1, maxStackCount)
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
