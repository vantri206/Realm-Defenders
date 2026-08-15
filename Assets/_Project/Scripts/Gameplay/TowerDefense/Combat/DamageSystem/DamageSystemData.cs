using UnityEngine;

public readonly struct DamageRequest
{
    public GameObject Source { get; }
    public IDamageable Target { get; }
    public float BaseDamage { get; }
    public AttackDamageType DamageType { get; }
    public Vector3 HitPosition { get; }

    public DamageRequest(GameObject source, IDamageable target, float baseDamage, AttackDamageType damageType, Vector3 hitPosition)
    {
        Source = source;
        Target = target;
        BaseDamage = baseDamage;
        DamageType = damageType;
        HitPosition = hitPosition;
    }

    public DamageRequest(GameObject source, IDamageable target, float baseDamage, Vector3 hitPosition) 
        : this(source, target, baseDamage, AttackDamageType.PhysicalDamage, hitPosition)
    {
        
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

public readonly struct HitData
{
    public GameObject Source { get; }
    public Hurtbox TargetHurtbox { get; }
    public TeamIdentity SourceTeam { get; }
    public float BaseDamage { get; }
    public UnitAttackType AttackType { get; }
    public AttackDamageType DamageType { get; }
    public Vector3 HitPosition { get; }
}
