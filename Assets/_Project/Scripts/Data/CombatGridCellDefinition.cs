using System;
using UnityEngine;

[Serializable]
public class CombatGridCellDefinition
{
    [SerializeField] private Vector3Int cellPosition;
    [SerializeField] private CombatGridCellStates cellStates;

    public Vector3Int CellPosition => cellPosition;
    public CombatGridCellStates CellStates => cellStates;

    public CombatGridCellDefinition() { }

    public CombatGridCellDefinition(Vector3Int cellPosition, CombatGridCellStates cellStates)
    {
        this.cellPosition = cellPosition;
        this.cellStates = cellStates;
    }
}
