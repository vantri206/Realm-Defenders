using System;
using System.Collections.Generic;

[Serializable]
public abstract class NormalAttackOverrideSkill : AutoActiveSkill
{
    [NonSerialized] private List<Hurtbox> targets;

    protected List<Hurtbox> OverrideTargets => targets;
    protected virtual int MaxTargetCount => 1;
    protected virtual TargetSide OverrideTargetSide => Owner.NormalAttackDefinition.TargetSide;
    protected virtual AttackEffect OverrideAttackEffect => Owner.NormalAttackDefinition.AttackEffect;
    protected override bool InterruptsNormalAttack => false;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        targets = new List<Hurtbox>();
    }

    public override bool CanActivate()
    {
        if (!CanCastSkill || Owner.CurrentState != UnitRuntimeState.Idle || Owner.NormalAttackController == null)
        {
            return false;
        }

        return Owner.NormalAttackController.TrySetupOverrideAttack
        (
            Owner.ResolvedAttackPattern,
            OverrideTargetSide,
            OverrideAttackEffect,
            MaxTargetCount,
            targets
        );
    }

    public override void Activate()
    {
        if (targets == null || targets.Count == 0)
        {
            FinishSkill();
            return;
        }

        if (!Owner.NormalAttackController.TryConsumeOverrideAttack())
        {
            FinishSkill();
            return;
        }

        Hurtbox target = targets[0];
        Owner.FacePosition(target.AimPosition);
        // Owner.TriggerSkillAttackAnimation();

        if (!ExecuteOverrideAttack())
        {
            FinishSkill();
        }
    }

    public override void ClearData()
    {
        if (targets != null)
        {
            targets.Clear();
            targets = null;
        }

        base.ClearData();
    }

    protected abstract bool ExecuteOverrideAttack();

    protected void FinishOverrideAttack()
    {
        FinishSkill();
    }

}
