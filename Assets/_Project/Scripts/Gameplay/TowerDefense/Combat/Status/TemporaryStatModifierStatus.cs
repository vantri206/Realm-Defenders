using System.Collections.Generic;

public class TemporaryStatModifierStatus
{
    private readonly List<UnitStatModifier> activeStatModifiers = new List<UnitStatModifier>();
    private readonly UnitStats targetStats;
    private readonly CountdownTimer durationTimer;

    public StatusKey Key { get; }
    public bool IsActive => durationTimer.IsRunning && !durationTimer.IsFinished;

    public TemporaryStatModifierStatus(StatusKey key, UnitStats targetStats, IReadOnlyList<UnitStatModifier> modifiers, float duration)
    {
        Key = key;
        this.targetStats = targetStats;
        durationTimer = new CountdownTimer(duration);

        for (int i = 0; i < modifiers.Count; i++)
        {
            UnitStatModifier sourceModifier = modifiers[i];
            string runtimeModifierId = $"{sourceModifier.ModifierId}_{key.SourceId}";
            UnitStatModifier runtimeModifier = new UnitStatModifier
            (
                sourceModifier.StatType,
                sourceModifier.ModifierType,
                sourceModifier.Value,
                runtimeModifierId
            );

            if (targetStats.AddModifier(runtimeModifier))
            {
                activeStatModifiers.Add(runtimeModifier);
            }
        }

        durationTimer.StartTimer();
    }

    public void Refresh(float duration)
    {
        durationTimer.Reset(duration);
        durationTimer.StartTimer();
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f)
        {
            return;
        }

        durationTimer.Tick(deltaTime);
        if (durationTimer.IsFinished)
        {
            Clear();
        }
    }

    public void Clear()
    {
        durationTimer.StopTimer();

        for (int i = 0; i < activeStatModifiers.Count; i++)
        {
            targetStats.RemoveModifier(activeStatModifiers[i]);
        }

        activeStatModifiers.Clear();
    }
}
