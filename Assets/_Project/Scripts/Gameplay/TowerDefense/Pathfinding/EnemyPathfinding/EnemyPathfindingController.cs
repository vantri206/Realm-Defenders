using UnityEngine;
using System;

public class EnemyPathfindingController : MonoBehaviour
{
    [SerializeField] private UnitSeparationResolver separationResolver;
    [SerializeField] private UnitSeparationSettings separationSettings;

    private UnitPathfindingSystem pathfindingSystem;

    private string routeId;
    private EnemyRouteDefinition route;

    private int currentCheckpointIndex = 0;
    private int targetCheckpointIndex = 0;

    private float cellTargetThreshold = GameplayConstants.CELL_TARGET_THRESHOLD; // Threshold distance to consider the enemy has reached the center of the cell

    private float flowWeight = 1.0f;

    private const float cornerTurnMinDot = -0.75f;
    private const float cornerTurnMaxDot = 0.01f;
    private const float cornerStartDepth = 0f;
    private const float cornerFinishDepth = 0.8f;
    private const float cornerMinTurnDepth = 0.05f;
    private bool hasPreviousFlowDirection = false;
    private Vector2 previousFlowDirection = Vector2.zero;
    private bool isCornerSmoothing = false;
    private Vector3Int cornerCellPosition = Vector3Int.zero;
    private Vector2 cornerEntryDirection = Vector2.zero;
    private Vector2 cornerExitDirection = Vector2.zero;

    private bool hasPreviousEnemyPosition = false;
    private Vector3 previousEnemyPosition = Vector3.zero;
    private float pathProgressScore = 0f;
    private bool hasReachedFinalCheckpoint = false;

    public event Action OnReachedFinalCheckpoint;
    public float PathProgressScore => pathProgressScore;

    private void Awake()
    {
        CacheReferences();
    }

    public bool Initialize(EnemyRouteGraph routeGraph, UnitPathfindingSystem pathfindingSystem, string routeId, Action actionOnEscaped)
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

        if (actionOnEscaped != null)
        {
            OnReachedFinalCheckpoint += actionOnEscaped;
        }

        currentCheckpointIndex = 0;
        targetCheckpointIndex = route.CheckpointCount > 1 ? 1 : 0; // Start with the next checkpoint as the target
        ResetCornerSmoothing();
        previousEnemyPosition = Vector3.zero;
        hasPreviousEnemyPosition = false;
        pathProgressScore = 0f;
        hasReachedFinalCheckpoint = false;
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

    public Vector2 GetCurrentMoveDirection(EnemyRuntime enemy, Vector3Int activeCellPosition, Vector3 activeCellWorldCenter, Vector3 enemyCenterPosition)
    {
        if (hasReachedFinalCheckpoint)
        {
            return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
        }

        RouteCheckpoint targetCheckpoint = GetTargetCheckpoint();
        if (targetCheckpoint == null)
        {
            Debug.LogError("[EnemyPathfindingController] Target checkpoint is not defined.");
            return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
        }

        if (activeCellPosition == targetCheckpoint.CellPosition)
        {
            if (IsFinalCheckpoint(targetCheckpointIndex))
            {
                if (!HasReachedTarget(targetCheckpoint.WorldPosition, enemyCenterPosition))
                {
                    ResetCornerSmoothing();
                    Vector2 finalTargetDirection = GetDirectionToTarget(enemyCenterPosition, targetCheckpoint.WorldPosition);
                    UpdatePathProgress(enemyCenterPosition);
                    return ChangeMoveDirection(finalTargetDirection, enemyCenterPosition);
                }

                OnCheckpointReached(targetCheckpoint);
                UpdatePathProgress(enemyCenterPosition);
                return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
            }

            OnCheckpointReached(targetCheckpoint);
            targetCheckpoint = GetTargetCheckpoint();
            if (targetCheckpoint == null)
            {
                UpdatePathProgress(enemyCenterPosition);
                return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
            }
        }

        if (pathfindingSystem == null)
        {
            return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
        }

        UnitMovementType movementType = enemy != null ? enemy.MovementType : UnitMovementType.Ground;
        Vector2 flowDirection = GetFlowFieldDirection(activeCellPosition, targetCheckpoint.CellPosition, movementType);
        Vector3 cellSize = Vector3.one; // Default cell size if combat grid is not available
        if (enemy != null && enemy.CombatGrid != null)
        {
            cellSize = enemy.CombatGrid.CellSize;
        }
        Vector2 smoothedFlowDirection = ResolveCornerSmoothedDirection(flowDirection, activeCellPosition, activeCellWorldCenter, enemyCenterPosition, cellSize);

        Vector2 currentSeparationDirection = GetSeparationDirection(enemy);
        Vector2 moveDirection = BlendMoveDirection(smoothedFlowDirection, currentSeparationDirection);
        UpdatePathProgress(enemyCenterPosition);
        return ChangeMoveDirection(moveDirection, enemyCenterPosition);
    }

