using UnityEngine;

public class HeroProgression
{
    [SerializeField] private HeroProgressionConfig progressionConfig;

    private bool isInitialized = false;

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
            return 0;
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

        int currentLevel = Mathf.Clamp(hero.Level, 1, MaxLevel);
        ApplyHeroLevel(hero, currentLevel);
        
        return true;
    }

    public bool UpgradeHeroLevel(HeroInstance hero)
    {
        if (!isInitialized)
        {
            Debug.LogError("[HeroProgression] Cannot upgrade hero level. HeroProgression is not initialized.");
            return false;
        }

        if (hero == null || !hero.IsValid)
        {
            Debug.LogError("[HeroProgression] Invalid HeroInstance provided for level upgrade.");
            return false;
        }

        if (IsMaxLevel(hero.Level))
        {
            return false;
        }

        ApplyHeroLevel(hero, hero.Level + 1);

        return true;
    }

    private void ApplyHeroLevel(HeroInstance hero, int level)
    {
        if (hero == null || !hero.IsValid)
        {
            Debug.LogError("[HeroProgression] Invalid HeroInstance provided for applying level progression.");
            return;
        }

        UnitBaseStats newBaseStats = CalculateHeroBaseStats(hero.Definition, level);
        hero.ApplyProgression(level, newBaseStats);
    }

    public UnitBaseStats CalculateHeroBaseStats(HeroDefinition hero, int level)
    {
        UnitStatProgressionTable progressionTable = hero.StatProgressionTable;

        if (progressionTable != null && level >= 1 && level <= progressionTable.MaxLevel)
        {
            UnitBaseStats progressionStats = progressionTable.GetStatsForLevel(level);
            if (progressionStats != null)
            {
                return progressionStats;
            }
        }

        return new UnitBaseStats(
            hero.MaxHealth,
            hero.Attack,
            hero.AttackInterval,
            hero.Defense,
            hero.SpecialDefense,
            hero.MoveSpeed,
            hero.BlockCount);
    }

}
