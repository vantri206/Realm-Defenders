using UnityEngine;

public class RouteCheckpoint : MonoBehaviour
{
    [SerializeField] private string checkpointId;
    [SerializeField] private RouteCheckpointType checkpointType;

    // Grid reference for pathfinding
    private Vector3Int cellPosition;
    private Vector3 worldPosition;

    private bool isActive = false;

    public RouteCheckpointType CheckpointType => checkpointType;
    public string CheckpointId => checkpointId;
    public Vector3Int CellPosition => cellPosition;
    public Vector3 WorldPosition => worldPosition;

    public bool IsActive => isActive;

    public bool Initialize(CombatGrid combatGrid)
    {
        isActive = false;

        if (combatGrid == null)
        {
            Debug.LogError($"[CombatGrid] reference is null for RouteCheckpoint '{checkpointId}'.");
            return false;
        }

        if (!combatGrid.TryWorldToCellPosition(transform.position, out cellPosition))
        {
            Debug.LogError($"[RouteCheckpoint] '{checkpointId}' does not resolve to a built combat grid cell.");
            return false;
        }

        if (!combatGrid.TryCellToWorldCenter(cellPosition, out worldPosition))
        {
            Debug.LogError($"[RouteCheckpoint] '{checkpointId}' failed to get world position for cell {cellPosition}.");
            return false;
        }

        isActive = true;
        return true;
    }
}
