using UnityEngine;

public class RunProgression
{
    [SerializeField] private RunConfig runConfig;

    private bool isInitialized = false;

    public RunConfig RunConfig => runConfig;
    public int MaxLevel => runConfig != null ? runConfig.MaxLevel : 1;

    public bool IsInitialized => isInitialized;

    public void Initialize(RunConfig config)
    {
        if (config == null || !config.IsValid)
        {
            Debug.LogError("[RunProgression] Invalid RunConfig provided for initialization.");
            return;
        }

        runConfig = config;
        isInitialized = true;
    }

    public int GetLevelForExperience(int experience)
    {
        if (!isInitialized)
        {
            Debug.LogError("[RunProgression] Cannot get level for experience. RunProgression is not initialized.");
            return 1;
        }

        return Mathf.Min(MaxLevel, runConfig.ExperienceTable.GetLevelForExperience(experience));
    }

    public int GetExperienceForLevel(int level)
    {
        if (!isInitialized)
        {
            Debug.LogError("[RunProgression] Cannot get experience for level. RunProgression is not initialized.");
            return 0;
        }

        if (level > 1) level = Mathf.Min(level, MaxLevel);
        else level = 1;

        return runConfig.ExperienceTable.GetExperienceForLevel(level);
    }

    public bool IsMaxLevel(int level)
    {
        if (!isInitialized)
        {
            Debug.LogError("[RunProgression] Cannot check max level. RunProgression is not initialized.");
            return false;
        }

        return level >= runConfig.MaxLevel;
    }

    public int GetExperienceToLevelUp(int level)
    {
        if (!isInitialized)
        {
            Debug.LogError("[RunProgression] Cannot get experience to level up. RunProgression is not initialized.");
            return 0;
        }

        if (IsMaxLevel(level))
        {
            return 0; // No experience needed to level up from the max level
        }

        return runConfig.ExperienceTable.GetExperienceToLevelUp(level);
    }

    public bool RefreshHeroLevel(HeroInstance hero)
    {
        if (!isInitialized)
        {
            Debug.LogError("[RunProgression] Cannot refresh hero level. RunProgression is not initialized.");
            return false;
        }

        if (hero == null || !hero.IsValid)
        {
            Debug.LogError("[RunProgression] Invalid HeroInstance provided for level refresh.");
            return false;
        }

        return true;
    }

    public bool AddExperienceForHero(HeroInstance hero, int experienceAmount)
    {
        if (!isInitialized)
        {
            Debug.LogError("[RunProgression] Cannot add experience for hero. RunProgression is not initialized.");
            return false;
        }

        if (hero == null || !hero.IsValid)
        {
            Debug.LogError("[RunProgression] Invalid HeroInstance provided for adding experience.");
            return false;
        }

        return true;
    }
}
