using System;
using System.Collections.Generic;
using UnityEngine;

internal static class UnitStatProgressionGenerator
{
    private struct StatAnchor
    {
        public StatAnchor(int level, float value)
        {
            Level = level;
            Value = value;
        }

        public int Level { get; }
        public float Value { get; }
    }

    public static bool TryGetBaseStats(ScriptableObject definition, out UnitBaseStats baseStats, out string error)
    {
        switch (definition)
        {
            case HeroDefinition hero:
                baseStats = new UnitBaseStats(
                    hero.MaxHealth,
                    hero.Attack,
                    hero.AttackInterval,
                    hero.Defense,
                    hero.SpecialDefense,
                    hero.MoveSpeed,
                    hero.BlockCount);
                error = null;
                return true;

            case EnemyDefinition enemy:
                baseStats = new UnitBaseStats(
                    enemy.MaxHealth,
                    enemy.Attack,
                    enemy.AttackInterval,
                    enemy.Defense,
                    enemy.SpecialDefense,
                    enemy.MoveSpeed,
                    0);
                error = null;
                return true;

            default:
                baseStats = null;
                error = "Unit Definition must be a HeroDefinition or EnemyDefinition.";
                return false;
        }
    }

    public static bool TryGenerate(
        UnitBaseStats baseStats,
        int maxLevel,
        AnimationCurve growthCurve,
        UnitStatFinalBreakdown finalBreakdown,
        IReadOnlyList<UnitStatLevelBreakdown> levelBreakdowns,
        float randomness,
        out UnitBaseStats[] statsByLevel,
        out string error)
    {
        statsByLevel = null;

        if (!ValidateInputs(baseStats, maxLevel, growthCurve, finalBreakdown, levelBreakdowns, out error))
        {
            return false;
        }

        randomness = Mathf.Clamp(randomness, 0f, ProgressionGenerationLimits.MaxRandomness);
        var random = new System.Random(Guid.NewGuid().GetHashCode());
        List<UnitStatLevelBreakdown> sortedBreakdowns = GetSortedBreakdowns(levelBreakdowns);

        float[] maxHealth = GenerateFloatStat(UnitStatType.MaxHealth, baseStats, maxLevel, growthCurve,
            finalBreakdown, sortedBreakdowns, randomness, random);
        float[] attack = GenerateFloatStat(UnitStatType.Attack, baseStats, maxLevel, growthCurve,
            finalBreakdown, sortedBreakdowns, randomness, random);
        float[] attackInterval = GenerateFloatStat(UnitStatType.AttackInterval, baseStats, maxLevel, growthCurve,
            finalBreakdown, sortedBreakdowns, randomness, random);
        float[] defense = GenerateFloatStat(UnitStatType.Defense, baseStats, maxLevel, growthCurve,
            finalBreakdown, sortedBreakdowns, randomness, random);
        float[] specialDefense = GenerateFloatStat(UnitStatType.SpecialDefense, baseStats, maxLevel, growthCurve,
            finalBreakdown, sortedBreakdowns, randomness, random);
        float[] moveSpeed = GenerateFloatStat(UnitStatType.MoveSpeed, baseStats, maxLevel, growthCurve,
            finalBreakdown, sortedBreakdowns, randomness, random);
        int[] blockCount = GenerateBlockCount(baseStats, maxLevel, finalBreakdown, sortedBreakdowns);

        statsByLevel = new UnitBaseStats[maxLevel];
        for (int index = 0; index < maxLevel; index++)
        {
            statsByLevel[index] = new UnitBaseStats(
                maxHealth[index],
                attack[index],
                attackInterval[index],
                defense[index],
                specialDefense[index],
                moveSpeed[index],
                blockCount[index]);
        }

        error = null;
        return true;
    }