    private Vector2 GetFlowFieldDirection(Vector3Int activeCellPosition, Vector3Int targetCellPosition, UnitMovementType movementType)
    {
        if (pathfindingSystem == null)
        {
            return Vector2.zero;
        }

        return pathfindingSystem.TryGetFlowFieldDirection(activeCellPosition, targetCellPosition, movementType);
    }

    private Vector2 GetSeparationDirection(EnemyRuntime enemy)
    {
        if (enemy == null || separationResolver == null || enemy.CombatGrid == null)
        {
            return Vector2.zero;
        }

        return separationResolver.GetSeparationDirection(enemy, enemy.CombatGrid, separationSettings);
    }

    private Vector2 BlendMoveDirection(Vector2 flowDirection, Vector2 separationDirection)
    {
        Vector2 weightedSeparation = Vector2.zero;
        if (separationResolver != null)
        {
            weightedSeparation = separationResolver.ApplyWeight(separationDirection, separationSettings);
        }

        float flowDirectionWeight = Mathf.Max(0f, flowWeight);

        Vector2 moveDirection = flowDirection.normalized * flowDirectionWeight + weightedSeparation;

        return ResolvedDirection(moveDirection);
    }

    private Vector2 ResolveCornerSmoothedDirection(Vector2 flowDirection, Vector3Int activeCellPosition,
                                                Vector3 activeCellWorldCenter, Vector3 enemyCenterPosition, Vector3 cellSize)
    {
        Vector2 resolvedFlowDirection = ResolvedDirection(flowDirection);
        if (resolvedFlowDirection == Vector2.zero)
        {
            ResetCornerSmoothing();
            return Vector2.zero;
        }

        if (!hasPreviousFlowDirection)
        {
            previousFlowDirection = resolvedFlowDirection;
            hasPreviousFlowDirection = true;
            return resolvedFlowDirection;
        }

        if (ShouldStartCornerSmoothing(resolvedFlowDirection))
        {
            StartCornerSmoothing(activeCellPosition, previousFlowDirection, resolvedFlowDirection);
        }

        previousFlowDirection = resolvedFlowDirection;

        if (!isCornerSmoothing)
        {
            return resolvedFlowDirection;
        }

        if (activeCellPosition != cornerCellPosition)
        {
            isCornerSmoothing = false;
            return resolvedFlowDirection;
        }

        float turnDistance = GetCornerTurnDistance(activeCellWorldCenter, enemyCenterPosition, cellSize);
        Vector2 smoothedDirection = Vector2.Lerp(cornerEntryDirection, cornerExitDirection, turnDistance);
        if (turnDistance >= 1f)
        {
            isCornerSmoothing = false;
        }

        return ResolvedDirection(smoothedDirection);
    }

    private bool ShouldStartCornerSmoothing(Vector2 currentFlowDirection)
    {
        if (isCornerSmoothing && Vector2.Dot(cornerExitDirection, currentFlowDirection) > 0.99f)
        {
            return false;
        }

        float directionDot = Vector2.Dot(previousFlowDirection.normalized, currentFlowDirection.normalized);
        bool shouldSmoothCorner = directionDot >= cornerTurnMinDot && directionDot <= cornerTurnMaxDot;
        if (!shouldSmoothCorner)
        {
            isCornerSmoothing = false;
        }

        return shouldSmoothCorner;
    }

    private void StartCornerSmoothing(Vector3Int activeCellPosition, Vector2 entryDirection, Vector2 exitDirection)
    {
        isCornerSmoothing = true;
        cornerCellPosition = activeCellPosition;
        cornerEntryDirection = entryDirection;
        cornerExitDirection = exitDirection;
    }

