using System;
using UnityEngine;

[Serializable]
public class FairyCareSkill : BaseSkill
{
    [Header("Fairy Care")]
    [SerializeField] private float healthThreshold = 0.5f;
    [SerializeField] private float bonusHealMultiplier = 0.2f;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        Owner.NormalAttackController.OnNormalAttackHitResolved += HandleNormalAttackHitResolved;
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
        if (Owner != null && Owner.NormalAttackController != null)
        {
            Owner.NormalAttackController.OnNormalAttackHitResolved -= HandleNormalAttackHitResolved;
        }

        base.ClearData();
    }

    private void HandleNormalAttackHitResolved(int attackId, HitData hitData, HitResult hitResult)
    {
        if (Owner == null || hitData.Effect != AttackEffect.Heal || hitResult.HealthRestored <= 0f || hitData.TargetHurtbox == null)
        {
            return;
        }

        UnitRuntime healedTarget = hitData.TargetHurtbox.OwnerRuntime;
        if (healedTarget == null || healedTarget.IsDead || healedTarget.MaxHealth <= 0f)
        {
            return;
        }

        float healthBeforeHeal = healedTarget.Health.CurrentHealth - hitResult.HealthRestored;
        if (healthBeforeHeal / healedTarget.MaxHealth >= Mathf.Clamp01(healthThreshold))
        {
            return;
        }

        float bonusHeal = DamageCalculator.CalculateBaseEffectValue(hitData.RawEffectValue, Mathf.Max(0f, bonusHealMultiplier));
        DamageSystem.ApplyHeal(new HealRequest(Owner.gameObject, healedTarget.Health, bonusHeal, hitData.HitPosition));
    }
}
