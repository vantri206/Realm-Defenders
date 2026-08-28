using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointId;

    public string SpawnPointId => spawnPointId;
    public Vector3 WorldPosition => transform.position;
}
