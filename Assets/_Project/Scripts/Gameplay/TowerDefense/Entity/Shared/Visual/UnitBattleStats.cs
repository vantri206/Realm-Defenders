using System;
using UnityEngine;

[Serializable]
public class UnitStats
{
    private float maxHealth;
    private float attack;
    private float attackInterval;
    private float defense;
    private float specialDefense;

    public float MaxHealth
    {
        get => maxHealth;
        set => maxHealth = Mathf.Max(0f, value);
    }
    public float Attack
    {
        get => attack;
        set => attack = Mathf.Max(0f, value);
    }
    public float AttackInterval
    {
        get => attackInterval;
        set => attackInterval = Mathf.Max(0f, value);
    }
    public float Defense
    {
        get => defense;
        set => defense = value;
    }
    public float SpecialDefense
    {
        get => specialDefense;
        set => specialDefense = value;
    }

    public UnitStats()
    {
        maxHealth = 0f;
        attack = 0f;
        attackInterval = 0f;
        defense = 0f;
        specialDefense = 0f;
    }
    
    public UnitStats(float maxHealth, float attack, float attackInterval, float defense, float specialDefense)
    {
        this.maxHealth = Mathf.Max(0f, maxHealth);
        this.attack = Mathf.Max(0f, attack);
        this.attackInterval = Mathf.Max(0f, attackInterval);
        this.defense = defense;
        this.specialDefense = specialDefense;
    }
}
