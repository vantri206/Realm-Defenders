using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EnemyDepthSorter enemyDepthSorter;

    private readonly EnemyWaveDirector enemyWaveDirector = new EnemyWaveDirector();

    private UnitCombatContext combatContext;
    private bool isWaveRunning;
    private bool isSpawnCompleted;
    private bool isWaveResolved;
    private bool isInitialized;

    public bool IsWaveRunning => isWaveRunning;
    public bool IsSpawnCompleted => isSpawnCompleted;
    public bool IsWaveResolved => isWaveResolved;
    public bool IsInitialized => isInitialized;

    public event Action OnWaveResolved;

    private void Awake()
    {
        if (enemySpawner == null)
        {
            enemySpawner = GetComponent<EnemySpawner>();
        }

        if (enemyDepthSorter == null)
        {
            enemyDepthSorter = GetComponent<EnemyDepthSorter>();
        }
    }

    public void Initialize(UnitCombatContext combatContext, EnemyRouteGraph enemyRouteGraph, StageSystem stageSystem, CombatMapData mapData, IReadOnlyList<EnemySpawnEventDefinition> spawnEvents)
    {
        ClearWave();

        if (combatContext == null || !combatContext.IsValid)
        {
            Debug.LogError("[EnemyWaveController] A valid UnitCombatContext is required.", this);
            return;
        }

        this.combatContext = combatContext;

        if (enemySpawner != null)
        {
            enemySpawner.Initialize(combatContext, enemyRouteGraph, enemyDepthSorter, stageSystem, mapData);
        }

        if (enemySpawner == null || !enemySpawner.IsInitialized)
        {
            Debug.LogError("[EnemyWaveController] EnemySpawner must be initialized before the wave controller.", this);
            return;
        }

        enemyWaveDirector.Initialize(enemySpawner, spawnEvents);
        isInitialized = true;
    }

    private void Update()
    {
        if (!isWaveRunning)
        {
            return;
        }

        enemyWaveDirector.Tick(combatContext.CombatTime.CombatDeltaTime);
        RefreshWaveState();
    }

    public void StartWave()
    {
        if (!isInitialized)
        {
            Debug.LogError("[EnemyWaveController] EnemyWaveController must be initialized before starting a wave.", this);
            return;
        }

        if (isWaveRunning)
        {
            Debug.LogWarning("[EnemyWaveController] Wave is already running.", this);
            return;
        }

        isWaveRunning = true;
        isSpawnCompleted = false;
        isWaveResolved = false;
        enemyWaveDirector.StartDirector();
        enemyWaveDirector.Tick(0f);
        RefreshWaveState();
    }

    public void StopWave()
    {
        enemyWaveDirector.StopDirector();
        isWaveRunning = false;
    }

    public void ClearWave()
    {
        StopWave();
        enemyWaveDirector.ClearDirector();
        combatContext = null;
        isInitialized = false;
        isSpawnCompleted = false;
        isWaveResolved = false;
    }

    private void RefreshWaveState()
    {
        isSpawnCompleted = enemyWaveDirector.CheckAllSpawnFinished();
        if (isWaveResolved || !enemyWaveDirector.CheckAllSpawnResolved())
        {
            return;
        }

        isWaveResolved = true;
        isWaveRunning = false;
        OnWaveResolved?.Invoke();
    }
}
