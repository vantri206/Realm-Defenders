using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyRouteAuthoring
{
    [SerializeField] private string routeId;
    [SerializeField] private List<EnemyRouteCheckpointAuthoring> checkpoints = new List<EnemyRouteCheckpointAuthoring>();

    public string RouteId => routeId;
    public IReadOnlyList<EnemyRouteCheckpointAuthoring> Checkpoints => checkpoints;
    public int CheckpointCount => checkpoints != null ? checkpoints.Count : 0;
    public bool HasCheckpoints => CheckpointCount > 0;
    public EnemyRouteCheckpointAuthoring StartCheckpoint => GetCheckpoint(0);
    public EnemyRouteCheckpointAuthoring EndCheckpoint => GetCheckpoint(CheckpointCount - 1);

    public EnemyRouteCheckpointAuthoring GetCheckpoint(int index)
    {
        if (TryGetCheckpoint(index, out EnemyRouteCheckpointAuthoring checkpoint))
        {
            return checkpoint;
        }

        Debug.LogError($"[EnemyRouteAuthoring] Index {index} is out of bounds for route '{routeId}' with {CheckpointCount} checkpoints.");
        return null;
    }

    public bool TryGetCheckpoint(int index, out EnemyRouteCheckpointAuthoring checkpoint)
    {
        checkpoint = null;

        if (checkpoints == null || index < 0 || index >= checkpoints.Count)
        {
            return false;
        }

        checkpoint = checkpoints[index];
        return checkpoint != null;
    }

    public int GetCheckpointIndex(EnemyRouteCheckpointAuthoring checkpoint)
    {
        if (checkpoint == null || checkpoints == null)
        {
            return -1;
        }

        return checkpoints.IndexOf(checkpoint);
    }

    public bool ContainsCheckpoint(EnemyRouteCheckpointAuthoring checkpoint)
    {
        return GetCheckpointIndex(checkpoint) >= 0;
    }

    public bool AddCheckpoint(EnemyRouteCheckpointAuthoring checkpoint)
    {
        if (checkpoint == null)
        {
            return false;
        }

        if (checkpoints == null)
        {
            checkpoints = new List<EnemyRouteCheckpointAuthoring>();
        }

        checkpoints.Add(checkpoint);
        return true;
    }

    public bool InsertCheckpoint(int index, EnemyRouteCheckpointAuthoring checkpoint)
    {
        if (checkpoint == null)
        {
            return false;
        }

        if (checkpoints == null)
        {
            checkpoints = new List<EnemyRouteCheckpointAuthoring>();
        }
        
        int insertIndex = Mathf.Clamp(index, 0, checkpoints.Count);
        checkpoints.Insert(insertIndex, checkpoint);
        return true;
    }

    public bool RemoveCheckpoint(EnemyRouteCheckpointAuthoring checkpoint)
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
