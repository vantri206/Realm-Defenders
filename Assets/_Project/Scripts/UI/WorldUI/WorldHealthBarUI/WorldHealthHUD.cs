using UnityEngine;

public class WorldHealthHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private WorldHealthBar healthBar;

    private bool isInitialized;

    private bool isHealthEventSubscribed;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        RegisterHealthEvents();
        RefreshHealth();
    }

    private void OnDisable()
    {
        UnregisterHealthEvents();
    }

    public void Initialize()
    {
        CacheReferences();

        if (healthBar != null)
        {
            healthBar.Initialize();
        }
        else
        {
            Debug.LogError("[WorldHealthHUD] WorldHealthBar is required to initialize health HUD.", this);
        }

        isInitialized = true;

        RegisterHealthEvents();
        RefreshHealth();
    }

    public void Initialize(Health health)
    {
        if (this.health != health)
        {
            UnregisterHealthEvents();
            this.health = health;
        }

        Initialize();

        if (isActiveAndEnabled)
        {
            RegisterHealthEvents();
            RefreshHealth();
        }
    }

    private void CacheReferences()
    {
        if (healthBar == null)
        {
            healthBar = GetComponent<WorldHealthBar>();
        }

        if (health == null)
        {
            health = GetComponentInParent<Health>();
        }
    }

    public void SetTargetHealth(Health health)
    {
        if (this.health == health)
        {
            return;
        }

        UnregisterHealthEvents();
        this.health = health;
        RegisterHealthEvents();
        RefreshHealth();
    }

    private void RefreshHealth()
    {
        if (health == null || healthBar == null)
        {
            if (healthBar == null)
            {
                Debug.LogError("[WorldHealthHUD] WorldHealthBar is required to refresh health HUD.", this);
            }

            return;
        }

        HealthData currentData = health.CurrentData;

        healthBar.SetValue(currentData.CurrentHealth, currentData.MaxHealth);

        if (health.IsDead)
        {
            healthBar.SetDead();
        }
    }

    private void RegisterHealthEvents()
    {
        if (isHealthEventSubscribed || !isActiveAndEnabled)
        {
            return;
        }

        if (health != null)
        {
            health.OnHealthChanged += OnHealthChanged;
            health.OnDied += OnDied;

            isHealthEventSubscribed = true;
        }
    }

    private void UnregisterHealthEvents()
    {
        if (!isHealthEventSubscribed)
        {
            return;
        }   

        if (health != null)
        {
            health.OnHealthChanged -= OnHealthChanged;
            health.OnDied -= OnDied;
        }

        isHealthEventSubscribed = false;
    }

    private void OnHealthChanged(HealthData currentData)
    {
        if (healthBar == null)
        {
            Debug.LogError("[WorldHealthHUD] WorldHealthBar is required to handle health changed events.", this);
            return;
        }

        healthBar.SetValue(currentData.CurrentHealth, currentData.MaxHealth);
    }

    private void OnDied()
    {
        if (healthBar == null)
        {
            Debug.LogError("[WorldHealthHUD] WorldHealthBar is required to handle death events.", this);
            return;
        }

        healthBar.SetDead();
    }
}
