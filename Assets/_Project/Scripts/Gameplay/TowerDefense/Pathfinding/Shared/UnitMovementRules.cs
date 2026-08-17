public static class UnitMovementRules
{
    private const byte baseMovableCost = 1;

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
        return CanEnterCell(movementType, cell) ? baseMovableCost : GameplayConstants.BLOCKED_COST;
    }
}
