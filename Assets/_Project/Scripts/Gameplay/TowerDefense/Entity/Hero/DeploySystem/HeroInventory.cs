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
        if (inventoryView == null)
        {
            Debug.LogError("[HeroInventory] HeroInventoryView is required to initialize inventory.", this);
            return;
        }

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
        if (heroInventoryView == null)
        {
            Debug.LogError("[HeroInventory] HeroInventoryView is required before adding hero cards.", this);
            return false;
        }

        HeroCard heroCard = heroInventoryView.AddHeroCard(heroInstance);
        if (heroCard == null)
        {
            Debug.LogWarning("[HeroInventory] Failed to create a HeroCard for the added HeroInstance.");
            return false;
        }
        
        heroInstance.OnDeployStateChanged += OnHeroDeployStateChanged;

        return true;
    }

    public bool RemoveHeroInstance(HeroInstance heroInstance)
    {
        if (heroInstance == null)
        {
            Debug.LogWarning("[HeroInventory] Attempted to remove a null HeroInstance from the inventory.");
            return false;
        }

        if (heroInventoryView == null)
        {
            Debug.LogError("[HeroInventory] HeroInventoryView is required before removing hero cards.", this);
            return false;
        }

        heroInventoryView.RemoveHeroCard(heroInstance);
        heroInstance.OnDeployStateChanged -= OnHeroDeployStateChanged;
        heroInstances.Remove(heroInstance);
        return true;
    }

    private void OnHeroDeployStateChanged(HeroInstance heroInstance, HeroDeployState newState)
    {
        if (heroInventoryView == null)
        {
            Debug.LogError("[HeroInventory] HeroInventoryView is required to update hero card deploy state.", this);
            return;
        }

        if (newState == HeroDeployState.Deployed)
        {
            heroInventoryView.RemoveHeroCard(heroInstances.FirstOrDefault(hero => hero == heroInstance));
        }
        else
        {
            if (!heroInventoryView.HeroCards.Any(card => card.HeroInstance == heroInstance))
            {
                heroInventoryView.AddHeroCard(heroInstance);
            }

            HeroCard heroCard = heroInventoryView.HeroCards.FirstOrDefault(card => card.HeroInstance == heroInstance);
            if (heroCard != null)
            {
                heroCard.OnHeroDeployStateChanged(newState);
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
