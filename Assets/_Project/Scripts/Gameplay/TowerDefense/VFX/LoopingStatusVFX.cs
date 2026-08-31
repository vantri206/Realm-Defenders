using UnityEngine;

[DisallowMultipleComponent]
public class LoopingStatusVFX : MonoBehaviour, IPoolable
{
    public int PrefabID { get; set; }

    [SerializeField] private SimpleSpriteAnimator[] spriteAnimators;

    private bool isReturningToPool;

    private void Awake()
    {
        CacheSpriteAnimators();
        SetAnimatorsLooping();
    }

    public void OnSpawn()
    {
        isReturningToPool = false;

        for (int i = 0; i < spriteAnimators.Length; i++)
        {
            SimpleSpriteAnimator spriteAnimator = spriteAnimators[i];
            if (spriteAnimator != null)
            {
                spriteAnimator.Play();
            }
        }
    }

    public void StopVFX()
    {
        ReturnToPool();
    }

    public void OnDespawn()
    {
        isReturningToPool = true;

        for (int i = 0; i < spriteAnimators.Length; i++)
        {
            SimpleSpriteAnimator spriteAnimator = spriteAnimators[i];
            if (spriteAnimator != null)
            {
                spriteAnimator.Restart();
            }
        }
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

    private void SetAnimatorsLooping()
    {
        for (int i = 0; i < spriteAnimators.Length; i++)
        {
            SimpleSpriteAnimator spriteAnimator = spriteAnimators[i];
            if (spriteAnimator != null)
            {
                spriteAnimator.SetLooping(true);
            }
        }
    }

    private void CacheSpriteAnimators()
    {
        if (spriteAnimators == null || spriteAnimators.Length == 0)
        {
            spriteAnimators = GetComponentsInChildren<SimpleSpriteAnimator>(true);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheSpriteAnimators();
        SetAnimatorsLooping();
    }
#endif
}
