using UnityEngine;
using System;

public class EnemyPathfindingController : MonoBehaviour
{
    private UnitPathfindingSystem pathfindingSystem;
    private CombatGrid combatGrid;

    private EnemyRouteDefinition route;

    private int currentCheckpointIndex = 0;
    private int targetCheckpointIndex = 0;

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
    private bool useFlowField = false;

    public event Action OnReachedFinalCheckpoint;
    public float PathProgressScore => pathProgressScore;

    public bool Initialize(EnemyRouteGraph routeGraph, UnitPathfindingSystem pathfindingSystem, CombatGrid combatGrid,
                           string routeId, Action actionOnEscaped)
    {
        this.pathfindingSystem = pathfindingSystem;
        this.combatGrid = combatGrid;

        if (routeGraph == null || combatGrid == null)
        {
            Debug.LogError("[EnemyPathfindingController] EnemyRouteGraph and CombatGrid references are required.", this);
            return false;
        }

        route = routeGraph.GetRouteById(routeId);
        if (route == null)
        {
            Debug.LogError($"[EnemyPathfindingController] Route with ID '{routeId}' not found.", this);
            return false;
        }

        OnReachedFinalCheckpoint = actionOnEscaped;

        currentCheckpointIndex = 0;
        targetCheckpointIndex = route.CheckpointCount > 1 ? 1 : 0; // Start with the next checkpoint as the target
        ResetCornerSmoothing();
        previousEnemyPosition = Vector3.zero;
        hasPreviousEnemyPosition = false;
        pathProgressScore = 0f;
        hasReachedFinalCheckpoint = false;
        useFlowField = false;
        return true;
    }

    public EnemyRouteCheckpointDefinition GetCurrentCheckpoint()
    {
        if (route == null || route.Checkpoints.Count == 0)
        {
            Debug.LogError("[EnemyPathfindingController] Route is not defined or has no checkpoints.");
            return null;
        }

        return route.Checkpoints[currentCheckpointIndex];
    }

    public EnemyRouteCheckpointDefinition GetTargetCheckpoint()
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

        EnemyRouteCheckpointDefinition targetCheckpoint = GetTargetCheckpoint();
        if (targetCheckpoint == null)
        {
            Debug.LogError("[EnemyPathfindingController] Target checkpoint is not defined.");
            return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
        }

        EnemyRouteCheckpointDefinition currentCheckpoint = GetCurrentCheckpoint();
        if (currentCheckpoint == null)
        {
            Debug.LogError("[EnemyPathfindingController] Current checkpoint is not defined.");
            return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
        }

        Vector3 cellSize = Vector3.one;
        if (enemy != null && enemy.CombatGrid != null)
        {
            cellSize = enemy.CombatGrid.CellSize;
        }

        Vector2 segmentDirection = GetCurrentMoveDirection(currentCheckpoint, targetCheckpoint);
        if (!TryGetCheckpointWorldPosition(targetCheckpoint, out Vector3 targetWorldPosition))
        {
            return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
        }

        if (HasCrossedCheckpointLine(targetWorldPosition, segmentDirection, previousEnemyPosition, enemyCenterPosition, cellSize))
        {
            if (OnCheckpointReached(targetCheckpoint))
            {
                UpdatePathProgress(enemyCenterPosition);
                return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
            }

            currentCheckpoint = GetCurrentCheckpoint();
            targetCheckpoint = GetTargetCheckpoint();
            if (currentCheckpoint == null || targetCheckpoint == null)
            {
                UpdatePathProgress(enemyCenterPosition);
                return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
            }

            segmentDirection = GetCurrentMoveDirection(currentCheckpoint, targetCheckpoint);
        }

        UnitMovementType movementType = enemy != null ? enemy.MovementType : UnitMovementType.Ground;
        if (movementType == UnitMovementType.Flying)
        {
            Vector2 directDirection = GetDirectionToCheckpoint(targetCheckpoint, enemyCenterPosition);
            if (activeCellPosition == targetCheckpoint.CellPosition)
            {
                UpdatePathProgress(enemyCenterPosition);
                return ChangeMoveDirection(directDirection, enemyCenterPosition);
            }

            if (!useFlowField && IsNextCellBlocked(enemy, activeCellPosition, enemyCenterPosition, directDirection))
            {
                useFlowField = true;
                ResetCornerSmoothing();
            }

            if (!useFlowField)
            {
                UpdatePathProgress(enemyCenterPosition);
                return ChangeMoveDirection(directDirection, enemyCenterPosition);
            }
        }

        if (activeCellPosition == targetCheckpoint.CellPosition)
        {
            UpdatePathProgress(enemyCenterPosition);
            return ChangeMoveDirection(segmentDirection, enemyCenterPosition);
        }

        if (pathfindingSystem == null)
        {
            return ChangeMoveDirection(Vector2.zero, enemyCenterPosition);
        }

