using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeroInventory : MonoBehaviour
{
    [SerializeField] private List<HeroInstance> heroInstances = new List<HeroInstance>();
    
    public IReadOnlyList<HeroInstance> HeroInstances => heroInstances;
    public int Count => heroInstances.Count;

    public HeroInstance CreateHeroInstance(HeroDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogWarning("Attempted to create a HeroInstance with a null definition.");
            return null;
        }

        HeroInstance heroInstance = new HeroInstance();
        heroInstance.Initialize(definition);
        AddHeroInstance(heroInstance);
        return heroInstance;
    }

    public bool AddHeroInstance(HeroInstance heroInstance)
    {
        if (heroInstance == null)
        {
            Debug.LogWarning("[HeroInventory] Attempted to add a null HeroInstance to the inventory.");
            return false;
        }

        if (!heroInstance.IsValid)
        {
            Debug.LogWarning($"[HeroInventory] Attempted to add an invalid HeroInstance to the inventory.");
            return false;
        }

        heroInstances.Add(heroInstance);
        return true;
    }

    public bool RemoveHeroInstance(HeroInstance heroInstance)
    {
        if (heroInstance == null)
        {
            Debug.LogWarning("[HeroInventory] Attempted to remove a null HeroInstance from the inventory.");
            return false;
        }

        return heroInstances.Remove(heroInstance);
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
