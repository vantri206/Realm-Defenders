public static class UnitMovementRules
{
    public static bool CanEnterCell(UnitMovementType movementType, CombatGridCell cell)
    {
        if (cell == null)
        {
            return false;
        }

        return movementType == UnitMovementType.Flying || cell.CanWalk();
    }

    public static byte GetPathfindingCost(UnitMovementType movementType, CombatGridCell cell)
    {
        return CanEnterCell(movementType, cell) ? GameplayConstants.NORMAL_COST : GameplayConstants.BLOCKED_COST;
    }
}
