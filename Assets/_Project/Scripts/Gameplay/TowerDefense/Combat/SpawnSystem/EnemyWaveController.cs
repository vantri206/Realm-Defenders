using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EnemyWaveDirector enemyWaveDirector;

    [SerializeField] private EnemyDepthSorter enemyDepthSorter;

    [Header("Wave Settings")]
    [SerializeField] private List<EnemySpawnEvent> spawnEvents = new List<EnemySpawnEvent>();

    private bool isWaveRunning;
    private bool isSpawnCompleted;
    private Coroutine waveRoutine;

    private bool isInitialized;

    public bool IsWaveRunning => isWaveRunning;
    public bool IsSpawnCompleted => isSpawnCompleted;
    public IReadOnlyList<EnemySpawnEvent> SpawnEvents => spawnEvents;

    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        if (enemySpawner == null)
        {
            enemySpawner = GetComponent<EnemySpawner>();
        }

        if (enemyWaveDirector == null)
        {
            enemyWaveDirector = GetComponent<EnemyWaveDirector>();
        }
    }

    public void Initialize(CombatGrid combatGrid, EnemyRouteGraph routeGraph, UnitPathfindingSystem pathfindingSystem)
    {
        StopWave();

        if (enemySpawner != null)
        {
            enemySpawner.Initialize(combatGrid, routeGraph, pathfindingSystem, enemyDepthSorter);
        }

        if (enemySpawner == null || !enemySpawner.IsInitialized)
        {
            Debug.LogError("[EnemyWaveController] EnemySpawner must be initialized before initializing wave controller.", this);
            return;
        }

        if (enemyWaveDirector == null)
        {
            Debug.LogError("[EnemyWaveController] EnemyWaveDirector reference is missing.", this);
            return;
        }

        enemyWaveDirector.Initialize(enemySpawner, spawnEvents);

        isInitialized = true;
        isSpawnCompleted = false;

        StartWave();
    }

    public void StartWave()
    {
        if (!isInitialized)
        {
            Debug.LogError("[EnemyWaveController] EnemyWaveController must be initialized before starting wave.", this);
            return;
        }

        if (isWaveRunning)
        {
            Debug.LogWarning("[EnemyWaveController] Wave is already running.", this);
            return;
        }

        isWaveRunning = true;
        isSpawnCompleted = false;
        waveRoutine = StartCoroutine(RunWaveRoutine());
    }

    public void StopWave()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        if (enemyWaveDirector != null)
        {
            enemyWaveDirector.StopDirector();
        }

        isWaveRunning = false;
    }

    public void ClearWave()
    {
        StopWave();

        if (enemyWaveDirector != null)
        {
            enemyWaveDirector.ClearDirector();
        }

        isInitialized = false;
        isSpawnCompleted = false;
    }

    private IEnumerator RunWaveRoutine()
    {
        enemyWaveDirector.StartDirector();

        while (!CheckWaveSpawnFinished())
        {
            yield return null;
        }

        isSpawnCompleted = true;

        isWaveRunning = false;
        waveRoutine = null;
    }

    private bool CheckWaveSpawnFinished()
    {
        return enemyWaveDirector != null && enemyWaveDirector.CheckAllSpawnFinished();
    }

    private bool CheckWaveResolved()
    {
        return CheckWaveSpawnFinished();    // Add logic resolved = kill all enmies, or all enemies are dead, or all enemies are despawned. PLACEHOLDER
    }
}
