using System;
using UnityEngine;

[Serializable]
public class CombatGridCell
{
    private Vector3Int cellPosition;
    private CombatGridCellStates cellStates;
    private HeroRuntime deployedHero;

    public Vector3Int CellPosition => cellPosition;
    public CombatGridCellStates CellStates => cellStates;
    public HeroRuntime DeployedHero => deployedHero;
    public bool HasDeployedHero => deployedHero != null;
    public bool IsWalkable => HasState(CombatGridCellStates.Walkable);
    public bool IsDeployable => HasState(CombatGridCellStates.Deployable);
    public bool IsBlocked => HasState(CombatGridCellStates.Blocked);

    public CombatGridCell(Vector3Int cellPosition)
    {
        this.cellPosition = cellPosition;
        cellStates = CombatGridCellStates.None;
    }

    public bool HasState(CombatGridCellStates state)
    {
        return (cellStates & state) == state;
    }

    public void AddState(CombatGridCellStates state)
    {
        cellStates |= state;
    }   

    public void RemoveState(CombatGridCellStates state)
    {
        cellStates &= ~state;
    }

    public void SetDeployedHero(HeroRuntime hero)
    {
        deployedHero = hero;
    }

    public void ClearDeployedHero()
    {
        deployedHero = null;
    }

    public bool CanDeployHero()
    {
        return IsDeployable && !HasDeployedHero;
    }

    public bool CanWalk()
    {
        return IsWalkable && !IsBlocked;
    }
}
