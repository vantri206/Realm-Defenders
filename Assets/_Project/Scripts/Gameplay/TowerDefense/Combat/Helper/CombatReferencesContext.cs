public class UnitCombatContext
{
    public CombatGrid CombatGrid { get; }
    public UnitPathfindingSystem UnitPathfindingSystem { get; }
    public CombatTimeController CombatTime { get; }

    public bool IsValid => CombatGrid != null && UnitPathfindingSystem != null && CombatTime != null;

    public UnitCombatContext(CombatGrid combatGrid, UnitPathfindingSystem unitPathfindingSystem, CombatTimeController combatTime)
    {
        CombatGrid = combatGrid;
        UnitPathfindingSystem = unitPathfindingSystem;
        CombatTime = combatTime;
    }
}