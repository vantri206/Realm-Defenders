using System;
using UnityEngine;

public abstract class Timer
{
    protected float initialTime;
    protected float time;

    public bool IsRunning { get; protected set; }

    public float Progress => initialTime > 0f ? time / initialTime : 0f;

    public Action OnTimeStart;
    public Action OnTimeStop;

    public virtual void Initialize(float duration)
    {
        initialTime = duration;
        IsRunning = false;
    }

    public virtual void StartTimer()
    {
        time = initialTime;
        if (!IsRunning)
        {
            IsRunning = true;
            OnTimeStart?.Invoke();
        }
    }

    public virtual void StopTimer()
    {
        if (IsRunning)
        {
            IsRunning = false;
            OnTimeStop?.Invoke();
        }
    }

    public void Pause() => IsRunning = false;
    public void Resume() => IsRunning = true;

    public abstract void Tick(float deltaTime);
}

public class CountdownTimer : Timer
{
    public CountdownTimer(float duration)
    {
        Initialize(duration);
    }

    public override void Tick(float deltaTime)
    {
        if (IsRunning && time > 0f)
        {
            time -= deltaTime;
        }

        if (IsRunning && time <= 0f)
        {
            time = 0f;
            StopTimer();
        }
    }

    public float RemainingTime => time;
    public float TotalTime => initialTime;
    public bool IsFinished => time <= 0f;

    public void Reset()
    {
        time = initialTime;
    }

    public void Reset(float newDuration)
    {
        initialTime = newDuration;
        time = initialTime;
    }
}

public class StopwatchTimer : Timer
{
    public StopwatchTimer()
    {
        Initialize(0f);
    }

    public override void StartTimer()
    {
        time = 0f;
        if (!IsRunning)
        {
            IsRunning = true;
            OnTimeStart?.Invoke();
        }
    }

    public override void Tick(float deltaTime)
    {
        if (IsRunning)
        {
            time += deltaTime;
        }
    }

    public void Reset()
    {
        time = 0f;
    }

    public float CurrentTime => time;
}
