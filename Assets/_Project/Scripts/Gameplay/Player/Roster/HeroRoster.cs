using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StarterHeroRoster
{
    [SerializeField] private List<HeroInstance> startHeroes = new List<HeroInstance>();

    public IReadOnlyList<HeroInstance> StartHeroes => startHeroes;
    public int HeroCount => startHeroes.Count;
    public bool HasHeroes => startHeroes.Count > 0;
}

[Serializable]
public class HeroRoster
{
    [SerializeField] private List<HeroInstance> heroes = new List<HeroInstance>();

    public IReadOnlyList<HeroInstance> Heroes => heroes;
    public int HeroCount => heroes.Count;
    public bool HasHeroes => heroes.Count > 0;

    public void LoadInitialRoster(StarterHeroRoster startRoster)
    {
        heroes.Clear();

        if (startRoster == null)
        {
            Debug.LogWarning("[HeroRoster] Cannot load from a null StarterHeroRoster.");
            return;
        }

        foreach (HeroInstance hero in startRoster.StartHeroes)
        {
            AddHero(hero);
        }
    }

    public bool AddHero(HeroInstance hero)
    {
        if (hero == null)
        {
            Debug.LogWarning("[HeroRoster] Cannot add a null HeroInstance.");
            return false;
        }

        if (!hero.IsValid)
        {
            Debug.LogWarning("[HeroRoster] Failed to create a valid HeroInstance.");
            return false;
        }

        HeroInstance instance = new HeroInstance(hero);
        if (!instance.IsValid)
        {
            Debug.LogWarning("[HeroRoster] Failed to create a valid HeroInstance.");
            return false;
        }

        heroes.Add(instance);
        return true;
    }

    public void Clear()
    {
        heroes.Clear();
    }
}
