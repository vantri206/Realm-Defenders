using UnityEngine;

public interface IDamageable
{
    public bool IsDead { get; }

    public float ApplyDamage(float damage, Vector3 hitPosition, GameObject source);
}
