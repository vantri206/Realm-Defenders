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
    private float defense = 0f;
    private float specialDefense = 0f;

    private float maxHealth = 100f;
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
                return new HealthData(maxHealth, maxHealth);
            }

            return new HealthData(currentHealth, maxHealth);
        }
    }

    public float MaxHealth => CurrentData.MaxHealth;
    public float CurrentHealth => CurrentData.CurrentHealth;
    public float Defense => defense;
    public float SpecialDefense => specialDefense;

    private bool isInitialized;

    public void Initialize(float maxHealth)
    {
        this.maxHealth = Mathf.Max(0f, maxHealth);
        defense = 0f;
        specialDefense = 0f;
        RefreshHealth(false);
    }

    public void Initialize(float maxHealth, float defense, float specialDefense)
    {
        this.maxHealth = Mathf.Max(0f, maxHealth);
        this.defense = Mathf.Max(0f, defense);
        this.specialDefense = Mathf.Max(0f, specialDefense);
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
            AttackDamageType.PhysicalDamage => defense,
            AttackDamageType.MagicalDamage => specialDefense,
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
        if (IsDead || currentHealth >= maxHealth || request.BaseHeal <= 0f)
        {
            return default;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + request.BaseHeal);

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
        currentHealth = maxHealth;
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
    private void OnValidate()
    {
        defense = Mathf.Max(0f, defense);
        specialDefense = Mathf.Max(0f, specialDefense);
    }
    [ContextMenu("Take 20% Damage")]
    private void ContextTakeTwentyPercentDamage()
    {
        DamageSystem.ApplyDamage(new DamageRequest(null, this, maxHealth * 0.2f, AttackDamageType.TrueDamage, transform.position));
    }
#endif
}
