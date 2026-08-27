using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory
{
    private List<GearInstance> gears = new List<GearInstance>();
    private int experiencePoints;

    public IReadOnlyList<GearInstance> Gears => gears;
    public int GearCount => gears.Count;
    public bool HasGear => gears.Count > 0;
    public int ExperiencePoints => experiencePoints;

    public IReadOnlyList<StarterHeroWeaponAssignment> LoadInitialInventory(StarterInventoryConfig config)
    {
        List<StarterHeroWeaponAssignment> assignments = new List<StarterHeroWeaponAssignment>();

        if (config == null)
        {
            Debug.LogWarning("[PlayerInventory] Cannot load from a null StarterInventoryConfig.");
            return assignments;
        }

        experiencePoints = config.StartingExperiencePoints;

        foreach (StarterGearConfig starterGear in config.StartGear)
        {
            if (starterGear == null || !starterGear.IsValid)
            {
                Debug.LogWarning("[PlayerInventory] Invalid starter gear config found.");
                continue;
            }

            GearInstance newGear = AddGear(new GearInstance(starterGear.Definition));
            if (newGear != null && starterGear.InitialEquippedHero != null)
            {
                assignments.Add(new StarterHeroWeaponAssignment(starterGear.InitialEquippedHero, newGear));
            }
        }

        return assignments;
    }

    private GearInstance AddGear(GearInstance gear)
    {
        if (gear == null || !gear.IsValid)
        {
            Debug.LogWarning("[PlayerInventory] Cannot add an invalid GearInstance.");
            return null;
        }

        gears.Add(gear);
        return gear;
    }

    public bool ContainsGear(GearInstance gear)
    {
        return gear != null && gear.IsValid && gears.Contains(gear);
    }

    public bool AddExperiencePoints(int amount)
    {
        experiencePoints += amount;
        return true;
    }

    public bool TrySpendExperiencePoints(int amount)
    {
        if (experiencePoints < amount)
        {
            return false;
        }

        experiencePoints -= amount;
        return true;
    }

    public bool TryClearInventory()
    {
        if (gears != null)
        {
            for (int i = 0; i < gears.Count; i++)
            {
                GearInstance gear = gears[i];

                if (gear != null && gear.EquippedHero != null)
                {
                    Debug.LogWarning($"[PlayerInventory] Cannot clear inventory: {gear.Definition.GearName} is equipped on {gear.EquippedHero.Definition.HeroName}.");
                    return false;
                }
            }

            gears.Clear();
        }

        experiencePoints = 0;
        return true;
    }
}

[Serializable]
public class StarterGearConfig
{
    [SerializeField] private GearDefinition definition;
    [SerializeField] private HeroDefinition initialEquippedHero;

    public GearDefinition Definition => definition;
    public HeroDefinition InitialEquippedHero => initialEquippedHero;

    public bool IsValid => definition != null;
}

[Serializable]
public class StarterInventoryConfig
{
    [SerializeField] private int startingExperiencePoints;
    [SerializeField] private List<StarterGearConfig> startGear = new List<StarterGearConfig>();

    public int StartingExperiencePoints => startingExperiencePoints;
    public IReadOnlyList<StarterGearConfig> StartGear => startGear;
    public int StartGearCount => startGear != null ? startGear.Count : 0;
    public bool HasStartGears => StartGearCount > 0;
}

public class StarterHeroWeaponAssignment
{
    public HeroDefinition HeroDefinition { get; private set; }
    public GearInstance GearInstance { get; private set; }

    public StarterHeroWeaponAssignment(HeroDefinition heroDefinition, GearInstance gearInstance)
    {
        HeroDefinition = heroDefinition;
        GearInstance = gearInstance;
    }
}

public class HeroGearAssignment
{
    public HeroInstance HeroInstance { get; private set; }
    public GearInstance GearInstance { get; private set; }

    public HeroGearAssignment(HeroInstance heroInstance, GearInstance gearInstance)
    {
        HeroInstance = heroInstance;
        GearInstance = gearInstance;
    }
}
