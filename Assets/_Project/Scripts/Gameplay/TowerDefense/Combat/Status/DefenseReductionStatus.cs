using System.Collections.Generic;
using UnityEngine;

public class DefenseReductionStatus
{
    private readonly List<DefenseReductionStack> stacks = new List<DefenseReductionStack>();
    private readonly UnitStats targetStats;
    private int nextStackId;
    private int maxStackCount;

    public StatusKey Key { get; }
    public bool IsActive => stacks.Count > 0;

    public DefenseReductionStatus(StatusKey key, UnitStats targetStats, int maxStackCount)
    {
        Key = key;
        this.targetStats = targetStats;
        this.maxStackCount = Mathf.Max(1, maxStackCount);
    }

    public void AddStack(float defenseReduction, float duration, int maxStackCount, string modifierId)
    {
        this.maxStackCount = Mathf.Max(1, maxStackCount);
        RemoveStacksOverLimit();

        if (stacks.Count >= this.maxStackCount)
        {
            stacks[FindShortestRemainingStack()].Refresh(duration);
            return;
        }

        string runtimeModifierId = $"{modifierId}_{Key.SourceId}_{nextStackId++}";
        UnitStatModifier modifier = new UnitStatModifier
        (
            UnitStatType.Defense,
            UnitStatModifierType.AdditivePercent,
            -Mathf.Abs(defenseReduction),
            runtimeModifierId
        );

        if (targetStats.AddModifier(modifier))
        {
            stacks.Add(new DefenseReductionStack(targetStats, modifier, duration));
        }
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        for (int i = stacks.Count - 1; i >= 0; i--)
        {
            DefenseReductionStack stack = stacks[i];
            stack.Tick(deltaTime);
            if (!stack.IsFinished)
            {
                continue;
            }

            stack.Clear();
            stacks.RemoveAt(i);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < stacks.Count; i++)
        {
            stacks[i].Clear();
        }

        stacks.Clear();
    }

    private void RemoveStacksOverLimit()
    {
        while (stacks.Count > maxStackCount)
        {
            int removeIndex = FindShortestRemainingStack();
            stacks[removeIndex].Clear();
            stacks.RemoveAt(removeIndex);
        }
    }

    private int FindShortestRemainingStack()
    {
        int shortestIndex = 0;
        float shortestDuration = stacks[0].RemainingDuration;

        for (int i = 1; i < stacks.Count; i++)
        {
            if (stacks[i].RemainingDuration < shortestDuration)
            {
                shortestIndex = i;
                shortestDuration = stacks[i].RemainingDuration;
            }
        }

        return shortestIndex;
    }

    private class DefenseReductionStack
    {
        private readonly UnitStats targetStats;
        private readonly UnitStatModifier modifier;
        private readonly CountdownTimer durationTimer;
        private bool isCleared;

        public float RemainingDuration => durationTimer.RemainingTime;
        public bool IsFinished => !durationTimer.IsRunning || durationTimer.IsFinished;

        public DefenseReductionStack(UnitStats targetStats, UnitStatModifier modifier, float duration)
        {
            this.targetStats = targetStats;
            this.modifier = modifier;
            durationTimer = new CountdownTimer(Mathf.Max(0f, duration));
            durationTimer.StartTimer();
        }

        public void Refresh(float duration)
        {
            durationTimer.Reset(Mathf.Max(0f, duration));
            durationTimer.StartTimer();
        }

        public void Tick(float deltaTime)
        {
            if (!IsFinished)
            {
                durationTimer.Tick(deltaTime);
            }
        }

        public void Clear()
        {
            if (isCleared)
            {
                return;
            }

            isCleared = true;
            durationTimer.StopTimer();
            targetStats.RemoveModifier(modifier);
        }
    }
}
