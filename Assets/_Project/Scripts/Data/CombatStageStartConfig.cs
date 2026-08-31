using System;
using UnityEngine;

[Serializable]
public class CombatStageStartConfig
{
    [SerializeField] private int startingMeat = 20;
    [SerializeField] private int startingLives = 10;
    [SerializeField] private int naturalMeatPerSecond = 1;

    public int StartingMeat => startingMeat;
    public int StartingLives => startingLives;
    public int NaturalMeatPerSecond => naturalMeatPerSecond;

    public CombatStageStartConfig() { }

    public CombatStageStartConfig(int startingMeat, int startingLives, int naturalMeatPerSecond)
    {
        this.startingMeat = startingMeat;
        this.startingLives = startingLives;
        this.naturalMeatPerSecond = naturalMeatPerSecond;
    }

    public CombatStageStartConfig(CombatStageStartConfig other)
    {
        if (other != null)
        {
            this.startingMeat = other.startingMeat;
            this.startingLives = other.startingLives;
            this.naturalMeatPerSecond = other.naturalMeatPerSecond;
        }
    }
}
