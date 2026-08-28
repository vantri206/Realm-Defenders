using UnityEngine;

public class EnemyRouteCheckpointAuthoring : MonoBehaviour
{
    [SerializeField] private string checkpointId;
    [SerializeField] private EnemyRouteCheckpointType checkpointType;

    public EnemyRouteCheckpointType CheckpointType => checkpointType;
    public string CheckpointId => checkpointId;
}
