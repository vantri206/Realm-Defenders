using System;
using UnityEngine;

[Serializable]
public class EnemySpawnEvent
{
    [Header("Identity")]
    [SerializeField] private string eventId;

    [Header("Enemy Actor Settings")]
    [SerializeField] private EnemyDefinition enemyDefinition;
    [SerializeField] private EnemySpawnPoint spawnPoint;
    [SerializeField] private string spawnPointId;
    [SerializeField] private string routeId;

    [Header("Spawn Settings")]
    [SerializeField] private int groupCount = 1;
    [SerializeField] private int enemyCount =  1;
    [SerializeField] private float interval = 0.5f;

    [Header("Start Condition Settings")]
    [SerializeField] private EnemySpawnEventStartCondition startCondition = EnemySpawnEventStartCondition.AfterDelay;
    [SerializeField] private string requiredEventId;
    [SerializeField] private float startDelay = 0f;

    public string EventId => eventId;
    public EnemyDefinition EnemyDefinition => enemyDefinition;
    public EnemySpawnPoint SpawnPoint => spawnPoint;
    public string SpawnPointId
    {
        get
        {
            if (spawnPoint != null && !string.IsNullOrWhiteSpace(spawnPoint.SpawnPointId))
            {
                return spawnPoint.SpawnPointId;
            }

            return spawnPointId;
        }
    }

    public string RouteId => routeId;
    public int GroupCount => groupCount;
    public int EnemiesPerGroup => enemyCount;
    public float GroupInterval => interval;
    public EnemySpawnEventStartCondition StartCondition => startCondition;
    public string RequiredEventId => requiredEventId;
    public float StartDelay => startDelay;

    public EnemySpawnEvent() { }

    public EnemySpawnEvent(string eventId, EnemyDefinition enemyDefinition, EnemySpawnPoint spawnPoint, string routeId, int groupCount, int enemyCount, float interval,
                           EnemySpawnEventStartCondition startCondition, string requiredEventId, float startDelay)
        : this(eventId, enemyDefinition, spawnPoint, GetSpawnPointId(spawnPoint), routeId, groupCount, enemyCount, interval, startCondition, requiredEventId, startDelay)
    {
    }

    public EnemySpawnEvent(string eventId, EnemyDefinition enemyDefinition, EnemySpawnPoint spawnPoint, string spawnPointId, string routeId, int groupCount, int enemyCount, float interval,
                           EnemySpawnEventStartCondition startCondition, string requiredEventId, float startDelay)
    {
        this.eventId = eventId;
        this.enemyDefinition = enemyDefinition;
        this.spawnPoint = spawnPoint;
        this.spawnPointId = spawnPointId;
        this.routeId = routeId;
        this.groupCount = groupCount;
        this.enemyCount = enemyCount;
        this.interval = interval;
        this.startCondition = startCondition;
        this.requiredEventId = requiredEventId;
        this.startDelay = startDelay;
    }

    private static string GetSpawnPointId(EnemySpawnPoint spawnPoint)
    {
        if (spawnPoint != null && !string.IsNullOrWhiteSpace(spawnPoint.SpawnPointId))
        {
            return spawnPoint.SpawnPointId;
        }

        return string.Empty;
    }
}
