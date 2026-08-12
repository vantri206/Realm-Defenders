using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyRouteDefinition
{
    [SerializeField] private string routeId;
    [SerializeField] private List<RouteCheckpoint> checkpoints = new List<RouteCheckpoint>();

    public string RouteId => routeId;
    public IReadOnlyList<RouteCheckpoint> Checkpoints => checkpoints;
    public int CheckpointCount => checkpoints != null ? checkpoints.Count : 0;
    public bool HasCheckpoints => CheckpointCount > 0;
    public RouteCheckpoint StartCheckpoint => GetCheckpoint(0);
    public RouteCheckpoint EndCheckpoint => GetCheckpoint(CheckpointCount - 1);

    public RouteCheckpoint GetCheckpoint(int index)
    {
        if (TryGetCheckpoint(index, out RouteCheckpoint checkpoint))
        {
            return checkpoint;
        }

        Debug.LogError($"[EnemyRouteDefinition] Index {index} is out of bounds for route '{routeId}' with {CheckpointCount} checkpoints.");
        return null;
    }

    public bool TryGetCheckpoint(int index, out RouteCheckpoint checkpoint)
    {
        checkpoint = null;

        if (checkpoints == null || index < 0 || index >= checkpoints.Count)
        {
            return false;
        }

        checkpoint = checkpoints[index];
        return checkpoint != null;
    }

    public int GetCheckpointIndex(RouteCheckpoint checkpoint)
    {
        if (checkpoint == null || checkpoints == null)
        {
            return -1;
        }

        return checkpoints.IndexOf(checkpoint);
    }

    public bool ContainsCheckpoint(RouteCheckpoint checkpoint)
    {
        return GetCheckpointIndex(checkpoint) >= 0;
    }

    public bool AddCheckpoint(RouteCheckpoint checkpoint)
    {
        if (checkpoint == null)
        {
            return false;
        }

        if (checkpoints == null)
        {
            checkpoints = new List<RouteCheckpoint>();
        }

        checkpoints.Add(checkpoint);
        return true;
    }

    public bool InsertCheckpoint(int index, RouteCheckpoint checkpoint)
    {
        if (checkpoint == null)
        {
            return false;
        }

        if (checkpoints == null)
        {
            checkpoints = new List<RouteCheckpoint>();
        }
        
        int insertIndex = Mathf.Clamp(index, 0, checkpoints.Count);
        checkpoints.Insert(insertIndex, checkpoint);
        return true;
    }

    public bool RemoveCheckpoint(RouteCheckpoint checkpoint)
    {
        if (checkpoint == null || checkpoints == null)
        {
            return false;
        }
        return checkpoints.Remove(checkpoint);
    }

    public bool RemoveCheckpointAt(int index)
    {
        if (checkpoints == null || index < 0 || index >= checkpoints.Count)
        {
            return false;
        }

        checkpoints.RemoveAt(index);
        return true;
    }
}
