using UnityEngine;

public class StunStatus
{
    private readonly CountdownTimer durationTimer;

    public StatusKey Key { get; }
    public float RemainingDuration => durationTimer.RemainingTime;
    public bool IsActive => durationTimer.IsRunning && !durationTimer.IsFinished;

    public StunStatus(StatusKey key, float duration)
    {
        Key = key;
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
        if (!IsActive || deltaTime <= 0f)
        {
            return;
        }

        durationTimer.Tick(deltaTime);
    }
}
