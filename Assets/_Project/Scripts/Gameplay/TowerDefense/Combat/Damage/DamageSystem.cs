public static class DamageSystem
{
    public static HitResult ApplyDamage(DamageRequest request)
    {
        if (request.Target == null || request.Target.IsDead)
        {
            return default;
        }

        if (request.BaseDamage <= 0f)
        {
            return default;
        }

        return request.Target.TakeDamage(request);
    }

    public static HitResult ApplyHeal(HealRequest request)
    {
        if (request.Target == null || request.Target.IsDead)
        {
            return default;
        }

        if (request.BaseHeal <= 0f || request.Target.CurrentHealth >= request.Target.MaxHealth)
        {
            return default;
        }

        return request.Target.Heal(request);
    }
}
