using System;
using UnityEngine;

internal static class ExperienceProgressionGenerator
{
    private const double MinimumWeight = 0.000001d;

    public static bool TryGenerate(
        int maxLevel,
        int totalExperienceForMaxLevel,
        AnimationCurve growthCurve,
        float randomness,
        out int[] experienceThresholds,
        out string error)
    {
        experienceThresholds = null;
        error = null;

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

        int thresholdCount = maxLevel - 1;
        if (thresholdCount == 0)
        {
            experienceThresholds = Array.Empty<int>();
            return true;
        }

        // Reserve costs 1, 2, 3, ... so every level-up cost is strictly greater than the previous one.
        long minimumTotalExperience = (long)thresholdCount * (thresholdCount + 1) / 2;
        if (totalExperienceForMaxLevel < minimumTotalExperience)
        {
            error = $"Total EXP must be at least {minimumTotalExperience} so EXP required by each level can increase.";
            return false;
        }

        randomness = Mathf.Clamp(randomness, 0f, ProgressionGenerationLimits.MaxRandomness);
        experienceThresholds = new int[thresholdCount];

        var random = new System.Random(Guid.NewGuid().GetHashCode());
        var costWeights = new double[thresholdCount];
        if (!TryBuildIncreasingCostWeights(growthCurve, randomness, random, costWeights, out error))
        {
            experienceThresholds = null;
            return false;
        }

        var extraExperienceByLevel = new long[thresholdCount];
        long remainingExperience = totalExperienceForMaxLevel - minimumTotalExperience;
        double totalWeight = 0d;
        for (int index = 0; index < costWeights.Length; index++)
        {
            totalWeight += costWeights[index];
        }

        long distributedExperience = 0;
        for (int index = 0; index < thresholdCount; index++)
        {
            long extraExperience = (long)Math.Floor(remainingExperience * costWeights[index] / totalWeight);
            extraExperienceByLevel[index] = extraExperience;
            distributedExperience += extraExperience;
        }

        // Add rounding leftovers evenly, then to a suffix so the extra allocation stays non-decreasing.
        long roundingRemainder = remainingExperience - distributedExperience;
        long sharedRemainder = roundingRemainder / thresholdCount;
        int suffixRemainder = (int)(roundingRemainder % thresholdCount);

        long cumulativeExperience = 0;
        long previousLevelCost = 0;

        for (int index = 0; index < thresholdCount; index++)
        {
            long suffixBonus = suffixRemainder > 0 && index >= thresholdCount - suffixRemainder ? 1 : 0;
            long extraExperience = extraExperienceByLevel[index] + sharedRemainder + suffixBonus;
            long levelCost = index + 1L + extraExperience;

            if (levelCost <= previousLevelCost)
            {
                experienceThresholds = null;
                error = "Failed to generate strictly increasing EXP costs. Increase Total EXP or adjust the curve.";
                return false;
            }

            cumulativeExperience += levelCost;
            experienceThresholds[index] = (int)cumulativeExperience;
            previousLevelCost = levelCost;
        }

        if (cumulativeExperience != totalExperienceForMaxLevel)
        {
            experienceThresholds = null;
            error = "Failed to preserve Total EXP while generating thresholds.";
            return false;
        }

        return true;
    }

    private static bool TryBuildIncreasingCostWeights(
        AnimationCurve growthCurve,
        float randomness,
        System.Random random,
        double[] costWeights,
        out string error)
    {
        if (!TryEvaluateCurve(growthCurve, 0f, out double previousCurveValue)
            || !TryEvaluateCurve(growthCurve, 1f, out double finalCurveValue))
        {
            error = "Growth curve must contain only finite values.";
            return false;
        }

        if (finalCurveValue <= previousCurveValue)
        {
            error = "Growth curve must end above its starting value.";
            return false;
        }

        double previousMarginalWeight = 0d;
        double previousCostWeight = 0d;

        for (int index = 0; index < costWeights.Length; index++)
        {
            float progress = (index + 1f) / costWeights.Length;
            if (!TryEvaluateCurve(growthCurve, progress, out double curveValue))
            {
                error = "Growth curve must contain only finite values.";
                return false;
            }

            double marginalWeight = Math.Max(0d, curveValue - previousCurveValue);
            double randomScale = 1d + ((random.NextDouble() * 2d - 1d) * randomness);

            double costWeight;
            if (index == 0)
            {
                costWeight = Math.Max(MinimumWeight, marginalWeight * randomScale);
            }
            else
            {
                double weightIncrease = Math.Max(0d, marginalWeight - previousMarginalWeight);
                costWeight = previousCostWeight + Math.Max(MinimumWeight, weightIncrease * randomScale);
            }

            costWeights[index] = costWeight;
            previousCurveValue = curveValue;
            previousMarginalWeight = marginalWeight;
            previousCostWeight = costWeight;
        }

        error = null;
        return true;
    }

    private static bool TryEvaluateCurve(AnimationCurve growthCurve, float progress, out double value)
    {
        float evaluatedValue = growthCurve.Evaluate(progress);
        if (float.IsNaN(evaluatedValue) || float.IsInfinity(evaluatedValue))
        {
            value = 0d;
            return false;
        }

        value = Mathf.Clamp01(evaluatedValue);
        return true;
    }
}
