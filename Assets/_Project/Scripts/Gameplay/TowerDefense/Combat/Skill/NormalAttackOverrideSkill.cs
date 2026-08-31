using System;

[Serializable]
public abstract class NormalAttackOverrideSkill : AutoActiveSkill
{
    public override bool CanActivate()
    {
        if (!CanCastSkill || Owner.NormalAttackController == null)
        {
            return false;
        }

        return CanUseOverrideAttack(out Hurtbox target) && target != null;
    }

    public override void Activate()
    {
        if (!TryUseOverrideAttack(out Hurtbox target))
        {
            FinishSkill();
            return;
        }

        Owner.FacePosition(target.AimPosition);
        Owner.TriggerSkillAttackAnimation();

        if (!ExecuteOverrideAttack(target))
        {
            FinishSkill();
        }
    }

    protected virtual bool CanUseOverrideAttack(out Hurtbox target)
    {
        target = null;
        return Owner != null && Owner.NormalAttackController != null &&
               Owner.NormalAttackController.CanUseOverrideAttack(Owner.ResolvedAttackPattern, out target);
    }

    protected virtual bool TryUseOverrideAttack(out Hurtbox target)
    {
        target = null;
        return Owner != null && Owner.NormalAttackController != null && Owner.NormalAttackController.TryUseOverrideAttack(Owner.ResolvedAttackPattern, out target);
    }

    protected abstract bool ExecuteOverrideAttack(Hurtbox target);

    protected void FinishOverrideAttack()
    {
        FinishSkill();
    }

}
