public static class DamageSystem
{
    public static DamageResult ApplyDamage(DamageRequest request)
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
}
