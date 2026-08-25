using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private const float spawnSpreadCell = 0.18f;

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

    public void Initialize(UnitCombatContext combatContext, EnemyRouteGraph routeGraph, EnemyDepthSorter enemyDepthSorter, StageSystem stageSystem)
    {
        this.combatContext = combatContext;
        this.routeGraph = routeGraph;
        this.enemyDepthSorter = enemyDepthSorter;
        this.stageSystem = stageSystem;
        isInitialized = this.combatContext != null && this.combatContext.IsValid && this.enemyDepthSorter != null && this.stageSystem != null;

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

        enemy.Initialize(enemyInstance, combatContext, routeGraph, spawnCell.CellPosition, routeId, enemyDepthSorter);
        if (!enemy.IsInitialized)
        {
            Debug.LogError("[EnemySpawner] Spawned enemy failed to initialize.", enemy);
            return null;
        }

        enemyDepthSorter.RegisterEnemy(enemy);
        stageSystem.RegisterEnemy(enemy, new EnemyTrackingData(enemyInstance.IsObjectiveEnemy, GameplayConstants.NORMAL_ENEMY_LIVES_DAMAGE, enemyInstance.Definition.MeatReward));
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

    private bool TryGetSpawnCell(EnemySpawnPoint spawnPoint, out CombatGridCell cell)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[EnemySpawner] EnemySpawnPoint is required to resolve spawn cell.", this);
            cell = null;
            return false;
        }

        return spawnPoint.TryGetSpawnCell(combatContext.CombatGrid, out cell);
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
        float spreadRadius = Mathf.Min(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y)) * spawnSpreadCell;
        Vector2 centerSpread = Random.insideUnitCircle * spreadRadius;
        Vector3 enemyCenterSpawnPosition = cellCenter + (Vector3)centerSpread;
        spawnPosition = enemyCenterSpawnPosition - (Vector3)enemyDefinition.NavigationOffset;
        return true;
    }

    private void RegisterEnemyEvents(EnemyRuntime enemy)
    {
        if (enemy == null)
        {
            return;
        }

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
