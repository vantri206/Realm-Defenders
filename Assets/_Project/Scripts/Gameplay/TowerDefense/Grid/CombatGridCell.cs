using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CombatGridCell
{
    private Vector3Int cellPosition;
    private CombatGridCellStates cellStates;
    private HeroRuntime deployedHero;
    private List<EnemyRuntime> enemies = new List<EnemyRuntime>();

    public Vector3Int CellPosition => cellPosition;
    public CombatGridCellStates CellStates => cellStates;
    public HeroRuntime DeployedHero => deployedHero;
    public bool HasDeployedHero => deployedHero != null;
    public IReadOnlyList<EnemyRuntime> Enemies => enemies;
    public bool HasEnemies => enemies.Count > 0;

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

    public void AddEnemy(EnemyRuntime enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    public void RemoveEnemy(EnemyRuntime enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }

    public void ClearEnemies()
    {
        enemies.Clear();
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
