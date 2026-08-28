using System;
using UnityEngine;

[Serializable]
public class EnemyRouteCheckpointDefinition
{
    [SerializeField] private string checkpointId;
    [SerializeField] private EnemyRouteCheckpointType checkpointType;
    [SerializeField] private Vector3Int cellPosition;

    public string CheckpointId => checkpointId;
    public EnemyRouteCheckpointType CheckpointType => checkpointType;
    public Vector3Int CellPosition => cellPosition;

    public EnemyRouteCheckpointDefinition() { }

    public EnemyRouteCheckpointDefinition(string checkpointId, EnemyRouteCheckpointType checkpointType, Vector3Int cellPosition)
    {
        this.checkpointId = checkpointId;
        this.checkpointType = checkpointType;
        this.cellPosition = cellPosition;
    }
}
