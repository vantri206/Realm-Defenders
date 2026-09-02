using UnityEngine;

public class HeroCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroCardView heroCardView;
    [SerializeField] private HeroCardInput heroCardInput;

    public HeroCardView CardView => heroCardView;
    public HeroCardInput CardInput => heroCardInput;

    private HeroCombatState combatState;

    public HeroCombatState CombatState => combatState;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        if(combatState == null || !combatState.IsValid)
        {
            return;
        }

        if (combatState.DeployState == HeroDeployState.Countdown)
        {
            if (heroCardView == null)
            {
                Debug.LogError("[HeroCard] HeroCardView is required to tick countdown UI.", this);
                return;
            }

            heroCardView.Tick();
        }
    }

    public void Initialize(HeroCombatState combatState)
    {
        CacheReferences();

        if (combatState == null || !combatState.IsValid)
        {
            Debug.LogWarning("[HeroCard] Invalid hero combat state. Cannot bind data.");
            return;
        }

        this.combatState = combatState;

        if (heroCardView == null)
        {
            Debug.LogError("[HeroCard] HeroCardView is required to initialize a hero card.", this);
            return;
        }

        heroCardView.SetData(combatState);

        if (heroCardInput == null)
        {
            Debug.LogError("[HeroCard] HeroCardInput is required to initialize a hero card.", this);
            return;
        }

        heroCardInput.Initialize(this);
        OnHeroDeployStateChanged(combatState.DeployState);
    }

    public void Clear()
    {
        combatState = null;

        if (heroCardView != null)
        {
            heroCardView.Clear();
        }

        if (heroCardInput != null)
        {
            heroCardInput.SetInputEnabled(false);
        }
    }

    public void OnHeroDeployStateChanged(HeroDeployState newState)
    {
        if (heroCardView != null)
        {
            heroCardView.SetState(newState);
        }

        if (heroCardInput != null)
        {
            heroCardInput.SetState(newState);
        }
    }

    private void CacheReferences()
    {
        if (heroCardView == null)
        {
            heroCardView = GetComponent<HeroCardView>();
        }

        if (heroCardInput == null)
        {
            heroCardInput = GetComponent<HeroCardInput>();
        }
    }
}
