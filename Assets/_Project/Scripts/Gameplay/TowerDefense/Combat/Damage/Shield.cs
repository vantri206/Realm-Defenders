using System;
using UnityEngine;

public class Shield : MonoBehaviour
{
    private UnitStats stats;
    private float currentShield;

    public float CurrentShield => currentShield;

    public event Action<float> OnShieldValueChanged;

    public void Initialize(UnitStats stats)
    {
        if (this.stats != null)
        {
            this.stats.OnStatsChanged -= HandleStatsChanged;
        }

        this.stats = stats;
        if (this.stats != null)
        {
            this.stats.OnStatsChanged += HandleStatsChanged;
        }

        ClampToMaxHealth();
    }

    public void AddShield(float value)
    {
        if (value <= 0f)
        {
            return;
        }

        float maxShield = stats != null ? stats.MaxHealth : 0f;
        currentShield = Mathf.Min(maxShield, currentShield + value);
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

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnStatsChanged -= HandleStatsChanged;
        }
    }

    private void HandleStatsChanged()
    {
        ClampToMaxHealth();
    }

    private void ClampToMaxHealth()
    {
        float maxShield = stats != null ? stats.MaxHealth : 0f;
        float clampedShield = Mathf.Min(currentShield, maxShield);
        if (Mathf.Approximately(clampedShield, currentShield))
        {
            return;
        }

        currentShield = clampedShield;
        OnShieldValueChanged?.Invoke(currentShield);
    }
}
