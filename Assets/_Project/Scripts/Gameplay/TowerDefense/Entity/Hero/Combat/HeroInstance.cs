using System;
using UnityEngine;

[Serializable]
public class HeroInstance
{
    [SerializeField] private HeroDefinition definition;
    [SerializeField] private int experience = 0;
    [SerializeField] private UnitStats stats = new UnitStats();

    private int currentLevel = 1;

    public HeroDefinition Definition => definition;
    public int Level => currentLevel;
    public UnitStats Stats => stats;
    public bool IsValid => definition != null;

    public HeroInstance() { }

    public HeroInstance(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogError("[HeroInstance] A valid HeroInstance is required to create a copy.");
            return;
        }

        definition = heroInstance.definition;
        experience = heroInstance.experience;
        stats = new UnitStats(heroInstance.stats);
    }

    public HeroInstance(HeroDefinition definition)
    {
        Initialize(definition, 0, null);
    }

    public HeroInstance(HeroDefinition definition, int experience, UnitStats stats)
    {
        Initialize(definition, experience, stats);
    }

    public void Initialize(HeroDefinition definition, int experience, UnitStats stats)
    {
        if (definition == null)
        {
            Debug.LogError("[HeroInstance] HeroDefinition cannot be null.");
            return;
        }

        this.definition = definition;
        this.experience = Mathf.Max(0, experience);
        if (stats != null)
        {
            this.stats = new UnitStats(stats);
        }
        else
        {
            this.stats = new UnitStats(GetDefaultStats(definition));
        }
    }

    public void SetProgression(int experience, int level)
    {
        this.experience = Mathf.Max(0, experience);
        this.currentLevel = Mathf.Max(1, level);
    }

    public void SetCombatStats(UnitStats stats)
    {
        if (stats == null)
        {
            Debug.LogError("[HeroInstance] Stats cannot be null.");
            return;
        }

        this.stats = new UnitStats(stats);
    }

    private static UnitBaseStats GetDefaultStats(HeroDefinition definition)
    {
        return new UnitBaseStats(
            definition.MaxHealth,
            definition.Attack,
            definition.AttackInterval,
            definition.Defense,
            definition.SpecialDefense,
            definition.MoveSpeed,
            definition.BlockCount);
    }
}
