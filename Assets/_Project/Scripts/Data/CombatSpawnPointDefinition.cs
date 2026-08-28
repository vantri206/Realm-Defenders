using System;
using UnityEngine;

[Serializable]
public class CombatSpawnPointDefinition
{
    [SerializeField] private string spawnPointId;
    [SerializeField] private Vector3Int cellPosition;

    public string SpawnPointId => spawnPointId;
    public Vector3Int CellPosition => cellPosition;

    public CombatSpawnPointDefinition() { }

    public CombatSpawnPointDefinition(string spawnPointId, Vector3Int cellPosition)
    {
        this.spawnPointId = spawnPointId;
        this.cellPosition = cellPosition;
    }
}
