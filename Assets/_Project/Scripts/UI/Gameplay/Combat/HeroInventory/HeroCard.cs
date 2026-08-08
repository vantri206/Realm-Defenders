using UnityEngine;

public class HeroCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroCardView heroCardView;
    [SerializeField] private HeroCardInput heroCardInput;

    public HeroCardView CardView => heroCardView;
    public HeroCardInput CardInput => heroCardInput;

    private HeroInstance heroInstance;

    public HeroInstance HeroInstance => heroInstance;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        if(heroInstance == null || !heroInstance.IsValid)
        {
            return;
        }

        if (heroInstance.DeployState == HeroDeployState.Countdown)
        {
            heroCardView.Tick(Time.deltaTime);
        }
    }

    public void Initialize(HeroInstance heroInstance)
    {
        CacheReferences();

        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogWarning("[HeroInventoryView] Invalid hero instance. Cannot bind data.");
            return;
        }

        this.heroInstance = heroInstance;

        if (heroCardView != null)
        {
            heroCardView.SetData(heroInstance);
        }

        if (heroCardInput != null)
        {
            heroCardInput.Initialize(heroInstance);
        }

        OnHeroDeployStateChanged(heroInstance, heroInstance.DeployState);
    }

    public void Clear()
    {
        if (heroCardView != null)
        {
            heroCardView.Clear();
        }

        if (heroCardInput != null)
        {
            heroCardInput.SetInputEnabled(false);
        }
    }

    public void OnHeroDeployStateChanged(HeroInstance heroInstance, HeroDeployState newState)
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
