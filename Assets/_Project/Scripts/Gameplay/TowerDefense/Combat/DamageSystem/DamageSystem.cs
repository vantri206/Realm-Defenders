using UnityEngine;

public static class DamageSystem
{
    public static DamageResult ApplyDamage(DamageRequest request)
    {
        if (request.Target == null || request.Target.IsDead)
        {
            return default;
        }

        float damage = Mathf.Max(0f, request.Damage);
        if (damage <= 0f)
        {
            return default;
        }

        float damageTaken = request.Target.ApplyDamage(damage, request.HitPosition, request.Source);
        bool isLastHit = damageTaken > 0f && request.Target.IsDead;

        return new DamageResult(damageTaken, isLastHit);
    }
}
