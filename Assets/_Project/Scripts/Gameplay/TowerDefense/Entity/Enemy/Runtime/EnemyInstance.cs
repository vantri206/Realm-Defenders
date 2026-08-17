using System;
using UnityEngine;

[Serializable]
public class EnemyInstance
{
    private EnemyDefinition definition;
    private UnitStats unitStats;
    private UnitSpeed enemySpeed;

    public EnemyDefinition Definition => definition;
    public UnitStats Stats => unitStats;
    public UnitSpeed Speed => enemySpeed;

    public bool IsValid => definition != null;

    public void Initialize(EnemyDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogError("[EnemyInstance] EnemyDefinition cannot be null.");
            return;
        }

        this.definition = definition;
        unitStats = new UnitStats(definition.MaxHealth, definition.Attack, definition.AttackInterval,
                                  definition.Defense, definition.SpecialDefense);
        enemySpeed = new UnitSpeed(definition.MoveSpeed);
    }

}
