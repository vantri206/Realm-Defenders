using UnityEngine;
using System;

public class EnemyPathfindingController : MonoBehaviour
{
    private UnitPathfindingSystem pathfindingSystem;

    private string routeId;
    private EnemyRouteDefinition route;

    private int currentCheckpointIndex = 0;
    private int targetCheckpointIndex = 0;

    private float cellTargetThreshold = 0.08f; // Threshold distance to consider the enemy has reached the center of the cell

    private bool hasActiveCell = false;
    private Vector3Int lastActiveCellPosition = Vector3Int.zero;
    private bool isMovingToCellCenter = false;
    private bool hasPreviousFlowDirection = false;
    private Vector2 previousFlowDirection = Vector2.zero;

    private bool hasPreviousEnemyPosition = false;
    private Vector3 previousEnemyPosition = Vector3.zero;

    public event Action OnReachedFinalCheckpoint;

    private void Awake()
    {
        CacheReferences();
    }

    public bool Initialize(EnemyRouteGraph routeGraph, UnitPathfindingSystem pathfindingSystem, string routeId)
    {
        this.routeId = routeId;
        this.pathfindingSystem = pathfindingSystem;

        if (routeGraph == null)
        {
            Debug.LogError("[EnemyPathfindingController] EnemyRouteGraph reference is missing.", this);
            return false;
        }

        route = routeGraph.GetRouteById(routeId);
        if (route == null)
        {
            Debug.LogError($"[EnemyPathfindingController] Route with ID '{routeId}' not found.", this);
            return false;
        }

        currentCheckpointIndex = 0;
        targetCheckpointIndex = route.CheckpointCount > 1 ? 1 : 0; // Start with the next checkpoint as the target
        hasActiveCell = false;
        isMovingToCellCenter = false;
        hasPreviousFlowDirection = false;
        previousFlowDirection = Vector2.zero;
        previousEnemyPosition = Vector3.zero;
        hasPreviousEnemyPosition = false;
        return true;
    }

    public RouteCheckpoint GetCurrentCheckpoint()
    {
        if (route == null || route.Checkpoints.Count == 0)
        {
            Debug.LogError("[EnemyPathfindingController] Route is not defined or has no checkpoints.");
            return null;
        }

        return route.Checkpoints[currentCheckpointIndex];
    }

    public RouteCheckpoint GetTargetCheckpoint()
    {
        if (route == null || route.Checkpoints.Count == 0)
        {
            Debug.LogError("[EnemyPathfindingController] Route is not defined or has no checkpoints.");
            return null;
        }

        return route.Checkpoints[targetCheckpointIndex];
    }

    public Vector2 GetCurrentMoveDirection(Vector3Int activeCellPosition, Vector3 activeCellWorldCenter, Vector3 enemyWorldPosition)
    {
        RouteCheckpoint targetCheckpoint = GetTargetCheckpoint();
        if (targetCheckpoint == null)
        {
            Debug.LogError("[EnemyPathfindingController] Target checkpoint is not defined.");
            return ChangeMoveDirection(Vector2.zero, enemyWorldPosition);
        }

        if (!hasActiveCell || activeCellPosition != lastActiveCellPosition)
        {
            UpdateActiveCell(activeCellPosition, targetCheckpoint.CellPosition);
        }
        
        if (isMovingToCellCenter)
        {
            if (!HasReachedTarget(activeCellWorldCenter, enemyWorldPosition))
            {
                return ChangeMoveDirection((activeCellWorldCenter - enemyWorldPosition).normalized, enemyWorldPosition);
            }
            
            isMovingToCellCenter = false; // Reached the center of the cell, now move towards the target checkpoint
        }

        if (activeCellPosition == targetCheckpoint.CellPosition)
        {
            if (!HasReachedTarget(targetCheckpoint.WorldPosition, enemyWorldPosition))
            {
                return ChangeMoveDirection((targetCheckpoint.WorldPosition - enemyWorldPosition).normalized, enemyWorldPosition);
            }
            
            bool isFinished = OnCheckpointReached(targetCheckpoint);
            if (isFinished)
            {
                return ChangeMoveDirection(Vector2.zero, enemyWorldPosition); // Enemy has reached the final checkpoint, stop moving
            }
            
            return GetCurrentMoveDirection(activeCellPosition, activeCellWorldCenter, enemyWorldPosition);
        }

        if (pathfindingSystem == null)
        {
            return ChangeMoveDirection(Vector2.zero, enemyWorldPosition);
        }

        Vector2 flowDirection = GetFlowFieldDirection(activeCellPosition, targetCheckpoint.CellPosition);
        UpdatePreviousFlowDirection(flowDirection);
        return ChangeMoveDirection(flowDirection, enemyWorldPosition);
    }

