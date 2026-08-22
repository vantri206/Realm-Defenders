using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveDirector
{
    private class SpawnEventRuntime
    {
        public int SpawnedCount;
        public float SpawnIntervalTimer;
        public float SpawnDelayTimer;
    }

    private readonly Dictionary<EnemySpawnEvent, SpawnEventRuntime> spawnEventRuntimes = new Dictionary<EnemySpawnEvent, SpawnEventRuntime>();

    private EnemySpawner enemySpawner;
    private IReadOnlyList<EnemySpawnEvent> spawnEvents;

    private bool isRunning;

    public void Initialize(EnemySpawner enemySpawner, IReadOnlyList<EnemySpawnEvent> spawnEvents)
    {
        StopDirector();

        this.enemySpawner = enemySpawner;
        this.spawnEvents = spawnEvents;

        ResetSpawnEvents();
    }

    public void StartDirector()
    {
        if (enemySpawner == null || !enemySpawner.IsInitialized)
        {
            Debug.LogError("[EnemyWaveDirector] EnemySpawner must be initialized before starting director.");
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("[EnemyWaveDirector] Wave director is already running.");
            return;
        }

        ResetSpawnEvents();
        CheckSpawnEventRequirements();

        for (int i = 0; i < spawnEvents.Count; i++)
        {
            var spawnEvent = spawnEvents[i];
            if (spawnEvent == null)
            {
                Debug.LogWarning($"[EnemyWaveDirector] Spawn event at index {i} is null.");
                continue;
            }

            if (!spawnEventRuntimes.TryGetValue(spawnEvent, out SpawnEventRuntime spawnEventRuntime))
            {
                spawnEventRuntime = new SpawnEventRuntime();
                spawnEventRuntime.SpawnDelayTimer = 0f;
                spawnEventRuntimes.Add(spawnEvent, spawnEventRuntime);
            }

        }

        isRunning = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isRunning)
        {
            return;
        }

        if (spawnEvents != null)
        {
            foreach (var spawnEvent in spawnEvents)
            {
                if (spawnEvent == null)
                {
                    continue;
                }

                spawnEventRuntimes.TryGetValue(spawnEvent, out SpawnEventRuntime spawnEventRuntime);
                if (spawnEventRuntime == null)
                {
                    Debug.LogWarning($"[EnemyWaveDirector] Spawn event runtime for {spawnEvent.EventId} is null.");
                    continue;
                }

                if (CanStartSpawnEvent(spawnEvent, deltaTime))
                {
                    StartSpawnEvent(spawnEvent);
                }

                if (spawnEvent != null && spawnEvent.IsSpawning)
                {
                    TickSpawnEvent(spawnEvent, deltaTime);
                }
            }
        }

        if (CheckAllSpawnFinished())
        {
            isRunning = false;
        }
    }

    public void StopDirector()
    {
        isRunning = false;
    }

    public void ClearDirector()
    {
        StopDirector();
        ResetSpawnEvents();

        enemySpawner = null;
        spawnEvents = null;
    }

    public bool CheckAllSpawnFinished()
    {
        if (spawnEvents == null || spawnEvents.Count == 0)
        {
            return true;
        }

        foreach (var spawnEvent in spawnEvents)
        {
            if (spawnEvent == null)
            {
                continue;
            }

            if (!spawnEvent.IsFinished && !spawnEvent.IsResolved)
            {
                return false;
            }
        }

        return true;
    }

    public bool TryGetSpawnEventById(string eventId, out EnemySpawnEvent spawnEvent)
    {
        spawnEvent = null;

        if (string.IsNullOrEmpty(eventId) || spawnEvents == null)
        {
            return false;
        }

        foreach (var currentSpawnEvent in spawnEvents)
        {
            if (currentSpawnEvent == null)
            {
                continue;
            }

            if (currentSpawnEvent.EventId == eventId)
            {
                spawnEvent = currentSpawnEvent;
                return true;
            }
        }

        return false;
    }

    private void ResetSpawnEvents()
    {
        spawnEventRuntimes.Clear();

        if (spawnEvents == null)
        {
            return;
        }

        foreach (var spawnEvent in spawnEvents)
        if (spawnEvent != null)
        {
            spawnEvent.ResetState();
        }
    }

    private void CheckSpawnEventRequirements()
    {
        if (spawnEvents == null)
        {
            return;
        }

        foreach (var spawnEvent in spawnEvents)
        {
            if (spawnEvent == null || spawnEvent.StartCondition != EnemySpawnEventStartCondition.AfterSpawnEventFinished)
            {
                continue;
            }

            if (string.IsNullOrEmpty(spawnEvent.RequiredEventId))
            {
                Debug.LogWarning($"[EnemyWaveDirector] Spawn event '{spawnEvent.EventId}' requires another event but has no required event id.");
                continue;
            }

            if (!TryGetSpawnEventById(spawnEvent.RequiredEventId, out EnemySpawnEvent requiredEvent) || requiredEvent == null)
            {
                Debug.LogWarning($"[EnemyWaveDirector] Spawn event '{spawnEvent.EventId}' requires missing event '{spawnEvent.RequiredEventId}'.");
            }
        }
    }

    private void StartSpawnEvent(EnemySpawnEvent spawnEvent)
    {
        if (spawnEvent == null)
        {
            return;
        }

        spawnEvent.MarkSpawning();
        spawnEventRuntimes[spawnEvent] = new SpawnEventRuntime();
    }

    private void TickSpawnEvent(EnemySpawnEvent spawnEvent, float deltaTime)
    {
        spawnEventRuntimes.TryGetValue(spawnEvent, out SpawnEventRuntime spawnEventRuntime);
        if (spawnEventRuntime == null)
        {
            return;
        }

        int spawnCount = Mathf.Max(0, Mathf.CeilToInt(spawnEvent.SpawnCount));
        if (spawnEventRuntime.SpawnedCount >= spawnCount)
        {
            spawnEvent.MarkFinished();
            return;
        }

        if (spawnEventRuntime.SpawnIntervalTimer > 0f)
        {
            spawnEventRuntime.SpawnIntervalTimer -= deltaTime;
            if (spawnEventRuntime.SpawnIntervalTimer > 0f)
            {
                return;
            }
        }

        float spawnInterval = Mathf.Max(0f, spawnEvent.Interval);

        while (spawnEventRuntime.SpawnedCount < spawnCount)
        {
            SpawnEnemyGroup(spawnEvent);
            spawnEventRuntime.SpawnedCount++;

            if (spawnEventRuntime.SpawnedCount >= spawnCount)
            {
                spawnEvent.MarkFinished();
                break;
            }

            if (spawnInterval > 0f)
            {
                spawnEventRuntime.SpawnIntervalTimer = spawnInterval;
                break;
            }
        }
    }

    private void SpawnEnemyGroup(EnemySpawnEvent spawnEvent)
    {
        for (int i = 0; i < spawnEvent.EnemyCount; i++)
        {
            if (spawnEvent.SpawnPoint == null || spawnEvent.EnemyDefinition == null)
            {
                Debug.LogWarning($"[EnemyWaveDirector] Spawn event '{spawnEvent.EventId}' has invalid spawn point or enemy definition.");
                continue;
            }

            enemySpawner.SpawnEnemy(spawnEvent);
        }
    }

    private bool CanStartSpawnEvent(EnemySpawnEvent spawnEvent, float deltaTime)
    {
        if (spawnEvent == null || !spawnEvent.IsWaiting)
        {
            return false;
        }

        switch (spawnEvent.StartCondition)
        {
            case EnemySpawnEventStartCondition.AfterDelay:
                return TickDelay(spawnEvent, deltaTime);

            case EnemySpawnEventStartCondition.AfterSpawnEventFinished:
                if (!IsRequiredConditionCompleted(spawnEvent))
                {
                    return false;
                }

                return TickDelay(spawnEvent, deltaTime);

            default:
                return false;
        }
    }

    private bool TickDelay(EnemySpawnEvent spawnEvent, float deltaTime)
    {
        if (spawnEvent == null)
        {
            return false;
        }

        SpawnEventRuntime spawnEventRuntime = spawnEventRuntimes[spawnEvent];
        if (spawnEventRuntime != null)
        {
            spawnEventRuntime.SpawnDelayTimer += deltaTime;
            return spawnEventRuntime.SpawnDelayTimer >= spawnEvent.StartDelay;
        }

        return false;
    }

    private bool IsRequiredConditionCompleted(EnemySpawnEvent spawnEvent)
    {
        if (spawnEvent == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(spawnEvent.RequiredEventId))
        {
            return false;
        }

        if (!TryGetSpawnEventById(spawnEvent.RequiredEventId, out EnemySpawnEvent requiredEvent))
        {
            return false;
        }

        return requiredEvent.IsFinished || requiredEvent.IsResolved;
    }
}
