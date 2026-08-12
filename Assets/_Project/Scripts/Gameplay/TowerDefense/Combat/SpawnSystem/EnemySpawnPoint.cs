using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointId;

    public string SpawnPointId => spawnPointId;
    public Vector3 WorldPosition => transform.position;

    public bool TryGetSpawnCell(CombatGrid combatGrid, out CombatGridCell cell)
    {
        if (combatGrid == null)
        {
            Debug.LogError($"[EnemySpawnPoint] CombatGrid is required to resolve spawn point '{spawnPointId}'.", this);
            cell = null;
            return false;
        }

        if (!combatGrid.TryWorldToCell(WorldPosition, out cell))
        {
            Debug.LogError($"[EnemySpawnPoint] Spawn point '{spawnPointId}' does not resolve to a built combat grid cell.", this);
            return false;
        }

        return true;
    }
}
