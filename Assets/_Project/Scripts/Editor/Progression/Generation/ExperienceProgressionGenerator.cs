using System;
using UnityEngine;

internal static class ExperienceProgressionGenerator
{
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

        if (totalExperienceForMaxLevel < thresholdCount)
        {
            error = $"Total EXP must be at least {thresholdCount} so every threshold can increase.";
            return false;
        }

        randomness = Mathf.Clamp(randomness, 0f, ProgressionGenerationLimits.MaxRandomness);
        experienceThresholds = new int[thresholdCount];

        var random = new System.Random(Guid.NewGuid().GetHashCode());
        float averageStep = totalExperienceForMaxLevel / (float)thresholdCount;
        int previousThreshold = 0;

        for (int index = 0; index < thresholdCount; index++)
        {
            bool isFinalThreshold = index == thresholdCount - 1;
            if (isFinalThreshold)
            {
                experienceThresholds[index] = totalExperienceForMaxLevel;
                break;
            }

            float progress = (index + 1f) / thresholdCount;
            float shapedProgress = Mathf.Clamp01(growthCurve.Evaluate(progress));
            float randomOffset = ((float)random.NextDouble() * 2f - 1f) * averageStep * randomness;
            int candidate = Mathf.RoundToInt(totalExperienceForMaxLevel * shapedProgress + randomOffset);

            int remainingThresholds = thresholdCount - index - 1;
            int minimum = previousThreshold + 1;
            int maximum = totalExperienceForMaxLevel - remainingThresholds;
            candidate = Mathf.Clamp(candidate, minimum, maximum);

            experienceThresholds[index] = candidate;
            previousThreshold = candidate;
        }

        return true;
    }
}
