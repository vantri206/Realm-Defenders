using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class HeroRoster
{
    private List<HeroInstance> heroes = new List<HeroInstance>();

    public IReadOnlyList<HeroInstance> Heroes => heroes;
    public int HeroCount => heroes.Count;
    public bool HasHeroes => heroes.Count > 0;

    public void LoadInitialRoster(StarterHeroRoster startRoster)
    {
        if (startRoster == null)
        {
            Debug.LogWarning("[HeroRoster] Cannot load from a null StarterHeroRoster.");
            return;
        }

        foreach (StarterHeroConfig config in startRoster.StartHeroes)
        {
            if (config == null || !config.IsValid)
            {
                Debug.LogWarning("[HeroRoster] Invalid starter hero config found.");
                continue;
            }

            AddHero(new HeroInstance(config.Definition, config.StartingLevel));
        }
    }

    private bool AddHero(HeroInstance hero)
    {
        if (hero == null || !hero.IsValid)
        {
            Debug.LogWarning("[HeroRoster] Cannot add a null HeroInstance.");
            return false;
        }

        heroes.Add(hero);
        return true;
    }

    public bool ContainsHero(HeroInstance hero)
    {
        return hero != null && hero.IsValid && heroes.Contains(hero);
    }

    public HeroInstance GetHeroByDefinition(HeroDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        return heroes.FirstOrDefault(hero => hero.Definition == definition);
    }

    public bool TryClearRoster()
    {
        if (heroes == null || heroes.Count == 0)
        {
            return true;
        }

        heroes.Clear();
        return true;
    }
}


[Serializable]
public class StarterHeroConfig
{
    [SerializeField] private HeroDefinition definition;
    [SerializeField] private int startingLevel = 1;

    public HeroDefinition Definition => definition;
    public int StartingLevel => startingLevel;

    public bool IsValid => definition != null;
}

[Serializable]
public class StarterHeroRoster
{
    [SerializeField] private List<StarterHeroConfig> startHeroes = new List<StarterHeroConfig>();

    public IReadOnlyList<StarterHeroConfig> StartHeroes => startHeroes;
    public int HeroCount => startHeroes != null ? startHeroes.Count : 0;
    public bool HasStartHeroes => HeroCount > 0;
}
