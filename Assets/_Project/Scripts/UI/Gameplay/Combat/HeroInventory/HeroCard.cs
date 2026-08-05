using UnityEngine;

public class HeroCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroCardView heroCardView;
    [SerializeField] private HeroCardInput heroCardInput;

    public HeroCardView CardView => heroCardView;
    public HeroCardInput CardInput => heroCardInput;
    
    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(HeroInstance heroInstance)
    {
        CacheReferences();

        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogWarning("[HeroInventoryView] Invalid hero instance. Cannot bind data.");
            return;
        }

        if (heroCardView != null)
        {
            heroCardView.SetData(heroInstance);
        }

        if (heroCardInput != null)
        {
            heroCardInput.Initialize(heroInstance);
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
