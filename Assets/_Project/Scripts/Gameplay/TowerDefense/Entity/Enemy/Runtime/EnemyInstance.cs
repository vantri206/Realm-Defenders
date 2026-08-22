using System;
using UnityEngine;

[Serializable]
public class EnemyInstance
{
    private EnemyDefinition definition;
    private int level = 1;
    private UnitStats stats = new UnitStats();
    private bool isObjectiveEnemy = true;

    public EnemyDefinition Definition => definition;
    public int Level => level;
    public UnitStats Stats => stats;
    public bool IsObjectiveEnemy => isObjectiveEnemy;

    public bool IsValid => definition != null && stats != null;

    public void Initialize(EnemyDefinition definition)
    {
        Initialize(definition, null);
    }

    public void Initialize(EnemyDefinition definition, UnitStats stats)
    {
        if (definition == null)
        {
            Debug.LogError("[EnemyInstance] EnemyDefinition cannot be null.");
            return;
        }

        this.definition = definition;
        if (stats != null)
        {
            stats = new UnitStats(stats);
        }
        else
        {
            stats = new UnitStats(GetDefaultStats(definition));
        }
        isObjectiveEnemy = true;
    }

    private static UnitBaseStats GetDefaultStats(EnemyDefinition definition)
    {
        return new UnitBaseStats(
            definition.MaxHealth,
            definition.Attack,
            definition.AttackInterval,
            definition.Defense,
            definition.SpecialDefense,
            definition.MoveSpeed,
            0);
    }
}
