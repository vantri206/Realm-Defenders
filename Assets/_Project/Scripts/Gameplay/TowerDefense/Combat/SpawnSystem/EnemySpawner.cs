using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private CombatGrid combatGrid;
    private EnemyRouteGraph routeGraph;
    private UnitPathfindingSystem pathfindingSystem;

    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    public void Initialize(CombatGrid combatGrid, EnemyRouteGraph routeGraph, UnitPathfindingSystem pathfindingSystem)
    {
        this.combatGrid = combatGrid;
        this.routeGraph = routeGraph;
        this.pathfindingSystem = pathfindingSystem;
        isInitialized = this.combatGrid != null && this.routeGraph != null && this.pathfindingSystem != null;

        if (!isInitialized)
        {
            Debug.LogError("[EnemySpawner] Failed to initialize enemy spawner.", this);
        }
    }

    public EnemyRuntime SpawnEnemy(EnemySpawnEvent spawnEvent)
    {
        if (spawnEvent == null)
        {
            Debug.LogError("[EnemySpawner] EnemySpawnEvent is required to spawn enemy.", this);
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

        if (!TryResolveSpawnCell(spawnEvent.SpawnPoint, out CombatGridCell spawnCell))
        {
            return null;
        }

        if (!combatGrid.TryCellToWorldBottomCenter(spawnCell, out Vector3 spawnPosition))
        {
            Debug.LogError($"[EnemySpawner] Failed to resolve spawn world position for cell {spawnCell.CellPosition}.", this);
            return null;
        }
        
        return SpawnEnemy(spawnEvent.EnemyDefinition, spawnPosition, spawnCell, spawnEvent.RouteId);
    }

    public EnemyRuntime SpawnEnemy(EnemyDefinition enemyDefinition, Vector3 spawnPosition, CombatGridCell spawnCell, string routeId)
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

        enemy.Initialize(enemyInstance, combatGrid, spawnCell.CellPosition, routeGraph, pathfindingSystem, routeId);
        if (!enemy.IsInitialized)
        {
            Debug.LogError("[EnemySpawner] Spawned enemy failed to initialize.", enemy);
            return null;
        }

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

    private bool TryResolveSpawnCell(EnemySpawnPoint spawnPoint, out CombatGridCell cell)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[EnemySpawner] EnemySpawnPoint is required to resolve spawn cell.", this);
            cell = null;
            return false;
        }

        return spawnPoint.TryGetSpawnCell(combatGrid, out cell);
    }
}