    private float GetCornerTurnDistance(Vector3 activeCellWorldCenter, Vector3 enemyCenterPosition, Vector3 cellSize)
    {
        float entryHalfDistanceCorner = GetEntryHalfDistanceCorner(cornerEntryDirection, cellSize);
        float entryCellLength = entryHalfDistanceCorner * 2f;
        if (entryCellLength <= Mathf.Epsilon)
        {
            return 1f;
        }

        Vector2 cellCenter = activeCellWorldCenter;
        Vector2 enemyCenter = enemyCenterPosition;
        Vector2 entryBoundaryCenter = cellCenter - cornerEntryDirection * entryHalfDistanceCorner;
        float depth = Vector2.Dot(enemyCenter - entryBoundaryCenter, cornerEntryDirection);
        float normalizedDepth = Mathf.Clamp01(depth / entryCellLength);

        float minDepth = Mathf.Clamp01(cornerMinTurnDepth / entryCellLength);
        float startDepth = Mathf.Clamp01(Mathf.Max(cornerStartDepth, minDepth));
        float finishDepth = Mathf.Clamp01(Mathf.Max(cornerFinishDepth, startDepth + minDepth));
        if (finishDepth <= startDepth + Mathf.Epsilon)
        {
            return normalizedDepth >= finishDepth ? 1f : 0f;
        }

        float progress = Mathf.InverseLerp(startDepth, finishDepth, normalizedDepth);
        return Mathf.SmoothStep(0f, 1f, progress);
    }

    private float GetEntryHalfDistanceCorner(Vector2 direction, Vector3 cellSize)
    {
        Vector2 halfCellSize = new Vector2(Mathf.Abs(cellSize.x) * 0.5f, Mathf.Abs(cellSize.y) * 0.5f);
        return Mathf.Abs(direction.x) * halfCellSize.x + Mathf.Abs(direction.y) * halfCellSize.y;
    }

    private void ResetCornerSmoothing()
    {
        hasPreviousFlowDirection = false;
        previousFlowDirection = Vector2.zero;
        isCornerSmoothing = false;
        cornerCellPosition = Vector3Int.zero;
        cornerEntryDirection = Vector2.zero;
        cornerExitDirection = Vector2.zero;
    }

    private void UpdatePathProgress(Vector3 enemyCenterPosition)
    {
        if (route == null || route.CheckpointCount == 0)
        {
            pathProgressScore = 0f;
            return;
        }

        RouteCheckpoint currentCheckpoint = GetCurrentCheckpoint();
        RouteCheckpoint targetCheckpoint = GetTargetCheckpoint();
        if (currentCheckpoint == null || targetCheckpoint == null)
        {
            pathProgressScore = currentCheckpointIndex;
            return;
        }

        float segmentLength = Vector2.Distance(currentCheckpoint.WorldPosition, targetCheckpoint.WorldPosition);
        float segmentProgress = 0f;

        if (segmentLength > Mathf.Epsilon)
        {
            float targetDistance = Vector2.Distance(enemyCenterPosition, targetCheckpoint.WorldPosition);
            segmentProgress = 1f - Mathf.Clamp01(targetDistance / segmentLength);
        }

        pathProgressScore = currentCheckpointIndex + segmentProgress;
    }

    private Vector2 GetDirectionToTarget(Vector3 enemyCenterPosition, Vector3 targetWorldPosition)
    {
        Vector2 direction = targetWorldPosition - enemyCenterPosition;
        return ResolvedDirection(direction);
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
                hasReachedFinalCheckpoint = true;
                ResetCornerSmoothing();
                Debug.Log($"[EnemyPathfindingController] Enemy has reached the final checkpoint '{checkpoint.CheckpointId}' in route '{route.RouteId}'.");
                currentCheckpointIndex = targetCheckpointIndex;
                OnReachedFinalCheckpoint?.Invoke();
                return true; // Finished, reached the final checkpoint
            }
        }
        return false;
    }

    private bool IsFinalCheckpoint(int checkpointIndex)
    {
        return route != null && checkpointIndex >= route.CheckpointCount - 1;
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

    private Vector2 ResolvedDirection(Vector2 direction)
    {
        return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.zero;
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
        ResetCornerSmoothing();
        previousEnemyPosition = Vector3.zero;
        hasPreviousEnemyPosition = false;
        pathProgressScore = 0f;
        hasReachedFinalCheckpoint = false;
    }

    private void CacheReferences()
    {
        if (pathfindingSystem == null)
        {
            pathfindingSystem = FindAnyObjectByType<UnitPathfindingSystem>();
        }

        if (separationResolver == null)
        {
            separationResolver = FindAnyObjectByType<UnitSeparationResolver>();
        }
    }
}
