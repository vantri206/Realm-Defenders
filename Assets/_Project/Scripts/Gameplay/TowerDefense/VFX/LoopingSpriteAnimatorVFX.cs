using UnityEngine;

[DisallowMultipleComponent]
public class LoopingSpriteAnimatorVFX : SimpleSpriteAnimator, IPoolable
{
    public int PrefabID { get; set; }

    private bool isReturningToPool;

    protected override void Awake()
    {
        base.Awake();
        SetLooping(true);
    }

    public void OnSpawn()
    {
        isReturningToPool = false;
        SetLooping(true);

        if (!TryPlay())
        {
            ReturnToPool();
        }
    }

    public void StopVFX()
    {
        ReturnToPool();
    }

    public void OnDespawn()
    {
        isReturningToPool = true;
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

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetLooping(true);
    }
#endif
}
