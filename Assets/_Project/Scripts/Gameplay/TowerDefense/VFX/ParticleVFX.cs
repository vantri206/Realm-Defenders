using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleVFX : MonoBehaviour, IPoolable
{
    public int PrefabID { get; set; }

    [SerializeField] private ParticleSystem particle;

    private CombatTimeController combatTime;
    private bool isReturningToPool;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        SyncSimulationSpeed();

        if (isReturningToPool || particle == null || particle.IsAlive(true))
        {
            return;
        }

        ReturnToPool();
    }

    public void SetCombatTime(CombatTimeController combatTime)
    {
        this.combatTime = combatTime;
        SyncSimulationSpeed();
    }

    public void OnSpawn()
    {
        isReturningToPool = false;
        CacheReferences();

        if (particle == null || combatTime == null)
        {
            Debug.LogError("[ParticleVFX] ParticleSystem and CombatTimeController are required before spawning.", this);
            ReturnToPool();
            return;
        }

        ParticleSystem.MainModule main = particle.main;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.None;
        main.simulationSpeed = combatTime.CombatSpeedMultiplier;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
    }

    public void OnDespawn()
    {
        isReturningToPool = true;

        if (particle != null)
        {
            ParticleSystem.MainModule main = particle.main;
            main.simulationSpeed = 1f;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        combatTime = null;
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

    private void SyncSimulationSpeed()
    {
        if (particle == null || combatTime == null)
        {
            return;
        }

        ParticleSystem.MainModule main = particle.main;
        main.simulationSpeed = combatTime.CombatSpeedMultiplier;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif
}
