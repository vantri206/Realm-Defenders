using System;
using UnityEngine;

[Serializable]
public class HeroInstance
{
    [SerializeField] private HeroDefinition definition;
    [SerializeField] private int level = 1;
    [SerializeField] private UnitStats stats = new UnitStats();

    public HeroDefinition Definition => definition;
    public int Level => level;
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
        level = heroInstance.level;
        stats = new UnitStats(heroInstance.stats);
    }

    public HeroInstance(HeroDefinition definition)
    {
        Initialize(definition, 1, null);
    }

    public HeroInstance(HeroDefinition definition, int level, UnitStats stats)
    {
        Initialize(definition, level, stats);
    }

    public void Initialize(HeroDefinition definition, int level, UnitStats stats)
    {
        if (definition == null)
        {
            Debug.LogError("[HeroInstance] HeroDefinition cannot be null.");
            return;
        }

        this.definition = definition;
        this.level = Mathf.Max(1, level);
        if (stats != null)
        {
            stats = new UnitStats(stats);
        }
        else
        {
            stats = new UnitStats(GetDefaultStats(definition));
        }
    }

    public void SetLevel(int value)
    {
        level = Mathf.Max(1, value);
    }

    public void SetCombatStats(UnitStats stats)
    {
        if (stats == null)
        {
            Debug.LogError("[HeroInstance] Stats cannot be null.");
            return;
        }

        stats = new UnitStats(stats);
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
