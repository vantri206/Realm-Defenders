using UnityEngine;

public class ExperienceProgressionTable : ScriptableObject
{
    [SerializeField, HideInInspector] private int[] experienceThresholds;

    public int MaxLevel => experienceThresholds != null ? experienceThresholds.Length + 1 : 1;

    public int GetExperienceToLevelUp(int level)
    {
        if (experienceThresholds == null)
        {
            Debug.LogError($"[ExperienceProgressionTable] Invalid level {level}. Valid levels are between 1 and {MaxLevel}.");
            return 0;
        }

        if (level >= MaxLevel)
        {
            return 0;
        }

        if (level <= 1)
        {
            return experienceThresholds[0]; // Experience needed to reach level 2
        }

        return experienceThresholds[level - 1] - experienceThresholds[level - 2];
    }

    public int GetExperienceForLevel(int level)
    {
        if (experienceThresholds == null)
        {
            Debug.LogError($"[ExperienceProgressionTable] Invalid level {level}. Valid levels are between 1 and {MaxLevel}.");
            return 0;
        }

        level = Mathf.Clamp(level, 1, MaxLevel);

        if (level == 1)
        {
            return 0; // Level 1 starts at 0 experience
        }

        return experienceThresholds[level - 2];
    }

    public int GetLevelForExperience(int experience)
    {
        if (experienceThresholds == null || experienceThresholds.Length == 0)
        {
            Debug.LogError("[ExperienceProgressionTable] Experience table is empty.");
            return 1;
        }

        if (experience <= 0) 
        {
            return 1;
        }

        if (experience >= GetExperienceForLevel(MaxLevel))
        {
            return MaxLevel;
        }

        for (int level = 2; level < MaxLevel; level++)
        {
            if (experience >= GetExperienceForLevel(level) && experience < GetExperienceForLevel(level + 1))
            {
                return level;
            }
        }

        return 1; // Default to level 1 if no match found
    }
}
