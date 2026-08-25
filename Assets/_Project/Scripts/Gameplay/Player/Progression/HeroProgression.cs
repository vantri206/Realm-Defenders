using UnityEngine;

public class HeroProgression
{
    [SerializeField] private HeroProgressionConfig progressionConfig;

    private bool isInitialized = false;

    public HeroProgressionConfig ProgressionConfig => progressionConfig;
    public int MaxLevel => progressionConfig != null ? progressionConfig.MaxLevel : 1;

    public bool IsInitialized => isInitialized;

    public void Initialize(HeroProgressionConfig config)
    {
        if (config == null || !config.IsValid)
        {
            Debug.LogError("[HeroProgression] Invalid HeroProgressionConfig provided for initialization.");
            return;
        }

        progressionConfig = config;
        isInitialized = true;
    }

    public int GetLevelForExperience(int experience)
    {
        if (!isInitialized)
        {
            Debug.LogError("[HeroProgression] Cannot get level for experience. HeroProgression is not initialized.");
            return 1;
        }

        return Mathf.Min(MaxLevel, progressionConfig.ExperienceTable.GetLevelForExperience(experience));
    }

    public int GetExperienceForLevel(int level)
    {
        if (!isInitialized)
        {
            Debug.LogError("[HeroProgression] Cannot get experience for level. HeroProgression is not initialized.");
            return 0;
        }

        if (level > 1) level = Mathf.Min(level, MaxLevel);
        else level = 1;

        return progressionConfig.ExperienceTable.GetExperienceForLevel(level);
    }

    public bool IsMaxLevel(int level)
    {
        if (!isInitialized)
        {
            Debug.LogError("[HeroProgression] Cannot check max level. HeroProgression is not initialized.");
            return false;
        }

        return level >= progressionConfig.MaxLevel;
    }

    public int GetExperienceToLevelUp(int level)
    {
        if (!isInitialized)
        {
            Debug.LogError("[HeroProgression] Cannot get experience to level up. HeroProgression is not initialized.");
            return 0;
        }

        if (IsMaxLevel(level))
        {
            return 0; // No experience needed to level up from the max level
        }

        return progressionConfig.ExperienceTable.GetExperienceToLevelUp(level);
    }

    public bool RefreshHeroLevel(HeroInstance hero)
    {
        if (!isInitialized)
        {
            Debug.LogError("[HeroProgression] Cannot refresh hero level. HeroProgression is not initialized.");
            return false;
        }

        if (hero == null || !hero.IsValid)
        {
            Debug.LogError("[HeroProgression] Invalid HeroInstance provided for level refresh.");
            return false;
        }

        return true;
    }

    public bool AddExperienceForHero(HeroInstance hero, int experienceAmount)
    {
        if (!isInitialized)
        {
            Debug.LogError("[HeroProgression] Cannot add experience for hero. HeroProgression is not initialized.");
            return false;
        }

        if (hero == null || !hero.IsValid)
        {
            Debug.LogError("[HeroProgression] Invalid HeroInstance provided for adding experience.");
            return false;
        }

        return true;
    }
}
