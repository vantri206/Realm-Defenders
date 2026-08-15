using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMovement : MonoBehaviour
{
    private const float minBlockedCellDistance = 0.05f;
    private const float impulseStopSpeed = 0.01f;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private UnitRuntime unitRuntime;
    
    private UnitSpeed enemySpeed;
    private Vector2 currentMoveDirection;
    private Vector2 impulseVelocity;
    private float impulseDeceleration;

    private Vector2 rigidbodyCenterPosition => rb.position + (unitRuntime != null ? unitRuntime.CenterOffset : Vector2.zero);

    public UnitSpeed Speed => enemySpeed;
    public Vector2 CurrentMoveDirection => currentMoveDirection;

    private void Awake()
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
    
    public void Initialize(UnitSpeed enemySpeed)
    {
        if (enemySpeed == null)
        {
            Debug.LogError("[UnitMovement] UnitSpeed is required for movement initialization.", this);
            return;
        }

        this.enemySpeed = enemySpeed;
    }

    public void FixedTick(float fixedDeltaTime)
    {
        if (rb == null)
        {
            Debug.LogError("[UnitMovement] Rigidbody2D is required to process movement.", this);
            return;
        }

        if (enemySpeed == null)
        {
            Debug.LogError("[UnitMovement] UnitSpeed is required to process movement. Call Initialize before FixedTick.", this);
            return;
        }

        Vector2 normalVelocity = currentMoveDirection.normalized * enemySpeed.MoveSpeed;
        Vector2 movement = normalVelocity * fixedDeltaTime + GetImpulseMovement(fixedDeltaTime);
        Move(movement);
    }

    private Vector2 GetImpulseMovement(float fixedDeltaTime)
    {
        if (impulseVelocity.sqrMagnitude <= impulseStopSpeed * impulseStopSpeed)
        {
            impulseVelocity = Vector2.zero;
            impulseDeceleration = 0f;
            return Vector2.zero;
        }

        Vector2 previousVelocity = impulseVelocity;
        impulseVelocity = Vector2.MoveTowards(impulseVelocity, Vector2.zero, impulseDeceleration * fixedDeltaTime);

        Vector2 averageVelocity = (previousVelocity + impulseVelocity) * 0.5f;
        return averageVelocity * fixedDeltaTime;
    }

    private void Move(Vector2 movement)
    {
        Vector3 startCenterPosition = rigidbodyCenterPosition;
        Vector2 resolvedMovement = ResolveGridMovement(startCenterPosition, movement);
        Vector3 targetCenterPosition = startCenterPosition + (Vector3)resolvedMovement;
        if (resolvedMovement.sqrMagnitude > Mathf.Epsilon &&
            unitRuntime != null &&
            unitRuntime.CombatGrid != null &&
            !IsWorldPositionWalkable(unitRuntime.CombatGrid, targetCenterPosition))
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
        impulseVelocity = direction.normalized * initialSpeed;
        impulseDeceleration = initialSpeed / duration;
        return true;
    }

    private Vector2 ResolveGridMovement(Vector3 centerPosition, Vector2 movement)
    {
        if (movement.sqrMagnitude <= Mathf.Epsilon || unitRuntime == null || unitRuntime.CombatGrid == null)
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
        return CanMoveThroughWalkableCells(combatGrid, startPosition, checkedMovement);
    }

    private bool CanMoveThroughWalkableCells(CombatGrid combatGrid, Vector3 startPosition, Vector2 movement)
    {
        float moveDistance = movement.magnitude;
        if (moveDistance <= Mathf.Epsilon)
        {
            return true;
        }

        Grid grid = combatGrid.Grid;
        if (grid == null || grid.cellLayout != GridLayout.CellLayout.Rectangle)
        {
            return false;
        }

        Vector3 endPosition = startPosition + (Vector3)movement;
        if (!combatGrid.TryWorldToCell(startPosition, out CombatGridCell startCell) ||  startCell == null || !startCell.CanWalk() ||
            !combatGrid.TryWorldToCell(endPosition, out CombatGridCell endCell) || endCell == null)
        {
            return false;
        }

        Vector3Int currentCellPosition = startCell.CellPosition;
        Vector3Int endCellPosition = endCell.CellPosition;
        if (currentCellPosition == endCellPosition)
        {
            return endCell.CanWalk();
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
                if (!IsCellWalkable(combatGrid, horizontalCellPosition) ||
                    !IsCellWalkable(combatGrid, verticalCellPosition) ||
                    !IsCellWalkable(combatGrid, diagonalCellPosition))
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

            if (!IsCellWalkable(combatGrid, currentCellPosition))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsCellWalkable(CombatGrid combatGrid, Vector3Int cellPosition)
    {
        return combatGrid.TryGetCell(cellPosition, out CombatGridCell cell) && cell != null && cell.CanWalk();
    }

    private bool IsWorldPositionWalkable(CombatGrid combatGrid, Vector3 worldPosition)
    {
        return combatGrid.TryWorldToCell(worldPosition, out CombatGridCell cell) && cell != null && cell.CanWalk();
    }
    
    public void SetMoveSpeed(float newMoveSpeed)
    {
        if (enemySpeed == null)
        {
            Debug.LogError("[UnitMovement] Cannot set move speed before Initialize.", this);
            return;
        }
        
        enemySpeed.SetMoveSpeed(newMoveSpeed);
    }

    public void SetMoveDirection(Vector2 direction)
    {
        currentMoveDirection = direction;
    }
}
