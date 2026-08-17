public interface IDamageable
{
    bool IsDead { get; }
    float CurrentHealth { get; }
    float MaxHealth { get; }

    HitResult TakeDamage(DamageRequest request);
    HitResult Heal(HealRequest request);
}
