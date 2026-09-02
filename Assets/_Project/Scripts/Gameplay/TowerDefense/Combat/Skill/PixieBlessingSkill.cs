using System;
using UnityEngine;

[Serializable]
public class PixieBlessingSkill : AutoActiveSkill
{
    private const string BlessingStatusId = "SK17_PixieBlessing";
    private const string AttackIntervalModifierId = "SK17_AttackInterval";

    [Header("Pixie Blessing")]
    [SerializeField] private float attackIntervalReduction = 0.35f;
    [SerializeField] private float duration = 6f;
    [SerializeField] private float shieldMultiplier = 0.5f;

    [NonSerialized] private Hurtbox activationTarget;
    [NonSerialized] private CountdownTimer blessingTimer;

    protected override bool InterruptsNormalAttack => false;

    private bool IsBlessingActive => blessingTimer != null && blessingTimer.IsRunning && !blessingTimer.IsFinished;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        blessingTimer = null;
        Owner.NormalAttackController.OnNormalAttackHitResolved += HandleNormalAttackHitResolved;
    }

    public override void Tick(float deltaTime)
    {
        if (IsBlessingActive)
        {
            blessingTimer.Tick(deltaTime);
        }

        base.Tick(deltaTime);
    }

    public override bool CanActivate()
    {
        if (!CanCastSkill || Owner.NormalAttackController == null)
        {
            return false;
        }

        return Owner.NormalAttackController.TrySelectTarget
        (
            Owner.ResolvedAttackPattern,
            TargetSide.Ally,
            AttackEffect.Heal,
            out activationTarget
        );
    }

    public override void Activate()
    {
        UnitStatModifier[] modifiers =
        {
            new UnitStatModifier(UnitStatType.AttackInterval, UnitStatModifierType.AdditivePercent,
                                 -Mathf.Abs(attackIntervalReduction), AttackIntervalModifierId)
        };

        float resolvedDuration = Mathf.Max(0f, duration);
        Owner.ApplyTemporaryStatModifiers(BlessingStatusId, Owner.gameObject, modifiers, resolvedDuration);

        blessingTimer = new CountdownTimer(resolvedDuration);
        blessingTimer.StartTimer();
        activationTarget = null;
        FinishSkill();
    }

    public override void ClearData()
    {
        if (Owner != null && Owner.NormalAttackController != null)
        {
            Owner.NormalAttackController.OnNormalAttackHitResolved -= HandleNormalAttackHitResolved;
        }

        if (blessingTimer != null)
        {
            blessingTimer.StopTimer();
            blessingTimer = null;
        }

        activationTarget = null;
        base.ClearData();
    }

    private void HandleNormalAttackHitResolved(int attackId, HitData hitData, HitResult hitResult)
    {
        if (!IsBlessingActive || hitData.Effect != AttackEffect.Heal || hitResult.HealthRestored <= 0f || hitData.TargetHurtbox == null)
        {
            return;
        }

        UnitRuntime healedTarget = hitData.TargetHurtbox.OwnerRuntime;
        if (healedTarget == null || healedTarget.IsDead || healedTarget.Shield == null)
        {
            return;
        }

        float shieldValue = DamageCalculator.CalculateBaseEffectValue(hitResult.HealthRestored, Mathf.Max(0f, shieldMultiplier));
        healedTarget.Shield.AddShield(shieldValue);
    }
}
