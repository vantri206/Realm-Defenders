using UnityEngine;

public readonly struct DamageRequest
{
    public GameObject Source { get; }
    public IDamageable Target { get; }
    public float Damage { get; }
    public Vector3 HitPosition { get; }

    public DamageRequest(GameObject source, IDamageable target, float damage, Vector3 hitPosition)
    {
        Source = source;
        Target = target;
        Damage = damage;
        HitPosition = hitPosition;
    }
}

public readonly struct DamageResult
{
    public float DamageTaken { get; }
    public bool IsLastHit { get; }

    public DamageResult(float damageTaken, bool isLastHit)
    {
        DamageTaken = damageTaken;
        IsLastHit = isLastHit;
    }
}