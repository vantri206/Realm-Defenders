using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CombatMapData
{
    [SerializeField] private List<CombatGridCellDefinition> gridCells = new List<CombatGridCellDefinition>();
    [SerializeField] private List<EnemyRouteDefinition> routes = new List<EnemyRouteDefinition>();
    [SerializeField] private List<CombatSpawnPointDefinition> spawnPoints = new List<CombatSpawnPointDefinition>();

    public IReadOnlyList<CombatGridCellDefinition> GridCells => gridCells;
    public IReadOnlyList<EnemyRouteDefinition> Routes => routes;
    public IReadOnlyList<CombatSpawnPointDefinition> SpawnPoints => spawnPoints;

    public CombatMapData() { }

    public CombatMapData(List<CombatGridCellDefinition> gridCells,
                         List<EnemyRouteDefinition> routes,
                         List<CombatSpawnPointDefinition> spawnPoints)
    {
        if (gridCells != null)
        {
            this.gridCells = gridCells;
        }
        else
        {
            this.gridCells = new List<CombatGridCellDefinition>();
        }

        if (routes != null)
        {
            this.routes = routes;
        }
        else
        {
            this.routes = new List<EnemyRouteDefinition>();
        }

        if (spawnPoints != null)
        {
            this.spawnPoints = spawnPoints;
        }
        else
        {
            this.spawnPoints = new List<CombatSpawnPointDefinition>();
        }
    }
}
