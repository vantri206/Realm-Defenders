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
    public string RouteId => routeId;
    public int GroupCount => groupCount;
    public int EnemiesPerGroup => enemyCount;
    public float GroupInterval => interval;
    public EnemySpawnEventStartCondition StartCondition => startCondition;
    public string RequiredEventId => requiredEventId;
    public float StartDelay => startDelay;
}