    private static bool ValidateInputs(
        UnitBaseStats baseStats,
        int maxLevel,
        AnimationCurve growthCurve,
        UnitStatFinalBreakdown finalBreakdown,
        IReadOnlyList<UnitStatLevelBreakdown> levelBreakdowns,
        out string error)
    {
        if (baseStats == null)
        {
            error = "Base stats are required.";
            return false;
        }

        if (maxLevel < 1)
        {
            error = "Max level must be at least 1.";
            return false;
        }

        if (growthCurve == null || growthCurve.length == 0)
        {
            error = "A growth curve is required.";
            return false;
        }

        if (finalBreakdown == null)
        {
            error = "A final breakdown is required.";
            return false;
        }

        if (!ValidateFinalBreakdown(finalBreakdown, out error))
        {
            return false;
        }

        var usedLevels = new HashSet<int>();
        if (levelBreakdowns == null)
        {
            error = null;
            return true;
        }

        for (int index = 0; index < levelBreakdowns.Count; index++)
        {
            UnitStatLevelBreakdown breakdown = levelBreakdowns[index];
            if (breakdown == null)
            {
                error = "Level breakdown cannot be null.";
                return false;
            }

            if (breakdown.level <= 1 || breakdown.level >= maxLevel)
            {
                error = $"Breakdown level {breakdown.level} must be between level 2 and level {maxLevel - 1}.";
                return false;
            }

            if (!usedLevels.Add(breakdown.level))
            {
                error = $"Only one breakdown is allowed at level {breakdown.level}.";
                return false;
            }

            if (breakdown.targets == null)
            {
                error = $"Level {breakdown.level} has no target list.";
                return false;
            }

            if (breakdown.targets.Count == 0)
            {
                error = $"Level {breakdown.level} must contain at least one stat target.";
                return false;
            }

            var usedStats = new HashSet<UnitStatType>();
            for (int targetIndex = 0; targetIndex < breakdown.targets.Count; targetIndex++)
            {
                UnitStatBreakdownTarget target = breakdown.targets[targetIndex];
                if (target == null)
                {
                    error = $"Level {breakdown.level} contains an empty stat target.";
                    return false;
                }

                if (!usedStats.Add(target.statType))
                {
                    error = $"Level {breakdown.level} contains {target.statType} more than once.";
                    return false;
                }

                if (target.statType == UnitStatType.BlockCount)
                {
                    if (target.directValue < 0)
                    {
                        error = $"Block Count at level {breakdown.level} cannot be negative.";
                        return false;
                    }
                }
                else if (!IsValidMultiplier(target.multiplier))
                {
                    error = $"{target.statType} multiplier at level {breakdown.level} must be a finite value of 0 or more.";
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    private static bool ValidateFinalBreakdown(UnitStatFinalBreakdown finalBreakdown, out string error)
    {
        UnitStatType[] multiplierStats =
        {
            UnitStatType.MaxHealth,
            UnitStatType.Attack,
            UnitStatType.AttackInterval,
            UnitStatType.Defense,
            UnitStatType.SpecialDefense,
            UnitStatType.MoveSpeed
        };

        for (int index = 0; index < multiplierStats.Length; index++)
        {
            UnitStatType statType = multiplierStats[index];
            if (!IsValidMultiplier(finalBreakdown.GetMultiplier(statType)))
            {
                error = $"Final {statType} multiplier must be a finite value of 0 or more.";
                return false;
            }
        }

        if (finalBreakdown.blockCount < 0)
        {
            error = "Final Block Count cannot be negative.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsValidMultiplier(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }

    private static List<UnitStatLevelBreakdown> GetSortedBreakdowns(
        IReadOnlyList<UnitStatLevelBreakdown> levelBreakdowns)
    {
        var sorted = new List<UnitStatLevelBreakdown>();
        if (levelBreakdowns != null)
        {
            for (int index = 0; index < levelBreakdowns.Count; index++)
            {
                sorted.Add(levelBreakdowns[index]);
            }
        }

        sorted.Sort((left, right) => left.level.CompareTo(right.level));
        return sorted;
    }

    private static float[] GenerateFloatStat(
        UnitStatType statType,
        UnitBaseStats baseStats,
        int maxLevel,
        AnimationCurve growthCurve,
        UnitStatFinalBreakdown finalBreakdown,
        IReadOnlyList<UnitStatLevelBreakdown> sortedBreakdowns,
        float randomness,
        System.Random random)
    {
        var anchors = new List<StatAnchor>
        {
            new StatAnchor(1, UnitStatGenerationRules.GetBaseValue(baseStats, statType))
        };

        for (int breakdownIndex = 0; breakdownIndex < sortedBreakdowns.Count; breakdownIndex++)
        {
            UnitStatLevelBreakdown breakdown = sortedBreakdowns[breakdownIndex];
            for (int targetIndex = 0; targetIndex < breakdown.targets.Count; targetIndex++)
            {
                UnitStatBreakdownTarget target = breakdown.targets[targetIndex];
                if (target.statType != statType)
                {
                    continue;
                }

                float targetValue = UnitStatGenerationRules.GetBaseValue(baseStats, statType) * target.multiplier;
                anchors.Add(new StatAnchor(breakdown.level, targetValue));
                break;
            }
        }

        if (maxLevel > 1)
        {
            float finalValue = UnitStatGenerationRules.GetBaseValue(baseStats, statType)
                               * finalBreakdown.GetMultiplier(statType);
            anchors.Add(new StatAnchor(maxLevel, finalValue));
        }

        var values = new float[maxLevel];
        values[0] = UnitStatGenerationRules.NormalizeGeneratedValue(statType, anchors[0].Value);

        for (int anchorIndex = 0; anchorIndex < anchors.Count - 1; anchorIndex++)
        {
            StatAnchor start = anchors[anchorIndex];
            StatAnchor end = anchors[anchorIndex + 1];
            int levelDistance = end.Level - start.Level;
            float previousProgress = 0f;

            values[start.Level - 1] = UnitStatGenerationRules.NormalizeGeneratedValue(statType, start.Value);

            for (int level = start.Level + 1; level <= end.Level; level++)
            {
                float normalizedLevel = (level - start.Level) / (float)levelDistance;
                float shapedProgress = level == end.Level
                    ? 1f
                    : Mathf.Clamp01(growthCurve.Evaluate(normalizedLevel));

                if (level < end.Level && UnitStatGenerationRules.AllowsRandomness(statType) && randomness > 0f)
                {
                    shapedProgress += ((float)random.NextDouble() * 2f - 1f) * randomness;
                    shapedProgress = Mathf.Clamp01(shapedProgress);
                }

                shapedProgress = Mathf.Max(previousProgress, shapedProgress);
                float value = Mathf.LerpUnclamped(start.Value, end.Value, shapedProgress);
                values[level - 1] = UnitStatGenerationRules.NormalizeGeneratedValue(statType, value);
                previousProgress = shapedProgress;
            }
        }

        return values;
    }

    private static int[] GenerateBlockCount(
        UnitBaseStats baseStats,
        int maxLevel,
        UnitStatFinalBreakdown finalBreakdown,
        IReadOnlyList<UnitStatLevelBreakdown> sortedBreakdowns)
    {
        var anchors = new List<StatAnchor>
        {
            new StatAnchor(1, baseStats.BlockCount)
        };

        for (int breakdownIndex = 0; breakdownIndex < sortedBreakdowns.Count; breakdownIndex++)
        {
            UnitStatLevelBreakdown breakdown = sortedBreakdowns[breakdownIndex];
            for (int targetIndex = 0; targetIndex < breakdown.targets.Count; targetIndex++)
            {
                UnitStatBreakdownTarget target = breakdown.targets[targetIndex];
                if (target.statType == UnitStatType.BlockCount)
                {
                    anchors.Add(new StatAnchor(breakdown.level, target.directValue));
                    break;
                }
            }
        }

        if (maxLevel > 1)
        {
            anchors.Add(new StatAnchor(maxLevel, finalBreakdown.blockCount));
        }

        var values = new int[maxLevel];
        int anchorIndex = 0;
        int currentValue = Mathf.Max(0, Mathf.RoundToInt(anchors[0].Value));

        for (int level = 1; level <= maxLevel; level++)
        {
            while (anchorIndex + 1 < anchors.Count && anchors[anchorIndex + 1].Level <= level)
            {
                anchorIndex++;
                currentValue = Mathf.Max(0, Mathf.RoundToInt(anchors[anchorIndex].Value));
            }

            values[level - 1] = currentValue;
        }

        return values;
    }
}
