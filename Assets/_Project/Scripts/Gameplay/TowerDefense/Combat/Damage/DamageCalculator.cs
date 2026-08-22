using UnityEngine;

public static class DamageCalculator
{
    public static float CalculateBaseEffectValue(float attributeValue, float effectMultiplier)
    {
        return Mathf.Max(0f, attributeValue * effectMultiplier);
    }

    public static float CalculateBaseDamage(float offensiveStat, float damageMultiplier)
    {
        return CalculateBaseEffectValue(offensiveStat, damageMultiplier);
    }

    public static float CalculateDamageTaken(float baseDamage, float defensiveStat)
    {
        baseDamage = Mathf.Max(0f, baseDamage);

        if (baseDamage <= 0f)
        {
            return 0f;
        }

        defensiveStat = Mathf.Max(0f, defensiveStat);
        
        float finalDamage = baseDamage * 100f / (100f + defensiveStat);
        return Mathf.Max(1f, finalDamage);
    }

    public static float CalculateDamage(float offensiveStat, float damageMultiplier)
    {
        return CalculateBaseDamage(offensiveStat, damageMultiplier);
    }

    public static float GetFinalDamage(float offensiveStat, float defensiveStat, float damageMultiplier)
    {
        float baseDamage = CalculateBaseDamage(offensiveStat, damageMultiplier);
        return CalculateDamageTaken(baseDamage, defensiveStat);
    }
}
