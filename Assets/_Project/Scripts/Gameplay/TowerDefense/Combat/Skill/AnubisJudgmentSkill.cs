using System;
using UnityEngine;

[Serializable]
public class AnubisJudgmentSkill : BaseSkill
{
    [Header("Anubis Judgment")]
    [SerializeField] private float defenseDamageMultiplier = 0.15f;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        Owner.NormalAttackController.OnNormalAttackFired += HandleNormalAttackFired;
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
            Owner.NormalAttackController.OnNormalAttackFired -= HandleNormalAttackFired;
        }

        base.ClearData();
    }

    private void HandleNormalAttackFired(NormalAttackFiredData firedData)
    {
        if (Owner == null || firedData == null)
        {
            return;
        }

        float bonusDamage = DamageCalculator.CalculateBaseDamage(Owner.Defense, Mathf.Max(0f, defenseDamageMultiplier));
        firedData.RawEffectValue += bonusDamage;
    }
}
