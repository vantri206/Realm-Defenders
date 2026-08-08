using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeroInventory : MonoBehaviour
{
    [SerializeField] private List<HeroDefinition> initialHeroes = new List<HeroDefinition>();

    private List<HeroInstance> heroInstances = new List<HeroInstance>();
    private HeroInventoryView heroInventoryView;

    public IReadOnlyList<HeroInstance> HeroInstances => heroInstances;
    public int HeroCount => heroInstances.Count;
    
    public void Initialize(HeroInventoryView inventoryView)
    {
        heroInventoryView = inventoryView;

        heroInventoryView.Initialize();

        for (int i = 0; i < initialHeroes.Count; i++)
        {
            HeroInstance heroInstance = CreateHeroInstance(initialHeroes[i]);
            if (heroInstance != null && heroInstance.IsValid)
            {
                AddHeroInstance(heroInstance);
            }
        }
    }

    public HeroInstance CreateHeroInstance(HeroDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogWarning("Attempted to create a HeroInstance with a null definition.");
            return null;
        }

        HeroInstance heroInstance = new HeroInstance();
        heroInstance.Initialize(definition);
        return heroInstance;
    }

    public bool AddHeroInstance(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogWarning("[HeroInventory] Attempted to add a null HeroInstance to the inventory.");
            return false;
        }

        heroInstances.Add(heroInstance);
        HeroCard heroCard = heroInventoryView?.AddHeroCard(heroInstance);
        if (heroCard == null)
        {
            Debug.LogWarning("[HeroInventory] Failed to create a HeroCard for the added HeroInstance.");
            return false;
        }
        
        heroInstance.OnDeployStateChanged += OnHeroDeployStateChanged;
        heroInstance.OnDeployStateChanged += heroCard.OnHeroDeployStateChanged;

        return true;
    }

    public bool RemoveHeroInstance(HeroInstance heroInstance)
    {
        if (heroInstance == null)
        {
            Debug.LogWarning("[HeroInventory] Attempted to remove a null HeroInstance from the inventory.");
            return false;
        }

        heroInventoryView?.RemoveHeroCard(heroInstance);
        heroInstance.OnDeployStateChanged -= OnHeroDeployStateChanged;
        heroInstances.Remove(heroInstance);
        return true;
    }

    private void OnHeroDeployStateChanged(HeroInstance heroInstance, HeroDeployState newState)
    {
        if (newState == HeroDeployState.Deployed)
        {
            heroInventoryView?.RemoveHeroCard(heroInstances.FirstOrDefault(hero => hero == heroInstance));
        }
        else
        {
            if (!heroInventoryView.HeroCards.Any(card => card.HeroInstance == heroInstance))
            {
                heroInventoryView?.AddHeroCard(heroInstance);
            }
        }
    }

    public bool HasHeroDefinition(HeroDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        return heroInstances.Any(heroInstance => heroInstance != null && heroInstance.Definition == definition);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        heroInstances.RemoveAll(heroInstance => heroInstance == null);
    }
#endif
}
