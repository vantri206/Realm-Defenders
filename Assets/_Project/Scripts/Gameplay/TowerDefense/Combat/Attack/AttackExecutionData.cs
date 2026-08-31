using UnityEngine;

public readonly struct AttackExecutionData
{
    public GameObject Attacker { get; }
    public TeamIdentity AttackerTeam { get; }
    public TargetSide TargetSide { get; }
    public AttackEffect AttackEffect { get; }
    public UnitAttackType AttackType { get; }
    public float RawEffectValue { get; }
    public AttackDamageType DamageType { get; }

    public AttackExecutionData(GameObject attacker, TeamIdentity attackerTeam, TargetSide targetSide, AttackEffect attackEffect,
                               UnitAttackType attackType, float rawEffectValue, AttackDamageType damageType)
    {
        Attacker = attacker;
        AttackerTeam = attackerTeam;
        TargetSide = targetSide;
        AttackEffect = attackEffect;
        AttackType = attackType;
        RawEffectValue = rawEffectValue;
        DamageType = damageType;
    }
}

public readonly struct AttackVFXData
{
    public SimpleSpriteAnimatorVFX HitVFX { get; }
    public ParticleVFX HealVFX { get; }

    public AttackVFXData(SimpleSpriteAnimatorVFX hitVFX, ParticleVFX healVFX)
    {
        HitVFX = hitVFX;
        HealVFX = healVFX;
    }

    public AttackVFXData(SimpleSpriteAnimatorVFX hitVFX)
    {
        HitVFX = hitVFX;
        HealVFX = null;
    }

    public AttackVFXData(ParticleVFX healVFX)
    {
        HitVFX = null;
        HealVFX = healVFX;
    }
}