    private void UpdateActiveCell(Vector3Int activeCellPosition, Vector3Int targetCellPosition)
    {
        if (!hasActiveCell || activeCellPosition != lastActiveCellPosition)
        {
            Vector2 currentFlowDirection = GetFlowFieldDirection(activeCellPosition, targetCellPosition);
            isMovingToCellCenter = ShouldMoveToCellCenter(currentFlowDirection);
            UpdatePreviousFlowDirection(currentFlowDirection);

            lastActiveCellPosition = activeCellPosition;
            hasActiveCell = true;
        }
    }

    private Vector2 GetFlowFieldDirection(Vector3Int activeCellPosition, Vector3Int targetCellPosition)
    {
        if (pathfindingSystem == null)
        {
            return Vector2.zero;
        }

        return pathfindingSystem.TryGetFlowFieldDirection(activeCellPosition, targetCellPosition);
    }

    private bool ShouldMoveToCellCenter(Vector2 currentFlowDirection)
    {
        if (!hasPreviousFlowDirection || currentFlowDirection == Vector2.zero || previousFlowDirection == Vector2.zero)
        {
            return false;
        }

        float directionDot = Vector2.Dot(previousFlowDirection.normalized, currentFlowDirection.normalized);
        return directionDot <= 0.5f;
    }

    private void UpdatePreviousFlowDirection(Vector2 currentFlowDirection)
    {
        if (currentFlowDirection == Vector2.zero)
        {
            return;
        }

        previousFlowDirection = currentFlowDirection;
        hasPreviousFlowDirection = true;
    }

    public bool OnCheckpointReached(RouteCheckpoint checkpoint)
    {
        if (route == null || route.Checkpoints.Count == 0)
        {
            Debug.LogError("[EnemyPathfindingController] Route is not defined or has no checkpoints.");
            return false;
        }

        if (checkpoint == GetTargetCheckpoint())
        {
            if (targetCheckpointIndex < route.Checkpoints.Count - 1)
            {
                currentCheckpointIndex = targetCheckpointIndex;
                targetCheckpointIndex++;
                return false; // Not finished yet, there are more checkpoints to reach
            }
            else
            {
                Debug.Log($"[EnemyPathfindingController] Enemy has reached the final checkpoint '{checkpoint.CheckpointId}' in route '{route.RouteId}'.");
                currentCheckpointIndex = targetCheckpointIndex;
                OnReachedFinalCheckpoint?.Invoke();
                return true; // Finished, reached the final checkpoint
            }
        }
        return false;
    }

    private bool HasReachedTarget(Vector3 targetPosition, Vector3 enemyPosition)
    {
        Vector2 targetDistance = targetPosition - enemyPosition;

        if (targetDistance.sqrMagnitude <= cellTargetThreshold * cellTargetThreshold)
        {
            return true;
        }

        if (!hasPreviousEnemyPosition)
        {
            return false;
        }

        Vector2 previousToTarget = targetPosition - previousEnemyPosition;
        return Vector2.Dot(previousToTarget, targetDistance) <= 0f;
    }

    private Vector2 ChangeMoveDirection(Vector2 direction, Vector3 enemyWorldPosition)
    {
        previousEnemyPosition = enemyWorldPosition;
        hasPreviousEnemyPosition = true;
        return direction;
    }

    public void ResetPathfinding()
    {
        if (route == null || route.Checkpoints.Count == 0)
        {
            Debug.LogError("[EnemyPathfindingController] Cannot reset pathfinding because no route is defined or the route has no checkpoints.");
            return;
        }

        currentCheckpointIndex = 0;
        targetCheckpointIndex = route.CheckpointCount > 1 ? 1 : 0; // Reset to the first checkpoint and next checkpoint
        hasActiveCell = false;
        isMovingToCellCenter = false;
        hasPreviousFlowDirection = false;
        previousFlowDirection = Vector2.zero;
        previousEnemyPosition = Vector3.zero;
        hasPreviousEnemyPosition = false;
    }

    private void CacheReferences()
    {
        if (pathfindingSystem == null)
        {
            pathfindingSystem = FindAnyObjectByType<UnitPathfindingSystem>();
        }
    }
}
