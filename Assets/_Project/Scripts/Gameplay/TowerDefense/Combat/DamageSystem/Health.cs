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
    private float maxHealth = 100f;
    private float currentHealth = 0f;

    public event Action<HealthData, HealthData> OnHealthChanged;

    public event Action<float> OnDamaged;
    public event Action<float> OnHealed;
    public event Action OnDied;

    public bool IsDead { get; private set; } = false;

    public HealthData PreviousData { get; private set; }

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

    private bool isInitialized;

    public void Initialize(float maxHealth)
    {
        this.maxHealth = Mathf.Max(0f, maxHealth);
        RefreshHealth(false);
    }

    private void OnEnable()
    {
        RefreshHealth(false);
    }

    public float ApplyDamage(float damage, Vector3 hitPosition, GameObject source)
    {
        if (IsDead || damage <= 0f) return 0f;

        PreviousData = CurrentData;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        OnDamaged?.Invoke(damage);

        NotifyHealthChanged();

        if (currentHealth <= 0)
        {
            Die();
        }

        return damage;
    }

    public void Heal(float healAmount, Vector3 hitPosition)
    {
        if (IsDead || currentHealth >= maxHealth || healAmount <= 0f) return;

        PreviousData = CurrentData;

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        OnHealed?.Invoke(healAmount);

        NotifyHealthChanged();
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
        PreviousData = CurrentData;

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
        OnHealthChanged?.Invoke(PreviousData, CurrentData);
    }

    [ContextMenu("Take 20% Damage")]
    private void ContextTakeTwentyPercentDamage()
    {
        DamageSystem.ApplyDamage(new DamageRequest(null, this, maxHealth * 0.2f, transform.position));
    }
}
