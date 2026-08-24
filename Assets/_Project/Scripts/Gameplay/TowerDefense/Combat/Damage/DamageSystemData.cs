using UnityEngine;

public readonly struct DamageRequest
{
    public GameObject Attacker { get; }
    public IDamageable Target { get; }
    public float BaseDamage { get; }
    public AttackDamageType DamageType { get; }
    public Vector3 HitPosition { get; }

    public DamageRequest(GameObject attacker, IDamageable target, float baseDamage, AttackDamageType damageType, Vector3 hitPosition)
    {
        Attacker = attacker;
        Target = target;
        BaseDamage = baseDamage;
        DamageType = damageType;
        HitPosition = hitPosition;
    }
}

public readonly struct HealRequest
{
    public GameObject Healer { get; }
    public IDamageable Target { get; }
    public float BaseHeal { get; }
    public Vector3 HitPosition { get; }

    public HealRequest(GameObject healer, IDamageable target, float baseHeal, Vector3 hitPosition)
    {
        Healer = healer;
        Target = target;
        BaseHeal = baseHeal;
        HitPosition = hitPosition;
    }
}

public readonly struct HitResult
{
    public AttackEffect Effect { get; }
    public float AppliedValue { get; }
    public float DamageTaken => Effect == AttackEffect.Damage ? AppliedValue : 0f;
    public float HealthRestored => Effect == AttackEffect.Heal ? AppliedValue : 0f;
    public bool IsLastHit { get; }

    public HitResult(float damageTaken, bool isLastHit)
        : this(AttackEffect.Damage, damageTaken, isLastHit)
    {
    }

    public HitResult(AttackEffect effect, float appliedValue, bool isLastHit)
    {
        Effect = effect;
        AppliedValue = appliedValue;
        IsLastHit = isLastHit;
    }
}

public readonly struct HitData
{
    public GameObject Attacker { get; }
    public Hurtbox TargetHurtbox { get; }
    public TeamIdentity AttackerTeam { get; }
    public TargetSide TargetSide { get; }
    public AttackEffect Effect { get; }
    public UnitAttackType AttackType { get; }
    public float RawEffectValue { get; }
    public AttackDamageType DamageType { get; }
    public Vector3 HitPosition { get; }

    public HitData(GameObject attacker, Hurtbox targetHurtbox, TeamIdentity attackerTeam, TargetSide targetSide,
                   AttackEffect effect, UnitAttackType attackType, float rawEffectValue,
                   AttackDamageType damageType, Vector3 hitPosition)
    {
        Attacker = attacker;
        TargetHurtbox = targetHurtbox;
        AttackerTeam = attackerTeam;
        TargetSide = targetSide;
        Effect = effect;
        AttackType = attackType;
        RawEffectValue = rawEffectValue;
        DamageType = damageType;
        HitPosition = hitPosition;
    }
}
