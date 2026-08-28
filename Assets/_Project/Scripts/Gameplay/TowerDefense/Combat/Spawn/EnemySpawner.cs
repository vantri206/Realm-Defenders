using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private const float spawnSpreadCell = 0.35f;
    private const float minSpawnSpreadDistance = 0.45f;
    private const int spawnPositionAttemptCount = 32;

    private readonly Dictionary<string, CombatSpawnPointDefinition> spawnPoints = new Dictionary<string, CombatSpawnPointDefinition>();

    private UnitCombatContext combatContext;
    private EnemyRouteGraph routeGraph;
    private EnemyDepthSorter enemyDepthSorter;
    private StageSystem stageSystem;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(UnitCombatContext combatContext, EnemyRouteGraph routeGraph, EnemyDepthSorter enemyDepthSorter, StageSystem stageSystem, CombatMapData mapData)
    {
        this.combatContext = combatContext;
        this.routeGraph = routeGraph;
        this.enemyDepthSorter = enemyDepthSorter;
        this.stageSystem = stageSystem;

        spawnPoints.Clear();
        if (mapData != null)
        {
            for (int i = 0; i < mapData.SpawnPoints.Count; i++)
            {
                CombatSpawnPointDefinition spawnPoint = mapData.SpawnPoints[i];
                if (spawnPoint != null && !string.IsNullOrWhiteSpace(spawnPoint.SpawnPointId))
                {
                    spawnPoints[spawnPoint.SpawnPointId] = spawnPoint;
                }
            }
        }

        isInitialized = this.combatContext != null && this.combatContext.IsValid && this.routeGraph != null && this.stageSystem != null && spawnPoints.Count > 0;

        if (!isInitialized)
        {
            Debug.LogError("[EnemySpawner] Failed to initialize enemy spawner.", this);
        }
    }

    public EnemyRuntime SpawnEnemy(EnemySpawnEventDefinition spawnEvent)
    {
        if (spawnEvent == null)
        {
            Debug.LogError("[EnemySpawner] EnemySpawnEventDefinition is required to spawn enemy.", this);
            return null;
        }

        if (!isInitialized)
        {
            Debug.LogError("[EnemySpawner] EnemySpawner must be initialized before spawning enemies.", this);
            return null;
        }

        if (spawnEvent.EnemyDefinition == null || !spawnEvent.EnemyDefinition.IsValid)
        {
            Debug.LogError("[EnemySpawner] A valid EnemyDefinition is required to spawn enemy.", this);
            return null;
        }

        if (!TryGetSpawnCell(spawnEvent.SpawnPointId, out CombatGridCell spawnCell))
        {
            return null;
        }

        if (!TryGetSpawnPosition(spawnEvent.EnemyDefinition, spawnCell, out Vector3 spawnPosition))
        {
            Debug.LogError($"[EnemySpawner] Failed to get spawn position for cell {spawnCell.CellPosition}.", this);
            return null;
        }

        return SpawnEnemy(spawnEvent.EnemyDefinition, spawnPosition, spawnCell, spawnEvent.RouteId);
    }

    private EnemyRuntime SpawnEnemy(EnemyDefinition enemyDefinition, Vector3 spawnPosition, CombatGridCell spawnCell, string routeId)
    {
        EnemyInstance enemyInstance = CreateEnemyInstance(enemyDefinition);
        if (enemyInstance == null)
        {
            Debug.LogError("[EnemySpawner] Failed to create enemy instance.", this);
            return null;
        }

        EnemyRuntime enemy = Instantiate(enemyDefinition.Prefab, spawnPosition, Quaternion.identity, transform);
        if (enemy == null)
        {
            Debug.LogError("[EnemySpawner] Failed to instantiate enemy prefab.", this);
            return null;
        }

        enemy.Initialize(enemyInstance, combatContext, routeGraph, spawnCell.CellPosition, routeId, enemyDepthSorter);
        if (!enemy.IsInitialized)
        {
            Debug.LogError("[EnemySpawner] Spawned enemy failed to initialize.", enemy);
            Destroy(enemy.gameObject);
            return null;
        }

        enemyDepthSorter.RegisterEnemy(enemy);
        stageSystem.RegisterEnemy(enemy, new EnemyTrackingData(
            enemyInstance.IsObjectiveEnemy,
            GameplayConstants.NORMAL_ENEMY_LIVES_DAMAGE,
            enemyInstance.Definition.MeatReward));
        RegisterEnemyEvents(enemy);
        return enemy;
    }

    private EnemyInstance CreateEnemyInstance(EnemyDefinition enemyDefinition)
    {
        if (enemyDefinition == null)
        {
            Debug.LogError("[EnemySpawner] EnemyDefinition is required to create enemy instance.", this);
            return null;
        }

        EnemyInstance enemyInstance = new EnemyInstance();
        enemyInstance.Initialize(enemyDefinition);
        return enemyInstance;
    }

    private bool TryGetSpawnCell(string spawnPointId, out CombatGridCell cell)
    {
        if (string.IsNullOrWhiteSpace(spawnPointId) || !spawnPoints.TryGetValue(spawnPointId, out CombatSpawnPointDefinition spawnPoint))
        {
            Debug.LogError($"[EnemySpawner] Spawn point '{spawnPointId}' was not found in combat map data.", this);
            cell = null;
            return false;
        }

        if (!combatContext.CombatGrid.TryGetCell(spawnPoint.CellPosition, out cell))
        {
            Debug.LogError($"[EnemySpawner] Spawn point '{spawnPointId}' does not resolve to a built combat grid cell.", this);
            return false;
        }

        return true;
    }

    private bool TryGetSpawnPosition(EnemyDefinition enemyDefinition, CombatGridCell spawnCell, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        if (enemyDefinition == null || spawnCell == null || combatContext == null || combatContext.CombatGrid == null)
        {
            return false;
        }

        if (!combatContext.CombatGrid.TryCellToWorldCenter(spawnCell, out Vector3 cellCenter))
        {
            return false;
        }

        Vector3 cellSize = combatContext.CombatGrid.CellSize;
        float spreadX = Mathf.Abs(cellSize.x) * spawnSpreadCell;
        float spreadY = Mathf.Abs(cellSize.y) * spawnSpreadCell;
        float minDistanceSqr = minSpawnSpreadDistance * minSpawnSpreadDistance;
        float bestDistanceSqr = float.NegativeInfinity;
        Vector3 bestCenterPosition = cellCenter;

        for (int i = 0; i < spawnPositionAttemptCount; i++)
        {
            Vector2 centerSpread = new Vector2(
                Random.Range(-spreadX, spreadX),
                Random.Range(-spreadY, spreadY));
            Vector3 candidateCenterPosition = cellCenter + (Vector3)centerSpread;
            float nearestEnemyDistanceSqr = GetNearestEnemyDistanceSqr(candidateCenterPosition);

            if (nearestEnemyDistanceSqr >= minDistanceSqr)
            {
                spawnPosition = candidateCenterPosition - (Vector3)enemyDefinition.NavigationOffset;
                return true;
            }

            if (nearestEnemyDistanceSqr > bestDistanceSqr)
            {
                bestDistanceSqr = nearestEnemyDistanceSqr;
                bestCenterPosition = candidateCenterPosition;
            }
        }

        spawnPosition = bestCenterPosition - (Vector3)enemyDefinition.NavigationOffset;
        return true;
    }

    private float GetNearestEnemyDistanceSqr(Vector3 centerPosition)
    {
        if (stageSystem == null || stageSystem.ActiveEnemies == null || stageSystem.ActiveEnemies.Count == 0)
        {
            return float.PositiveInfinity;
        }

        float nearestDistanceSqr = float.PositiveInfinity;
        foreach (EnemyRuntime enemy in stageSystem.ActiveEnemies.Keys)
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            float distanceSqr = ((Vector2)centerPosition - (Vector2)enemy.CenterPosition).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
            }
        }

        return nearestDistanceSqr;
    }

    private void RegisterEnemyEvents(EnemyRuntime enemy)
    {
        enemy.OnDestroyed += HandleEnemyDestroyed;
        enemy.OnEscaped += HandleEnemyEscaped;
    }

    private void UnregisterEnemyEvents(EnemyRuntime enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.OnDestroyed -= HandleEnemyDestroyed;
        enemy.OnEscaped -= HandleEnemyEscaped;
    }

    private void HandleEnemyDestroyed(UnitRuntime unitRuntime)
    {
        if (!(unitRuntime is EnemyRuntime enemy))
        {
            return;
        }

        stageSystem.ResolveEnemy(enemy, EnemyResolveReason.Killed);
        enemyDepthSorter.UnregisterEnemy(enemy);
        UnregisterEnemyEvents(enemy);
    }

    private void HandleEnemyEscaped(EnemyRuntime enemy)
    {
        if (enemy == null)
        {
            return;
        }

        stageSystem.ResolveEnemy(enemy, EnemyResolveReason.Escaped);
        enemyDepthSorter.UnregisterEnemy(enemy);
        UnregisterEnemyEvents(enemy);
    }

    private void CacheReferences()
    {
        if (enemyDepthSorter == null)
        {
            enemyDepthSorter = GetComponent<EnemyDepthSorter>();
        }
    }
}
