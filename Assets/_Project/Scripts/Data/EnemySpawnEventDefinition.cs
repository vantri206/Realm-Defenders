using System;
using UnityEngine;

[Serializable]
public class EnemySpawnEventDefinition
{
    [Header("Identity")]
    [SerializeField] private string eventId;

    [Header("Enemy Actor Settings")]
    [SerializeField] private EnemyDefinition enemyDefinition;
    [SerializeField] private string spawnPointId;
    [SerializeField] private string routeId;

    [Header("Spawn Settings")]
    [SerializeField] private int groupCount = 1;
    [SerializeField] private int enemiesPerGroup = 1;
    [SerializeField] private float groupInterval = 0.5f;

    [Header("Start Condition")]
    [SerializeField] private EnemySpawnEventStartCondition startCondition = EnemySpawnEventStartCondition.AfterDelay;
    [SerializeField] private string requiredEventId;
    [SerializeField] private float startDelay;

    public string EventId => eventId;
    public EnemyDefinition EnemyDefinition => enemyDefinition;
    public string SpawnPointId => spawnPointId;
    public string RouteId => routeId;
    public int GroupCount => groupCount;
    public int EnemiesPerGroup => enemiesPerGroup;
    public float GroupInterval => groupInterval;
    public EnemySpawnEventStartCondition StartCondition => startCondition;
    public string RequiredEventId => requiredEventId;
    public float StartDelay => startDelay;

    public EnemySpawnEventDefinition() { }

    public EnemySpawnEventDefinition(string eventId, EnemyDefinition enemyDefinition, string spawnPointId, string routeId,
                                    int groupCount, int enemiesPerGroup, float groupInterval,
                                    EnemySpawnEventStartCondition startCondition, string requiredEventId, float startDelay)
    {
        this.eventId = eventId;
        this.enemyDefinition = enemyDefinition;
        this.spawnPointId = spawnPointId;
        this.routeId = routeId;
        this.groupCount = groupCount;
        this.enemiesPerGroup = enemiesPerGroup;
        this.groupInterval = groupInterval;
        this.startCondition = startCondition;
        this.requiredEventId = requiredEventId;
        this.startDelay = startDelay;
    }
}
