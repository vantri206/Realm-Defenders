using System;
using UnityEngine;

[Serializable]
public class UnitBaseStats
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float attack;
    [SerializeField] private float attackInterval;
    [SerializeField] private float defense;
    [SerializeField] private float specialDefense;
    [SerializeField] private float moveSpeed;
    [SerializeField] private int blockCount;

    public float MaxHealth => maxHealth;
    public float Attack => attack;
    public float AttackInterval => attackInterval;
    public float Defense => defense;
    public float SpecialDefense => specialDefense;
    public float MoveSpeed => moveSpeed;
    public int BlockCount => blockCount;

    public UnitBaseStats()
    {
        maxHealth = 0f;
        attack = 0f;
        attackInterval = 0f;
        defense = 0f;
        specialDefense = 0f;
        moveSpeed = 0f;
        blockCount = 0;
    }

    public UnitBaseStats(float maxHealth, float attack, float attackInterval, float defense,
                         float specialDefense, float moveSpeed, int blockCount)
    {
        this.maxHealth = maxHealth;
        this.attack = attack;
        this.attackInterval = attackInterval;
        this.defense = defense;
        this.specialDefense = specialDefense;
        this.moveSpeed = moveSpeed;
        this.blockCount = blockCount;
    }

    public UnitBaseStats(UnitBaseStats source)
    {
        if (source == null)
        {
            return;
        }

        maxHealth = source.maxHealth;
        attack = source.attack;
        attackInterval = source.attackInterval;
        defense = source.defense;
        specialDefense = source.specialDefense;
        moveSpeed = source.moveSpeed;
        blockCount = source.blockCount;
    }

    public float GetValue(UnitStatType statType)
    {
        return statType switch
        {
            UnitStatType.MaxHealth => maxHealth,
            UnitStatType.Attack => attack,
            UnitStatType.AttackInterval => attackInterval,
            UnitStatType.Defense => defense,
            UnitStatType.SpecialDefense => specialDefense,
            UnitStatType.MoveSpeed => moveSpeed,
            UnitStatType.BlockCount => blockCount,
            _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, null)
        };
    }
}
