using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CombatGridCell
{
    private Vector3Int cellPosition;
    private CombatGridCellStates cellStates;
    private HeroRuntime anchoredHero;
    private List<UnitRuntime> activeUnits = new List<UnitRuntime>();

    public Vector3Int CellPosition => cellPosition;
    public CombatGridCellStates CellStates => cellStates;
    public HeroRuntime AnchoredHero => anchoredHero;
    public bool HasAnchoredHero => anchoredHero != null;
    public IReadOnlyList<UnitRuntime> Units => activeUnits;
    public bool HasUnits => activeUnits.Count > 0;

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

    public void SetAchoredHero(HeroRuntime hero)
    {
        anchoredHero = hero;
    }

    public void ClearAnchoredHero()
    {
        anchoredHero = null;
    }

    public void AddUnit(UnitRuntime unit)
    {
        if (!activeUnits.Contains(unit))
        {
            activeUnits.Add(unit);
        }
    }

    public void RemoveUnit(UnitRuntime unit)
    {
        if (activeUnits.Contains(unit))
        {
            activeUnits.Remove(unit);
        }
    }

    public void ClearUnits()
    {
        activeUnits.Clear();
    }

    public bool CanDeployHero()
    {
        return IsDeployable && !HasAnchoredHero;
    }

    public bool CanWalk()
    {
        return IsWalkable && !IsBlocked;
    }
}
