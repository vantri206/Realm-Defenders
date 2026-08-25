using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EnemyDepthSorter enemyDepthSorter;

    [Header("Wave Settings")]
    [SerializeField] private List<EnemySpawnEvent> spawnEvents = new List<EnemySpawnEvent>();

    private readonly EnemyWaveDirector enemyWaveDirector = new EnemyWaveDirector();

    private UnitCombatContext combatContext;
    private int totalSpawnCount;

    private bool isWaveRunning;
    private bool isSpawnCompleted;
    private bool isInitialized;

    public bool IsWaveRunning => isWaveRunning;
    public bool IsSpawnCompleted => isSpawnCompleted;
    public IReadOnlyList<EnemySpawnEvent> SpawnEvents => spawnEvents;
    public int TotalSpawnCount => totalSpawnCount;
    public bool IsInitialized => isInitialized;

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
    
    public void Initialize(UnitCombatContext combatContext, EnemyRouteGraph enemyRouteGraph, StageSystem levelSystem)
    {
        StopWave();
        isInitialized = false;

        if (combatContext == null || !combatContext.IsValid)
        {
            Debug.LogError("[EnemyWaveController] A valid CombatReferencesContext is required to initialize wave controller.", this);
            return;
        }

        this.combatContext = combatContext;

        if (enemySpawner != null)
        {
            enemySpawner.Initialize(combatContext, enemyRouteGraph, enemyDepthSorter, levelSystem);
        }

        if (enemySpawner == null || !enemySpawner.IsInitialized)
        {
            Debug.LogError("[EnemyWaveController] EnemySpawner must be initialized before initializing wave controller.", this);
            return;
        }

        enemyWaveDirector.Initialize(enemySpawner, spawnEvents);

        isInitialized = true;
        isSpawnCompleted = false;

        totalSpawnCount = 0;
        foreach (var spawnEvent in spawnEvents)
        {
            if (spawnEvent != null)
            {
                totalSpawnCount += spawnEvent.SpawnCount * spawnEvent.EnemyCount;
            }
        }
    }

    private void Update()
    {
        if (!isWaveRunning)
        {
            return;
        }

        enemyWaveDirector.Tick(combatContext.CombatTime.CombatDeltaTime);
        RefreshSpawnCompletion();
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
        enemyWaveDirector.StartDirector();
        enemyWaveDirector.Tick(0f);
        RefreshSpawnCompletion();
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
    }

    private bool CheckWaveSpawnFinished()
    {
        return enemyWaveDirector.CheckAllSpawnFinished();
    }

    private void RefreshSpawnCompletion()
    {
        if (!CheckWaveSpawnFinished())
        {
            return;
        }

        isSpawnCompleted = true;
        isWaveRunning = false;
    }

    private bool CheckWaveResolved()
    {
        return CheckWaveSpawnFinished();    // Add logic resolved = kill all enmies, or all enemies are dead, or all enemies are despawned. PLACEHOLDER
    }
}
