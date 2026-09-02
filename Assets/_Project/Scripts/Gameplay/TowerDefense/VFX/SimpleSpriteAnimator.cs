using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSpriteAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<Sprite> frames = new List<Sprite>();
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private int startFrameIndex;
    [SerializeField] private bool isLoop;

    private int currentFrameIndex;
    private CountdownTimer frameTimer;
    private CombatTimeController combatTime;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;
    public bool IsLooping => isLoop;
    public int StartFrameIndex => startFrameIndex;

    public event Action<int> FrameChanged;
    public event Action AnimationCompleted;

    protected virtual void Awake()
    {
        CacheReferences();
    }

    protected virtual void Update()
    {
        if (!isPlaying || frameTimer == null)
        {
            return;
        }

        frameTimer.Tick(CombatDeltaTime);

        if (!frameTimer.IsRunning)
        {
            AdvanceFrame();

            if (isPlaying)
            {
                frameTimer.Reset();
                frameTimer.StartTimer();
            }
        }
    }

    public void Play()
    {
        TryPlay();
    }

    public void SetCombatTime(CombatTimeController combatTime)
    {
        this.combatTime = combatTime;
    }

    protected bool TryPlay()
    {
        CacheReferences();
        framesPerSecond = Mathf.Max(0.01f, framesPerSecond);

        if (spriteRenderer == null)
        {
            Debug.LogError("[SimpleSpriteAnimator] SpriteRenderer is required to play the animation.", this);
            Restart();
            return false;
        }

        if (frames == null || frames.Count == 0)
        {
            Debug.LogWarning("[SimpleSpriteAnimator] At least one animation frame is required to play the animation.", this);
            Restart();
            return false;
        }

        currentFrameIndex = Mathf.Clamp(startFrameIndex, 0, frames.Count - 1);

        float frameDuration = 1f / framesPerSecond;
        frameTimer = new CountdownTimer(frameDuration);
        frameTimer.StartTimer();

        isPlaying = true;
        ShowCurrentSprite();
        return true;
    }

    public void StopAnimation()
    {
        isPlaying = false;
        if (frameTimer != null)
        {
            frameTimer.StopTimer();
        }
    }

    public void Restart()
    {
        StopAnimation();
        currentFrameIndex = 0;
        frameTimer = null;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
        }
    }

    protected float CombatDeltaTime
    {
        get
        {
            if (combatTime != null)
            {
                return combatTime.CombatDeltaTime;
            }

            return 0f;
        }
    }

    protected virtual void CompleteAnimation()
    {
        StopAnimation();
        AnimationCompleted?.Invoke();
    }

    public void SetLooping(bool shouldLoop)
    {
        isLoop = shouldLoop;
    }

    private void AdvanceFrame()
    {
        currentFrameIndex++;

        if (currentFrameIndex >= frames.Count)
        {
            if (!isLoop)
            {
                CompleteAnimation();
                return;
            }

            currentFrameIndex = 0;
        }

        ShowCurrentSprite();
    }

    private void ShowCurrentSprite()
    {
        spriteRenderer.sprite = frames[currentFrameIndex];
        FrameChanged?.Invoke(currentFrameIndex);
    }

    private void CacheReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        framesPerSecond = Mathf.Max(0.01f, framesPerSecond);
        startFrameIndex = Mathf.Max(0, startFrameIndex);

        if (frames != null && frames.Count > 0)
        {
            startFrameIndex = Mathf.Min(startFrameIndex, frames.Count - 1);
        }

        CacheReferences();
    }
#endif
}
