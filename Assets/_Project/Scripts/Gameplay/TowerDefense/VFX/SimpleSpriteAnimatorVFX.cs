using UnityEngine;

[DisallowMultipleComponent]
public class SimpleSpriteAnimatorVFX : SimpleSpriteAnimator, IPoolable
{
    public int PrefabID { get; set; }

    [Header("VFX Lifecycle")]
    [SerializeField] private float playTime = 1f;

    private CountdownTimer playTimer;

    private bool isReturningToPool;

    protected override void Update()
    {
        base.Update();

        if (isReturningToPool || !IsPlaying || !IsLooping || playTimer == null)
        {
            return;
        }

        playTimer.Tick(Time.deltaTime);

        if (playTimer.IsFinished)
        {
            ReturnToPool();
        }
    }

    public void OnSpawn()
    {
        isReturningToPool = false;

        if (!TryPlay())
        {
            ReturnToPool();
            return;
        }

        if (IsLooping)
        {
            playTimer = new CountdownTimer(playTime);
            playTimer.StartTimer();
        }
    }

    public void OnDespawn()
    {
        isReturningToPool = true;

        if (playTimer != null)
        {
            playTimer.StopTimer();
            playTimer = null;
        }

        Restart();
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

    protected override void CompleteAnimation()
    {
        base.CompleteAnimation();
        ReturnToPool();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        playTime = Mathf.Max(0f, playTime);
    }
#endif
}
