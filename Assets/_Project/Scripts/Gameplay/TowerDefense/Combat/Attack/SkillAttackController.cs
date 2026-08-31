using System;

public class SkillAttackController
{
    public event Action<HitData, HitResult> OnSkillAttackHitResolved;

    public bool TryProcessHit(HitData hitData, out HitResult hitResult)
    {
        if (!HitProcessor.TryProcessHit(hitData, out hitResult))
        {
            return false;
        }

        NotifySkillAttackHitResolved(hitData, hitResult);
        return true;
    }

    public void NotifySkillAttackHitResolved(HitData hitData, HitResult hitResult)
    {
        if (hitResult.AppliedValue > 0f)
        {
            OnSkillAttackHitResolved?.Invoke(hitData, hitResult);
        }
    }

    public void Clear()
    {
        OnSkillAttackHitResolved = null;
    }
}
