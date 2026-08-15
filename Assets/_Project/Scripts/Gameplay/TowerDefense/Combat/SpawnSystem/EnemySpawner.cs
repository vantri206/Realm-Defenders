using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private const float spawnSpreadCell = 0.18f;

    private CombatGrid combatGrid;
    private EnemyRouteGraph routeGraph;
    private UnitPathfindingSystem pathfindingSystem;
    private EnemyDepthSorter enemyDepthSorter;

    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        CacheDepthSorter();
    }

    public void Initialize(CombatGrid combatGrid, EnemyRouteGraph routeGraph, UnitPathfindingSystem pathfindingSystem, EnemyDepthSorter enemyDepthSorter)
    {
        CacheDepthSorter();
        this.combatGrid = combatGrid;
        this.routeGraph = routeGraph;
        this.pathfindingSystem = pathfindingSystem;
        this.enemyDepthSorter = enemyDepthSorter;
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

        if (!TryGetSpawnCell(spawnEvent.SpawnPoint, out CombatGridCell spawnCell))
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

        enemy.Initialize(enemyInstance, combatGrid, spawnCell.CellPosition, routeGraph, pathfindingSystem, routeId, enemyDepthSorter);
        if (!enemy.IsInitialized)
        {
            Debug.LogError("[EnemySpawner] Spawned enemy failed to initialize.", enemy);
            return null;
        }

        enemyDepthSorter.RegisterEnemy(enemy);

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

    private void CacheDepthSorter()
    {
        if (enemyDepthSorter == null)
        {
            enemyDepthSorter = GetComponent<EnemyDepthSorter>();
        }
    }

    private bool TryGetSpawnCell(EnemySpawnPoint spawnPoint, out CombatGridCell cell)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[EnemySpawner] EnemySpawnPoint is required to resolve spawn cell.", this);
            cell = null;
            return false;
        }

        return spawnPoint.TryGetSpawnCell(combatGrid, out cell);
    }

    private bool TryGetSpawnPosition(EnemyDefinition enemyDefinition, CombatGridCell spawnCell, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        if (enemyDefinition == null || spawnCell == null || combatGrid == null)
        {
            return false;
        }

        if (!combatGrid.TryCellToWorldCenter(spawnCell, out Vector3 cellCenter))
        {
            return false;
        }

        Vector3 cellSize = combatGrid.CellSize;
        float spreadRadius = Mathf.Min(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y)) * spawnSpreadCell;
        Vector2 centerSpread = Random.insideUnitCircle * spreadRadius;
        Vector3 enemyCenterSpawnPosition = cellCenter + (Vector3)centerSpread;
        spawnPosition = enemyCenterSpawnPosition - (Vector3)enemyDefinition.CenterOffset;
        return true;
    }
}
