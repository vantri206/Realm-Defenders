using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyRouteDefinition
{
    [SerializeField] private string routeId;
    [SerializeField] private List<EnemyRouteCheckpointDefinition> checkpoints = new List<EnemyRouteCheckpointDefinition>();

    public string RouteId => routeId;
    public IReadOnlyList<EnemyRouteCheckpointDefinition> Checkpoints => checkpoints;
    public int CheckpointCount => checkpoints != null ? checkpoints.Count : 0;

    public EnemyRouteDefinition() { }

    public EnemyRouteDefinition(string routeId, List<EnemyRouteCheckpointDefinition> checkpoints)
    {
        this.routeId = routeId;
        if (checkpoints != null)
        {
            this.checkpoints = checkpoints;
        }
        else
        {
            this.checkpoints = new List<EnemyRouteCheckpointDefinition>();
        }
    }
}
