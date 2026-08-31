using System;
using UnityEngine;

public class Shield : MonoBehaviour
{
    private float currentShield;

    public float CurrentShield => currentShield;

    public event Action<float> OnShieldValueChanged;

    public void AddShield(float value)
    {
        if (value <= 0f)
        {
            return;
        }

        currentShield += value;
        OnShieldValueChanged?.Invoke(currentShield);
    }

    public float AbsorbDamage(float damage)
    {
        damage = Mathf.Max(0f, damage);
        if (damage <= 0f || currentShield <= 0f)
        {
            return damage;
        }

        float absorbedDamage = Mathf.Min(currentShield, damage);
        currentShield -= absorbedDamage;
        OnShieldValueChanged?.Invoke(currentShield);

        return damage - absorbedDamage;
    }

    public void Clear()
    {
        if (currentShield <= 0f)
        {
            return;
        }

        currentShield = 0f;
        OnShieldValueChanged?.Invoke(currentShield);
    }
}
