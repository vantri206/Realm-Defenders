using UnityEngine;

[DisallowMultipleComponent]
public class TriggeredSpriteAnimatorVFX : MonoBehaviour, IPoolable
{
    public int PrefabID { get; set; }

    [Header("Animation Sequence")]
    [SerializeField] private SimpleSpriteAnimator primaryAnimator;
    [SerializeField] private SimpleSpriteAnimator triggeredAnimator;
    [SerializeField] private int triggerFrameIndex = 0;

    private bool hasTriggered;
    private bool isReturningToPool;

    private void Awake()
    {
        SubscribeToAnimatorEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromAnimatorEvents();
    }

    public void SetCombatTime(CombatTimeController combatTime)
    {
        if (primaryAnimator != null)
        {
            primaryAnimator.SetCombatTime(combatTime);
        }

        if (triggeredAnimator != null)
        {
            triggeredAnimator.SetCombatTime(combatTime);
        }
    }

    public void OnSpawn()
    {
        isReturningToPool = false;
        hasTriggered = false;

        if (primaryAnimator == null || triggeredAnimator == null)
        {
            Debug.LogError("[TriggeredSpriteAnimatorVFX] Primary Animator and Triggered Animator are required.", this);
            ReturnToPool();
            return;
        }

        primaryAnimator.Restart();
        primaryAnimator.SetLooping(false);

        triggeredAnimator.Restart();
        triggeredAnimator.SetLooping(false);

        primaryAnimator.Play();
    }

    public void OnDespawn()
    {
        isReturningToPool = true;
        hasTriggered = false;

        if (primaryAnimator != null)
        {
            primaryAnimator.Restart();
        }

        if (triggeredAnimator != null)
        {
            triggeredAnimator.Restart();
        }

        SetCombatTime(null);
    }

    public void ReturnToPool()
    {
        if (isReturningToPool)
        {
            return;
        }

        isReturningToPool = true;
        ObjectPoolingHelper.Release(this);
    }

    private void HandlePrimaryFrameChanged(int frameIndex)
    {
        if (isReturningToPool || hasTriggered || frameIndex != triggerFrameIndex)
        {
            return;
        }

        hasTriggered = true;
        triggeredAnimator.Play();
    }

    private void HandlePrimaryAnimationCompleted()
    {
        if (isReturningToPool)
        {
            return;
        }

        primaryAnimator.Restart();

        if (!hasTriggered)
        {
            Debug.LogError($"[TriggeredSpriteAnimatorVFX] Primary animation completed before reaching trigger frame {triggerFrameIndex}.", this);
            ReturnToPool();
        }
    }

    private void HandleTriggeredAnimationCompleted()
    {
        ReturnToPool();
    }

    private void SubscribeToAnimatorEvents()
    {
        if (primaryAnimator != null)
        {
            primaryAnimator.FrameChanged += HandlePrimaryFrameChanged;
            primaryAnimator.AnimationCompleted += HandlePrimaryAnimationCompleted;
        }

        if (triggeredAnimator != null)
        {
            triggeredAnimator.AnimationCompleted += HandleTriggeredAnimationCompleted;
        }
    }

    private void UnsubscribeFromAnimatorEvents()
    {
        if (primaryAnimator != null)
        {
            primaryAnimator.FrameChanged -= HandlePrimaryFrameChanged;
            primaryAnimator.AnimationCompleted -= HandlePrimaryAnimationCompleted;
        }

        if (triggeredAnimator != null)
        {
            triggeredAnimator.AnimationCompleted -= HandleTriggeredAnimationCompleted;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        triggerFrameIndex = Mathf.Max(0, triggerFrameIndex);
    }
#endif
}
