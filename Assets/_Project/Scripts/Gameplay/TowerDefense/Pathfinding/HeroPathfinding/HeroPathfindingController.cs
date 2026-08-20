using System.Collections.Generic;
using UnityEngine;

public class HeroPathfindingController : MonoBehaviour
{
    private int guardRange = 3;

    private readonly HashSet<Vector3Int> targetEnemyCells = new HashSet<Vector3Int>();

    private UnitPathfindingSystem pathfindingSystem;
    private CombatGrid combatGrid;
    private TeamIdentity teamIdentity;
    private bool hasMoveTarget;
    private bool isReturning;
    private Vector3Int moveTargetCellPosition;
    private float cellTargetThreshold = GameplayConstants.CELL_TARGET_THRESHOLD;

    public bool HasGuardTarget => targetEnemyCells.Count > 0;

    public bool Initialize(CombatGrid combatGrid, UnitPathfindingSystem pathfindingSystem, TeamIdentity teamIdentity)
    {
        if (combatGrid == null || pathfindingSystem == null || teamIdentity == null)
        {
            Debug.LogError("[HeroPathfindingController] CombatGrid, PathfindingSystem, and TeamIdentity are required to initialize HeroPathfindingController.", this);
            return false;
        }

        this.combatGrid = combatGrid;
        this.pathfindingSystem = pathfindingSystem;
        this.teamIdentity = teamIdentity;
        return true;
    }

    public Vector2 GetCurrentMoveDirection(HeroRuntime hero, CombatGridCell activeCell, CombatGridCell anchorCell, Vector3 heroCenterPosition)
    {
        if (hero == null || activeCell == null || anchorCell == null)
        {
            ResetMoveTarget();
            return Vector2.zero;
        }

        bool shouldMoveReturn = !TryFindEnemyInGuard(hero, anchorCell.CellPosition);
        if (hasMoveTarget && shouldMoveReturn != isReturning)
        {
            ResetMoveTarget();
        }

        if (hasMoveTarget)
        {
            Vector2 directionToTarget = GetDirectionToMoveTarget(heroCenterPosition);
            if (directionToTarget != Vector2.zero)
            {
                return directionToTarget;
            }

            hasMoveTarget = false;
        }

        Vector2Int primaryDirection = GetPrimaryDirection(activeCell, anchorCell, shouldMoveReturn);
        if (primaryDirection == Vector2Int.zero)
        {
            return Vector2.zero;
        }

        moveTargetCellPosition = activeCell.CellPosition + new Vector3Int(primaryDirection.x, primaryDirection.y, 0);
        isReturning = shouldMoveReturn;
        hasMoveTarget = true;
        return GetDirectionToMoveTarget(heroCenterPosition);
    }

    public void ResetMoveTarget()
    {
        hasMoveTarget = false;
        isReturning = false;
        moveTargetCellPosition = Vector3Int.zero;
    }

    private Vector2Int GetPrimaryDirection(CombatGridCell activeCell, CombatGridCell anchorCell, bool shouldMoveReturn)
    {
        if (!shouldMoveReturn)
        {
            if (pathfindingSystem.TryGetLocalBFSDirection(activeCell.CellPosition, targetEnemyCells, guardRange, out Vector2Int chaseDirection))
            {
                return chaseDirection;
            }

            return Vector2Int.zero;
        }

        if (activeCell == anchorCell)
        {
            return Vector2Int.zero;
        }

        if (pathfindingSystem.TryGetLocalBFSDirection(activeCell.CellPosition, anchorCell.CellPosition, guardRange, out Vector2Int returnDirection))
        {
            return returnDirection;
        }

        return Vector2Int.zero;
    }

    private bool TryFindEnemyInGuard(HeroRuntime hero, Vector3Int anchorCellPosition)
    {
        targetEnemyCells.Clear();

        int resolvedGuardRange = Mathf.Max(0, guardRange);
        for (int x = -resolvedGuardRange; x <= resolvedGuardRange; x++)
        {
            for (int y = -resolvedGuardRange; y <= resolvedGuardRange; y++)
            {
                Vector3Int cellPosition = anchorCellPosition + new Vector3Int(x, y, 0);
                if (!combatGrid.TryGetCell(cellPosition, out CombatGridCell cell) || !cell.HasUnits)
                {
                    continue;
                }

                IReadOnlyList<UnitRuntime> units = cell.Units;
                for (int i = 0; i < units.Count; i++)
                {
                    UnitRuntime unit = units[i];
                    if (unit == null || !unit.IsInitialized || unit.IsDead || unit.TeamIdentity == null)
                    {
                        continue;
                    }

                    if (teamIdentity.IsEnemy(unit.TeamIdentity) && AttackTargetRulling.CanTarget(hero, unit))
                    {
                        targetEnemyCells.Add(cellPosition);
                        break;
                    }
                }
            }
        }

        return targetEnemyCells.Count > 0;
    }

    private Vector2 GetDirectionToMoveTarget(Vector3 heroCenterPosition)
    {
        if (!combatGrid.TryCellToWorldCenter(moveTargetCellPosition, out Vector3 targetWorldCenter))
        {
            return Vector2.zero;
        }

        Vector2 toTarget = targetWorldCenter - heroCenterPosition;
        if (toTarget.sqrMagnitude <= cellTargetThreshold * cellTargetThreshold)
        {
            return Vector2.zero;
        }

        return toTarget.normalized;
    }
}
