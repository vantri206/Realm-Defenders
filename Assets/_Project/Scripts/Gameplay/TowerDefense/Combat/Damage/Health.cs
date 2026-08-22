using System;
using UnityEngine;

public readonly struct HealthData
{
    public float CurrentHealth { get; }
    public float MaxHealth { get; }

    public HealthData(float currentHealth, float maxHealth)
    {
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
    }
}

public class Health : MonoBehaviour, IDamageable
{
    // Defensive stats
    private UnitStats stats;

    private float currentHealth = 0f;

    public event Action<HealthData> OnHealthChanged;

    public event Action<float> OnDamaged;
    public event Action<float> OnHealed;
    public event Action OnDied;

    public bool IsDead { get; private set; } = false;

    public HealthData CurrentData
    {
        get
        {
            if (!isInitialized)
            {
                return new HealthData(MaxHealth, MaxHealth);
            }

            return new HealthData(CurrentHealth, MaxHealth);
        }
    }

    public float CurrentHealth => CurrentData.CurrentHealth;
    public float MaxHealth => stats.MaxHealth;
    public float Defense => stats.Defense;
    public float SpecialDefense => stats.SpecialDefense;

    private bool isInitialized;

    public void Initialize(UnitStats stats)
    {
        if (stats == null)
        {
            Debug.LogError("[Health] CombatStats cannot be null.");
            return;
        }

        this.stats = stats;
        RefreshHealth(false);
    }

    private void OnEnable()
    {
        RefreshHealth(false);
    }

    public HitResult TakeDamage(DamageRequest request)
    {
        if (IsDead || request.BaseDamage <= 0f)
        {
            return default;
        }

        float defensiveStat = GetDefensiveStat(request.DamageType);
        float finalDamage = DamageCalculator.CalculateDamageTaken(request.BaseDamage, defensiveStat);

        return ApplyFinalDamage(finalDamage);
    }

    private float GetDefensiveStat(AttackDamageType damageType)
    {
        return damageType switch
        {
            AttackDamageType.PhysicalDamage => Defense,
            AttackDamageType.MagicalDamage => SpecialDefense,
            AttackDamageType.TrueDamage => 0f,
            _ => 0f
        };
    }

    private HitResult ApplyFinalDamage(float damage)
    {
        if (IsDead || damage <= 0f)
        {
            return default;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        OnDamaged?.Invoke(damage);

        NotifyHealthChanged();

        if (currentHealth <= 0)
        {
            Die();
        }

        return new HitResult(damage, damage > 0f && IsDead);
    }

    public HitResult Heal(HealRequest request)
    {
        if (IsDead || currentHealth >= MaxHealth || request.BaseHeal <= 0f)
        {
            return default;
        }

        currentHealth = Mathf.Min(MaxHealth, currentHealth + request.BaseHeal);

        OnHealed?.Invoke(request.BaseHeal);

        NotifyHealthChanged();
        return new HitResult(AttackEffect.Heal, request.BaseHeal, false);
    }

    public void Heal(float healAmount, Vector3 hitPosition)
    {
        Heal(new HealRequest(null, this, healAmount, hitPosition));
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnDied?.Invoke();
    }

    public void OnSpawn()
    {
        RefreshHealth(true);
    }

    public void RefreshHealth(bool isNotify)
    {
        currentHealth = MaxHealth;
        IsDead = false;
        isInitialized = true;

        if (isNotify)
        {
            NotifyHealthChanged();
        }
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(CurrentData);
    }

#if UNITY_EDITOR
    [ContextMenu("Take 20% Damage")]
    private void ContextTakeTwentyPercentDamage()
    {
        DamageSystem.ApplyDamage(new DamageRequest(null, this, MaxHealth * 0.2f, AttackDamageType.TrueDamage, transform.position));
    }
#endif
}
