using System.Collections.Generic;
using UnityEngine;

public class EnemyRouteGraph : MonoBehaviour
{
    private readonly Dictionary<string, EnemyRouteDefinition> routes = new Dictionary<string, EnemyRouteDefinition>();

    public bool InitializeRoutes(CombatGrid combatGrid, IReadOnlyList<EnemyRouteDefinition> routeDefinitions)
    {
        routes.Clear();

        if (combatGrid == null || routeDefinitions == null)
        {
            Debug.LogError("[EnemyRouteGraph] CombatGrid and route definitions are required.", this);
            return false;
        }

        for (int i = 0; i < routeDefinitions.Count; i++)
        {
            EnemyRouteDefinition route = routeDefinitions[i];
            if (route == null || string.IsNullOrWhiteSpace(route.RouteId) || route.CheckpointCount < 2 || routes.ContainsKey(route.RouteId))
            {
                Debug.LogError($"[EnemyRouteGraph] Route at index {i} is invalid.", this);
                return false;
            }

            for (int checkpointIndex = 0; checkpointIndex < route.CheckpointCount; checkpointIndex++)
            {
                EnemyRouteCheckpointDefinition checkpoint = route.Checkpoints[checkpointIndex];
                if (checkpoint == null || !combatGrid.TryGetCell(checkpoint.CellPosition, out _))
                {
                    Debug.LogError($"[EnemyRouteGraph] Route '{route.RouteId}' has a checkpoint outside the combat grid.", this);
                    return false;
                }
            }

            routes.Add(route.RouteId, route);
        }

        return true;
    }

    public EnemyRouteDefinition GetRouteById(string routeId)
    {
        routes.TryGetValue(routeId, out EnemyRouteDefinition route);
        return route;
    }
}
