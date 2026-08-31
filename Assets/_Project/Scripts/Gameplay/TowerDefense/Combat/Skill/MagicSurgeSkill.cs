using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MagicSurgeSkill : BaseSkill
{
    [Header("Magic Surge")]
    [SerializeField] private int minimumUniqueTargetCount = 3;
    [SerializeField] private float bonusMagicDamageMultiplier = 0.2f;

    [NonSerialized] private Dictionary<int, float> bonusDamageByAttackId;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        bonusDamageByAttackId = new Dictionary<int, float>();

        Owner.NormalAttackController.OnNormalAttackFired += HandleNormalAttackFired;
        Owner.NormalAttackController.OnNormalAttackHitResolved += HandleNormalAttackHitResolved;
        Owner.NormalAttackController.OnNormalAttackFinished += HandleNormalAttackFinished;
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
            Owner.NormalAttackController.OnNormalAttackHitResolved -= HandleNormalAttackHitResolved;
            Owner.NormalAttackController.OnNormalAttackFinished -= HandleNormalAttackFinished;
        }

        bonusDamageByAttackId?.Clear();
        base.ClearData();
    }

    private void HandleNormalAttackFired(NormalAttackFiredData firedData)
    {
        if (firedData.UniqueTargetCount < Mathf.Max(1, minimumUniqueTargetCount))
        {
            return;
        }

        bonusDamageByAttackId[firedData.AttackId] = DamageCalculator.CalculateBaseDamage
        (
            firedData.AttackSnapshot,
            Mathf.Max(0f, bonusMagicDamageMultiplier)
        );
    }

    private void HandleNormalAttackHitResolved(int attackId, HitData hitData, HitResult hitResult)
    {
        if (!bonusDamageByAttackId.TryGetValue(attackId, out float bonusDamage) ||
            hitData.Effect != AttackEffect.Damage || hitResult.DamageTaken <= 0f)
        {
            return;
        }

        HitData bonusHitData = new HitData
        (
            hitData.Attacker,
            hitData.TargetHurtbox,
            hitData.AttackerTeam,
            hitData.TargetSide,
            AttackEffect.Damage,
            hitData.AttackType,
            bonusDamage,
            AttackDamageType.MagicalDamage,
            hitData.HitPosition
        );

        HitProcessor.TryProcessHit(bonusHitData, out _);
    }

    private void HandleNormalAttackFinished(int attackId)
    {
        bonusDamageByAttackId.Remove(attackId);
    }
}
