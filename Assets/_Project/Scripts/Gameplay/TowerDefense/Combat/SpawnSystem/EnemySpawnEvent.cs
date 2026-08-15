using System;
using UnityEngine;

[Serializable]
public class EnemySpawnEvent
{
    [SerializeField] private string eventId;
    [SerializeField] private EnemyDefinition enemyDefinition;
    [SerializeField] private EnemySpawnPoint spawnPoint;
    [SerializeField] private string routeId;
    [SerializeField] private int spawnCount = 1;
    [SerializeField] private int enemyCount =  1;
    [SerializeField] private float interval = 0.5f;
    [SerializeField] private EnemySpawnEventStartCondition startCondition = EnemySpawnEventStartCondition.AfterDelay;
    [SerializeField] private string requiredEventId;
    [SerializeField] private float startDelay = 0f;

    private EnemySpawnEventState state = EnemySpawnEventState.Waiting;

    public string EventId => eventId;
    public EnemyDefinition EnemyDefinition => enemyDefinition;
    public EnemySpawnPoint SpawnPoint => spawnPoint;
    public string RouteId => routeId;
    public int SpawnCount => spawnCount;
    public int EnemyCount => enemyCount;
    public float Interval => interval;
    public EnemySpawnEventStartCondition StartCondition => startCondition;
    public string RequiredEventId => requiredEventId;
    public float StartDelay => startDelay;
    public EnemySpawnEventState State => state;
    public bool IsWaiting => state == EnemySpawnEventState.Waiting;
    public bool IsSpawning => state == EnemySpawnEventState.Spawning;
    public bool IsFinished => state == EnemySpawnEventState.Finished;
    public bool IsResolved => state == EnemySpawnEventState.Resolved;

    public void ResetState()
    {
        state = EnemySpawnEventState.Waiting;
    }

    public void MarkSpawning()
    {
        state = EnemySpawnEventState.Spawning;
    }

    public void MarkFinished()
    {
        state = EnemySpawnEventState.Finished;
    }

    public void MarkResolved()
    {
        state = EnemySpawnEventState.Resolved;
    }
}
