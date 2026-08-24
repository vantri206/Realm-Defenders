using System;
using UnityEngine;

[Serializable]
public class RunConfig
{
    [SerializeField]private int maxLevel;
    [SerializeField] private ExperienceProgressionTable experienceTable;
    
    public int MaxLevel => maxLevel;
    public ExperienceProgressionTable ExperienceTable => experienceTable;

    public bool IsValid => experienceTable != null && maxLevel > 0 && maxLevel <= experienceTable.MaxLevel;

    public RunConfig(int maxLevel, ExperienceProgressionTable experienceTable)
    {
        this.maxLevel = maxLevel;
        this.experienceTable = experienceTable;
    }
} 
