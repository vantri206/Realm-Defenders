using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleVFX : MonoBehaviour, IPoolable
{
    public int PrefabID { get; set; }

    [SerializeField] private ParticleSystem particle;

    private bool isReturningToPool;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        if (isReturningToPool || particle == null || particle.IsAlive(true))
        {
            return;
        }

        ReturnToPool();
    }

    public void OnSpawn()
    {
        isReturningToPool = false;
        CacheReferences();

        if (particle == null)
        {
            Debug.LogError("[ParticleVFX] ParticleSystem reference is missing.", this);
            ReturnToPool();
            return;
        }

        ParticleSystem.MainModule main = particle.main;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.None;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
    }

    public void OnDespawn()
    {
        isReturningToPool = true;

        if (particle != null)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

    private void CacheReferences()
    {
        if (particle == null)
        {
            particle = GetComponent<ParticleSystem>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif
}
