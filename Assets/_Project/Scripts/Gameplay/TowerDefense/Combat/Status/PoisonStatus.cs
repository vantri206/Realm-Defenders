using System.Collections.Generic;
using UnityEngine;

public class PoisonStatus
{
    public StatusKey Key { get; private set; }

    private readonly List<PoisonStack> stacks = new List<PoisonStack>();
    private readonly UnitRuntime target;

    private int maxStackCount;

    public int StackCount => stacks.Count;
    public bool IsActive => stacks.Count > 0;

    public PoisonStatus(StatusKey key, UnitRuntime target, int maxStackCount)
    {
        Key = key;
        this.target = target;
        this.maxStackCount = Mathf.Max(1, maxStackCount);
    }

    public void AddStack(GameObject attacker, float damagePerTick, float duration, float tickInterval, int maxStackCount)
    {
        this.maxStackCount = Mathf.Max(1, maxStackCount);
        RemoveStacksOverLimit();

        PoisonStack newStack = new PoisonStack(attacker, damagePerTick, duration, tickInterval);
        if (stacks.Count < this.maxStackCount)
        {
            stacks.Add(newStack);
            return;
        }

        int replaceIndex = FindShortestRemainingStack();    // Replace the stack with the shortest remaining duration
        stacks[replaceIndex] = newStack;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || target == null || target.IsDead)
        {
            return;
        }

        for (int i = stacks.Count - 1; i >= 0; i--)
        {
            PoisonStack stack = stacks[i];
            stack.Tick(deltaTime, target);

            if (stack.IsFinished)
            {
                stacks.RemoveAt(i);
            }

            if (target.IsDead)
            {
                return;
            }
        }
    }

    public void Clear()
    {
        stacks.Clear();
    }

    private void RemoveStacksOverLimit()
    {
        while (stacks.Count > maxStackCount)
        {
            stacks.RemoveAt(FindShortestRemainingStack());
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

    private class PoisonStack
    {
        private readonly GameObject attacker;
        private readonly float damagePerTick;
        private readonly CountdownTimer durationTimer;
        private readonly CountdownTimer damageTickTimer;

        public float RemainingDuration => durationTimer.RemainingTime;
        public bool IsFinished => !durationTimer.IsRunning || durationTimer.IsFinished;

        public PoisonStack(GameObject attacker, float damagePerTick, float duration, float tickInterval)
        {
            this.attacker = attacker;
            this.damagePerTick = Mathf.Max(0f, damagePerTick);

            durationTimer = new CountdownTimer(Mathf.Max(0f, duration));
            damageTickTimer = new CountdownTimer(Mathf.Max(0f, tickInterval));

            durationTimer.StartTimer();
            damageTickTimer.StartTimer();
        }

        public void Tick(float deltaTime, UnitRuntime target)
        {
            if (IsFinished || deltaTime <= 0f || target == null || target.IsDead)
            {
                return;
            }

            float activeDeltaTime = Mathf.Min(deltaTime, durationTimer.RemainingTime);
            durationTimer.Tick(deltaTime);

            while (activeDeltaTime > 0f)
            {
                float tickDeltaTime = Mathf.Min(activeDeltaTime, damageTickTimer.RemainingTime);
                damageTickTimer.Tick(tickDeltaTime);
                activeDeltaTime -= tickDeltaTime;

                if (!damageTickTimer.IsFinished)
                {
                    return;
                }

                ApplyPoisonDamage(target);
                if (target.IsDead)
                {
                    return;
                }

                damageTickTimer.Reset();
                damageTickTimer.StartTimer();
            }
        }

        private void ApplyPoisonDamage(UnitRuntime target)
        {
            DamageRequest damageRequest = new DamageRequest
            (
                attacker,
                target.Health,
                damagePerTick,
                AttackDamageType.MagicalDamage,
                target.CenterPosition
            );

            DamageSystem.ApplyDamage(damageRequest);
        }
    }
}
