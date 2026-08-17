public static class HitProcessor
{
    public static bool TryProcessHit(in HitData hitData, out HitResult hitResult)
    {
        hitResult = new HitResult(0f, false);

        if (!CanProcessHit(hitData, out IDamageable damageable))
        {
            return false;
        }

        switch (hitData.Effect)
        {
            case AttackEffect.Damage:
                DamageRequest damageRequest = new DamageRequest
                (
                    hitData.Attacker,
                    damageable,
                    hitData.BaseEffectValue,
                    hitData.DamageType,
                    hitData.HitPosition
                );

                hitResult = DamageSystem.ApplyDamage(damageRequest);
                break;

            case AttackEffect.Heal:
                HealRequest healRequest = new HealRequest
                (
                    hitData.Attacker,
                    damageable,
                    hitData.BaseEffectValue,
                    hitData.HitPosition
                );

                hitResult = DamageSystem.ApplyHeal(healRequest);
                break;

            default:
                return false;
        }

        return hitResult.AppliedValue > 0f;
    }

    private static bool CanProcessHit(in HitData hitData, out IDamageable damageable)
    {
        damageable = null;

        if (hitData.TargetHurtbox == null || hitData.AttackerTeam == null || hitData.BaseEffectValue <= 0f)
        {
            return false;
        }

        if (!AttackTargetRulling.CanHit(hitData.AttackType, hitData.TargetHurtbox))
        {
            return false;
        }

        TeamIdentity targetTeam = hitData.TargetHurtbox.GetTargetTeam();
        if (targetTeam == null || !hitData.AttackerTeam.IsTargetSide(targetTeam, hitData.TargetSide))
        {
            return false;
        }

        damageable = hitData.TargetHurtbox.GetDamageable();
        if (damageable == null || damageable.IsDead)
        {
            damageable = null;
            return false;
        }

        if (hitData.Effect == AttackEffect.Heal && damageable.CurrentHealth >= damageable.MaxHealth)
        {
            damageable = null;
            return false;
        }

        return true;
    }
}
