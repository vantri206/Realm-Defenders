public interface IDamageable
{
    bool IsDead { get; }

    DamageResult TakeDamage(DamageRequest request);
}