        Vector2 flowDirection = GetFlowFieldDirection(activeCellPosition, targetCheckpoint.CellPosition, movementType);
        Vector2 resolvedFlowDirection = movementType == UnitMovementType.Flying ? ResolvedDirection(flowDirection) : ResolveCornerSmoothedDirection(flowDirection, activeCellPosition, activeCellWorldCenter, enemyCenterPosition, cellSize);

        UpdatePathProgress(enemyCenterPosition);
        return ChangeMoveDirection(resolvedFlowDirection, enemyCenterPosition);
    }

    private Vector2 GetFlowFieldDirection(Vector3Int activeCellPosition, Vector3Int targetCellPosition, UnitMovementType movementType)
    {
        if (pathfindingSystem == null)
        {
            return Vector2.zero;
        }

        return pathfindingSystem.TryGetFlowFieldDirection(activeCellPosition, targetCellPosition, movementType);
    }

    private Vector2 ResolveCornerSmoothedDirection(Vector2 flowDirection, Vector3Int activeCellPosition, Vector3 activeCellWorldCenter, Vector3 enemyCenterPosition, Vector3 cellSize)
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

        EnemyRouteCheckpointDefinition currentCheckpoint = GetCurrentCheckpoint();
        EnemyRouteCheckpointDefinition targetCheckpoint = GetTargetCheckpoint();
        if (currentCheckpoint == null || targetCheckpoint == null)
        {
            pathProgressScore = currentCheckpointIndex;
            return;
        }

        if (!TryGetCheckpointWorldPosition(currentCheckpoint, out Vector3 currentWorldPosition) ||
            !TryGetCheckpointWorldPosition(targetCheckpoint, out Vector3 targetWorldPosition))
        {
            pathProgressScore = currentCheckpointIndex;
            return;
        }

        float segmentLength = Vector2.Distance(currentWorldPosition, targetWorldPosition);
        float segmentProgress = 0f;

        if (segmentLength > Mathf.Epsilon)
        {
            float targetDistance = Vector2.Distance(enemyCenterPosition, targetWorldPosition);
            segmentProgress = 1f - Mathf.Clamp01(targetDistance / segmentLength);
        }

        pathProgressScore = currentCheckpointIndex + segmentProgress;
    }

    private Vector2 GetCurrentMoveDirection(EnemyRouteCheckpointDefinition currentCheckpoint, EnemyRouteCheckpointDefinition targetCheckpoint)
    {
        if (currentCheckpoint == null || targetCheckpoint == null)
        {
            return Vector2.zero;
        }

        if (!TryGetCheckpointWorldPosition(currentCheckpoint, out Vector3 currentWorldPosition) ||
            !TryGetCheckpointWorldPosition(targetCheckpoint, out Vector3 targetWorldPosition))
        {
            return Vector2.zero;
        }

        Vector2 direction = targetWorldPosition - currentWorldPosition;
        return ResolvedDirection(direction);
    }

    private Vector2 GetDirectionToCheckpoint(EnemyRouteCheckpointDefinition targetCheckpoint, Vector3 enemyPosition)
    {
        if (targetCheckpoint == null)
        {
            return Vector2.zero;
        }

        if (!TryGetCheckpointWorldPosition(targetCheckpoint, out Vector3 targetWorldPosition))
        {
            return Vector2.zero;
        }

        Vector2 direction = targetWorldPosition - enemyPosition;
        return ResolvedDirection(direction);
    }

    private bool TryGetCheckpointWorldPosition(EnemyRouteCheckpointDefinition checkpoint, out Vector3 worldPosition)
    {
        if (checkpoint == null || combatGrid == null)
        {
            worldPosition = Vector3.zero;
            return false;
        }

        return combatGrid.TryCellToWorldCenter(checkpoint.CellPosition, out worldPosition);
    }

    private bool IsNextCellBlocked(EnemyRuntime enemy, Vector3Int activeCellPosition, Vector3 enemyPosition, Vector2 moveDirection)
    {
        if (enemy == null || enemy.CombatGrid == null || moveDirection == Vector2.zero)
        {
            return false;
        }

        CombatGrid combatGrid = enemy.CombatGrid;
        Grid grid = combatGrid.Grid;
        if (grid == null)
        {
            return false;
        }

        Vector3 localPosition = grid.transform.InverseTransformPoint(enemyPosition);
        Vector3 localDirection = grid.transform.InverseTransformVector(moveDirection);
        Vector3 cellStride = grid.cellSize + grid.cellGap;
        if (Mathf.Abs(cellStride.x) <= Mathf.Epsilon || Mathf.Abs(cellStride.y) <= Mathf.Epsilon)
        {
            return false;
        }

        int stepX = localDirection.x > 0f ? 1 : localDirection.x < 0f ? -1 : 0;
        int stepY = localDirection.y > 0f ? 1 : localDirection.y < 0f ? -1 : 0;
        Vector3 currentCellOrigin = grid.CellToLocal(activeCellPosition);

        float nextBoundaryX = stepX > 0 ? currentCellOrigin.x + cellStride.x : currentCellOrigin.x;
        float nextBoundaryY = stepY > 0 ? currentCellOrigin.y + cellStride.y : currentCellOrigin.y;
        float nextCrossingX = stepX == 0 ? float.PositiveInfinity : (nextBoundaryX - localPosition.x) / localDirection.x;
        float nextCrossingY = stepY == 0 ? float.PositiveInfinity : (nextBoundaryY - localPosition.y) / localDirection.y;

        if (Mathf.Approximately(nextCrossingX, nextCrossingY))
        {
            Vector3Int horizontalCellPosition = activeCellPosition + new Vector3Int(stepX, 0, 0);
            Vector3Int verticalCellPosition = activeCellPosition + new Vector3Int(0, stepY, 0);
            Vector3Int diagonalCellPosition = activeCellPosition + new Vector3Int(stepX, stepY, 0);
            return !CanEnterCell(combatGrid, horizontalCellPosition, enemy.MovementType) ||
                   !CanEnterCell(combatGrid, verticalCellPosition, enemy.MovementType) ||
                   !CanEnterCell(combatGrid, diagonalCellPosition, enemy.MovementType);
        }

        Vector3Int nextCellPosition = nextCrossingX < nextCrossingY
            ? activeCellPosition + new Vector3Int(stepX, 0, 0)
            : activeCellPosition + new Vector3Int(0, stepY, 0);
        return !CanEnterCell(combatGrid, nextCellPosition, enemy.MovementType);
    }

    private bool CanEnterCell(CombatGrid combatGrid, Vector3Int cellPosition, UnitMovementType movementType)
    {
        return combatGrid.TryGetCell(cellPosition, out CombatGridCell cell) && UnitMovementRules.CanEnterCell(movementType, cell);
    }

    private bool HasCrossedCheckpointLine(Vector3 checkpointWorldPosition, Vector2 segmentDirection,
                                          Vector3 previousPosition, Vector3 currentPosition, Vector3 cellSize)
    {
        if (!hasPreviousEnemyPosition || segmentDirection == Vector2.zero)
        {
            return false;
        }

        Vector2 checkpointCenter = checkpointWorldPosition;
        Vector2 previousOffset = (Vector2)previousPosition - checkpointCenter;
        Vector2 currentOffset = (Vector2)currentPosition - checkpointCenter;
        float previousDepth = Vector2.Dot(previousOffset, segmentDirection);
        float currentDepth = Vector2.Dot(currentOffset, segmentDirection);
        float depthDelta = currentDepth - previousDepth;

        if (previousDepth > 0f || currentDepth < 0f || depthDelta <= Mathf.Epsilon)
        {
            return false;
        }

        float crossingProgress = Mathf.Clamp01(-previousDepth / depthDelta);
        Vector2 crossingPosition = Vector2.Lerp(previousPosition, currentPosition, crossingProgress);
        Vector2 checkpointTangent = new Vector2(-segmentDirection.y, segmentDirection.x);
        float checkpointHalfWidth = GetCheckpointHalfWidth(checkpointTangent, cellSize);
        float lateralDistance = Mathf.Abs(Vector2.Dot(crossingPosition - checkpointCenter, checkpointTangent));
        return lateralDistance <= checkpointHalfWidth;
    }

    private float GetCheckpointHalfWidth(Vector2 checkpointTangent, Vector3 cellSize)
    {
        float width = Mathf.Abs(checkpointTangent.x) * Mathf.Abs(cellSize.x) + Mathf.Abs(checkpointTangent.y) * Mathf.Abs(cellSize.y);
        return width * 0.5f;
    }

    public bool OnCheckpointReached(EnemyRouteCheckpointDefinition checkpoint)
    {
        if (route == null || route.Checkpoints.Count == 0)
        {
            Debug.LogError("[EnemyPathfindingController] Route is not defined or has no checkpoints.");
            return false;
        }

        if (checkpoint == GetTargetCheckpoint())
        {
            useFlowField = false;
            ResetCornerSmoothing();

            if (targetCheckpointIndex < route.Checkpoints.Count - 1)
            {
                currentCheckpointIndex = targetCheckpointIndex;
                targetCheckpointIndex++;
                return false; // Not finished yet, there are more checkpoints to reach
            }
            else
            {
                hasReachedFinalCheckpoint = true;
                Debug.Log($"[EnemyPathfindingController] Enemy has reached the final checkpoint '{checkpoint.CheckpointId}' in route '{route.RouteId}'.");
                currentCheckpointIndex = targetCheckpointIndex;
                OnReachedFinalCheckpoint?.Invoke();
                return true; // Finished, reached the final checkpoint
            }
        }
        return false;
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
        useFlowField = false;
    }
}
