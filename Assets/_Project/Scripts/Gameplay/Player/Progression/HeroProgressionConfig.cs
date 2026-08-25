using System;
using UnityEngine;

[Serializable]
public class HeroProgressionConfig
{
    [SerializeField] private ExperienceProgressionTable experienceTable;
    
    public int MaxLevel => experienceTable != null ? experienceTable.MaxLevel : 1;
    public ExperienceProgressionTable ExperienceTable => experienceTable;

    public bool IsValid => experienceTable != null && MaxLevel > 0 && MaxLevel <= experienceTable.MaxLevel;

    public HeroProgressionConfig(ExperienceProgressionTable experienceTable)
    {
        this.experienceTable = experienceTable;
    }
} 
