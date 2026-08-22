using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMovement : MonoBehaviour
{
    private const float minBlockedCellDistance = 0.05f;
    private const float impulseStopSpeed = 0.01f;
    private const float overrideAlignSpeedInCells = 2f;
    private const float overrideArrivalDistanceInCells = 0.01f;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private UnitRuntime unitRuntime;
    
    private UnitStats stats;
    private UnitMovementType movementType = UnitMovementType.Ground;
    private Vector2 currentMoveDirection;
    private Vector2 externalVelocity;
    private float deceleration;
    private Vector2 movementOverridePosition;
    private bool hasMovementOverride;
    private bool isInitialized;

    private Vector2 rbCenterPosition => rb.position + unitRuntime.CenterOffset;

    public Vector2 CurrentMoveDirection => currentMoveDirection;
    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        CacheReferences();
    }

    public bool Initialize(UnitStats stats, UnitMovementType movementType)
    {
        CacheReferences();

        if (stats == null || rb == null || unitRuntime == null || unitRuntime.CombatGrid == null || unitRuntime.CombatGrid.Grid == null)
        {
            Debug.LogError("[UnitMovement] Missing required references.", this);
            isInitialized = false;
            return false;
        }

        this.stats = stats;
        this.movementType = movementType;
        ResetTransientMovement();
        isInitialized = true;
        return true;
    }

    private void OnDisable()
    {
        ResetTransientMovement();
    }

    private void CacheReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (unitRuntime == null)
        {
            unitRuntime = GetComponent<UnitRuntime>();
        }
    }

    public void FixedTick(float fixedDeltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        Vector2 movement = Vector2.zero;
        if (hasMovementOverride)
        {
            movement = GetOverrideMovement(fixedDeltaTime);
        }
        else if (currentMoveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            movement = currentMoveDirection.normalized * stats.MoveSpeed * fixedDeltaTime;
        }
        movement += GetImpulseMovement(fixedDeltaTime);
        Move(movement);
    }

    private Vector2 GetOverrideMovement(float fixedDeltaTime)
    {
        Vector2 toTarget = movementOverridePosition - rb.position;
        float cellScale = GetCellScale();
        float arrivalDistance = cellScale * overrideArrivalDistanceInCells;
        if (toTarget.sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            return Vector2.zero;
        }

        float maxMovement = cellScale * overrideAlignSpeedInCells * fixedDeltaTime;
        Vector2 requestedMovement = Vector2.ClampMagnitude(toTarget, maxMovement);
        Vector2 resolvedMovement = ResolveGridMovement(rbCenterPosition, requestedMovement);
        if (resolvedMovement.sqrMagnitude > Mathf.Epsilon)
        {
            return resolvedMovement;
        }

        ClearMovementOverride();
        return Vector2.zero;
    }

    private float GetCellScale()
    {
        Vector3 cellSize = unitRuntime.CombatGrid.CellSize;
        float cellScale = Mathf.Min(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y));
        return cellScale > Mathf.Epsilon ? cellScale : 1f;
    }

    private Vector2 GetImpulseMovement(float fixedDeltaTime)
    {
        if (externalVelocity.sqrMagnitude <= impulseStopSpeed * impulseStopSpeed)
        {
            externalVelocity = Vector2.zero;
            deceleration = 0f;
            return Vector2.zero;
        }

        Vector2 previousVelocity = externalVelocity;
        externalVelocity = Vector2.MoveTowards(externalVelocity, Vector2.zero, deceleration * fixedDeltaTime);

        Vector2 averageVelocity = (previousVelocity + externalVelocity) * 0.5f;
        return averageVelocity * fixedDeltaTime;
    }

    private void Move(Vector2 movement)
    {
        Vector3 startCenterPosition = rbCenterPosition;
        Vector2 resolvedMovement = ResolveGridMovement(startCenterPosition, movement);
        Vector3 targetCenterPosition = startCenterPosition + (Vector3)resolvedMovement;
        if (resolvedMovement.sqrMagnitude > Mathf.Epsilon && !CanEnterWorldPosition(unitRuntime.CombatGrid, targetCenterPosition))
        {
            resolvedMovement = Vector2.zero;
        }

        rb.MovePosition(rb.position + resolvedMovement);
    }

    public bool ApplyForce(Vector2 direction, float targetDistance, float duration)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon || targetDistance <= Mathf.Epsilon || duration <= Mathf.Epsilon)
        {
            return false;
        }

        float initialSpeed = 2f * targetDistance / duration;
        externalVelocity = direction.normalized * initialSpeed;
        deceleration = initialSpeed / duration;
        return true;
    }

    private Vector2 ResolveGridMovement(Vector3 centerPosition, Vector2 movement)
    {
        if (movement.sqrMagnitude <= Mathf.Epsilon)
        {
            return movement;
        }

        CombatGrid combatGrid = unitRuntime.CombatGrid;
        if (CanMoveWithBlockedDistance(combatGrid, centerPosition, movement))
        {
            return movement;
        }

        Vector2 horizontalMovement = new Vector2(movement.x, 0f);
        Vector2 verticalMovement = new Vector2(0f, movement.y);
        bool canMoveHorizontal = Mathf.Abs(horizontalMovement.x) > Mathf.Epsilon && CanMoveWithBlockedDistance(combatGrid, centerPosition, horizontalMovement);
        bool canMoveVertical = Mathf.Abs(verticalMovement.y) > Mathf.Epsilon && CanMoveWithBlockedDistance(combatGrid, centerPosition, verticalMovement);

        if (canMoveHorizontal && canMoveVertical)
        {
            return horizontalMovement.sqrMagnitude > verticalMovement.sqrMagnitude ? horizontalMovement : verticalMovement;
        }

        if (canMoveHorizontal)
        {
            return horizontalMovement;
        }

        if (canMoveVertical)
        {
            return verticalMovement;
        }

        return Vector2.zero;
    }

    private bool CanMoveWithBlockedDistance(CombatGrid combatGrid, Vector3 startPosition, Vector2 movement)
    {
        Vector2 checkedMovement = movement + movement.normalized * minBlockedCellDistance;
        return CanMoveThroughGridCells(combatGrid, startPosition, checkedMovement);
    }

    private bool CanMoveThroughGridCells(CombatGrid combatGrid, Vector3 startPosition, Vector2 movement)
    {
        float moveDistance = movement.magnitude;
        if (moveDistance <= Mathf.Epsilon)
        {
            return true;
        }

        Grid grid = combatGrid.Grid;
        Vector3 endPosition = startPosition + (Vector3)movement;
        if (!combatGrid.TryWorldToCell(startPosition, out CombatGridCell startCell) ||
            !UnitMovementRules.CanEnterCell(movementType, startCell) ||
            !combatGrid.TryWorldToCell(endPosition, out CombatGridCell endCell))
        {
            return false;
        }

        Vector3Int currentCellPosition = startCell.CellPosition;
        Vector3Int endCellPosition = endCell.CellPosition;
        if (currentCellPosition == endCellPosition)
        {
            return UnitMovementRules.CanEnterCell(movementType, endCell);
        }

        Vector3 localStartPosition = grid.transform.InverseTransformPoint(startPosition);
        Vector3 localMovement = grid.transform.InverseTransformVector(movement);
        Vector3 cellStride = grid.cellSize + grid.cellGap;
        if (Mathf.Abs(cellStride.x) <= Mathf.Epsilon || Mathf.Abs(cellStride.y) <= Mathf.Epsilon)
        {
            return false;
        }

        int stepX = localMovement.x > 0f ? 1 : localMovement.x < 0f ? -1 : 0;
        int stepY = localMovement.y > 0f ? 1 : localMovement.y < 0f ? -1 : 0;
        Vector3 currentCellOrigin = grid.CellToLocal(currentCellPosition);

        float nextBoundaryX = stepX > 0 ? currentCellOrigin.x + cellStride.x : currentCellOrigin.x;
        float nextBoundaryY = stepY > 0 ? currentCellOrigin.y + cellStride.y : currentCellOrigin.y;
        float nextCrossingX = stepX == 0 ? float.PositiveInfinity : (nextBoundaryX - localStartPosition.x) / localMovement.x;
        float nextCrossingY = stepY == 0 ? float.PositiveInfinity : (nextBoundaryY - localStartPosition.y) / localMovement.y;
        float crossingIntervalX = stepX == 0 ? float.PositiveInfinity : Mathf.Abs(cellStride.x / localMovement.x);
        float crossingIntervalY = stepY == 0 ? float.PositiveInfinity : Mathf.Abs(cellStride.y / localMovement.y);

        int maxCellCrossings = Mathf.Abs(endCellPosition.x - currentCellPosition.x) + Mathf.Abs(endCellPosition.y - currentCellPosition.y) + 1;
        int cellCrossingCount = 0;
        while (currentCellPosition != endCellPosition)
        {
            cellCrossingCount++;
            if (cellCrossingCount > maxCellCrossings)
            {
                return false;
            }

            if (Mathf.Approximately(nextCrossingX, nextCrossingY))
            {
                Vector3Int horizontalCellPosition = currentCellPosition + new Vector3Int(stepX, 0, 0);
                Vector3Int verticalCellPosition = currentCellPosition + new Vector3Int(0, stepY, 0);
                Vector3Int diagonalCellPosition = currentCellPosition + new Vector3Int(stepX, stepY, 0);
                if (!CanEnterCell(combatGrid, horizontalCellPosition) ||
                    !CanEnterCell(combatGrid, verticalCellPosition) ||
                    !CanEnterCell(combatGrid, diagonalCellPosition))
                {
                    return false;
                }

                currentCellPosition = diagonalCellPosition;
                nextCrossingX += crossingIntervalX;
                nextCrossingY += crossingIntervalY;
                continue;
            }

            if (nextCrossingX < nextCrossingY)
            {
                currentCellPosition += new Vector3Int(stepX, 0, 0);
                nextCrossingX += crossingIntervalX;
            }
            else
            {
                currentCellPosition += new Vector3Int(0, stepY, 0);
                nextCrossingY += crossingIntervalY;
            }

            if (!CanEnterCell(combatGrid, currentCellPosition))
            {
                return false;
            }
        }

        return true;
    }

    private bool CanEnterCell(CombatGrid combatGrid, Vector3Int cellPosition)
    {
        return combatGrid.TryGetCell(cellPosition, out CombatGridCell cell) && UnitMovementRules.CanEnterCell(movementType, cell);
    }

    private bool CanEnterWorldPosition(CombatGrid combatGrid, Vector3 worldPosition)
    {
        return combatGrid.TryWorldToCell(worldPosition, out CombatGridCell cell) && UnitMovementRules.CanEnterCell(movementType, cell);
    }

    public void SetMoveDirection(Vector2 direction)
    {
        currentMoveDirection = direction;
    }

    public void SetMovementOverride(Vector2 worldPosition)
    {
        movementOverridePosition = worldPosition;
        hasMovementOverride = true;
    }

    public void ClearMovementOverride()
    {
        movementOverridePosition = Vector2.zero;
        hasMovementOverride = false;
    }

    private void ResetTransientMovement()
    {
        currentMoveDirection = Vector2.zero;
        externalVelocity = Vector2.zero;
        deceleration = 0f;
        ClearMovementOverride();
    }
}
