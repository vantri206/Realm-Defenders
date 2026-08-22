using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StartRunTeam
{
    [SerializeField] private List<HeroInstance> startHeroes = new List<HeroInstance>();

    public IReadOnlyList<HeroInstance> StartHeroes => startHeroes;
    public int HeroCount => startHeroes.Count;
    public bool HasHeroes => startHeroes.Count > 0;
}

[Serializable]
public class RunTeam
{
    [SerializeField] private List<HeroInstance> heroes = new List<HeroInstance>();

    public IReadOnlyList<HeroInstance> Heroes => heroes;
    public int HeroCount => heroes.Count;
    public bool HasHeroes => heroes.Count > 0;

    public void LoadInitialTeam(StartRunTeam startRunTeam)
    {
        heroes.Clear();

        if (startRunTeam == null)
        {
            Debug.LogWarning("[RunTeam] Cannot load from a null StartRunTeam.");
            return;
        }

        foreach (HeroInstance hero in startRunTeam.StartHeroes)
        {
            AddHero(hero);
        }
    }

    public bool AddHero(HeroInstance hero)
    {
        if (hero == null)
        {
            Debug.LogWarning("[RunTeam] Cannot add a null HeroInstance.");
            return false;
        }

        if (!hero.IsValid)
        {
            Debug.LogWarning("[RunTeam] Failed to create a valid HeroInstance.");
            return false;
        }

        HeroInstance instance = new HeroInstance(hero);
        if (!instance.IsValid)
        {
            Debug.LogWarning("[RunTeam] Failed to create a valid HeroInstance.");
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
