using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveDirector
{
    private class SpawnEventRuntime
    {
        public EnemySpawnEventState State = EnemySpawnEventState.Waiting;
        public int SpawnedGroupCount;
        public int ActiveEnemyCount;
        public float SpawnIntervalTimer;
        public float SpawnDelayTimer;
    }

    private readonly Dictionary<EnemySpawnEventDefinition, SpawnEventRuntime> spawnEventRuntimes = new Dictionary<EnemySpawnEventDefinition, SpawnEventRuntime>();
    private readonly Dictionary<string, EnemySpawnEventDefinition> spawnEventsById = new Dictionary<string, EnemySpawnEventDefinition>();
    private readonly Dictionary<EnemyRuntime, SpawnEventRuntime> trackedEnemies = new Dictionary<EnemyRuntime, SpawnEventRuntime>();

    private EnemySpawner enemySpawner;
    private IReadOnlyList<EnemySpawnEventDefinition> spawnEvents;
    private bool isRunning;

    public void Initialize(EnemySpawner enemySpawner, IReadOnlyList<EnemySpawnEventDefinition> spawnEvents)
    {
        StopDirector();
        ClearTracking();

        if (this.enemySpawner != null)
        {
            this.enemySpawner.OnEnemyReplaced -= HandleEnemyReplaced;
        }

        this.enemySpawner = enemySpawner;
        this.spawnEvents = spawnEvents;
        if (this.enemySpawner != null)
        {
            this.enemySpawner.OnEnemyReplaced += HandleEnemyReplaced;
        }

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
        isRunning = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isRunning || spawnEvents == null)
        {
            return;
        }

        for (int i = 0; i < spawnEvents.Count; i++)
        {
            EnemySpawnEventDefinition spawnEvent = spawnEvents[i];
            if (spawnEvent == null || !spawnEventRuntimes.TryGetValue(spawnEvent, out SpawnEventRuntime runtime))
            {
                continue;
            }

            if (runtime.State == EnemySpawnEventState.Waiting && CanStartSpawnEvent(spawnEvent, runtime, deltaTime))
            {
                runtime.State = EnemySpawnEventState.Spawning;
            }

            if (runtime.State == EnemySpawnEventState.Spawning)
            {
                TickSpawnEvent(spawnEvent, runtime, deltaTime);
            }
        }

        if (CheckAllSpawnResolved())
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
        ClearTracking();
        spawnEventRuntimes.Clear();
        spawnEventsById.Clear();

        if (enemySpawner != null)
        {
            enemySpawner.OnEnemyReplaced -= HandleEnemyReplaced;
        }

        enemySpawner = null;
        spawnEvents = null;
    }

    public bool CheckAllSpawnFinished()
    {
        if (spawnEvents == null || spawnEvents.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < spawnEvents.Count; i++)
        {
            EnemySpawnEventDefinition spawnEvent = spawnEvents[i];
            if (spawnEvent != null && spawnEventRuntimes.TryGetValue(spawnEvent, out SpawnEventRuntime runtime) &&
                runtime.State != EnemySpawnEventState.Finished && runtime.State != EnemySpawnEventState.Resolved)
            {
                return false;
            }
        }

        return true;
    }

    public bool CheckAllSpawnResolved()
    {
        if (spawnEvents == null || spawnEvents.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < spawnEvents.Count; i++)
        {
            EnemySpawnEventDefinition spawnEvent = spawnEvents[i];
            if (spawnEvent != null && (!spawnEventRuntimes.TryGetValue(spawnEvent, out SpawnEventRuntime runtime) || runtime.State != EnemySpawnEventState.Resolved))
            {
                return false;
            }
        }

        return true;
    }

    private void ResetSpawnEvents()
    {
        ClearTracking();
        spawnEventRuntimes.Clear();
        spawnEventsById.Clear();

        if (spawnEvents == null)
        {
            return;
        }

        for (int i = 0; i < spawnEvents.Count; i++)
        {
            EnemySpawnEventDefinition spawnEvent = spawnEvents[i];
            if (spawnEvent == null)
            {
                continue;
            }

            spawnEventRuntimes.Add(spawnEvent, new SpawnEventRuntime());
            if (!string.IsNullOrWhiteSpace(spawnEvent.EventId))
            {
                spawnEventsById[spawnEvent.EventId] = spawnEvent;
            }
        }
    }

    private bool CanStartSpawnEvent(EnemySpawnEventDefinition spawnEvent, SpawnEventRuntime runtime, float deltaTime)
    {
        if (spawnEvent.StartCondition == EnemySpawnEventStartCondition.AfterSpawnEventResolved)
        {
            if (string.IsNullOrWhiteSpace(spawnEvent.RequiredEventId) ||
                !spawnEventsById.TryGetValue(spawnEvent.RequiredEventId, out EnemySpawnEventDefinition requiredEvent) ||
                !spawnEventRuntimes.TryGetValue(requiredEvent, out SpawnEventRuntime requiredRuntime) ||
                requiredRuntime.State != EnemySpawnEventState.Resolved)
            {
                return false;
            }
        }

        runtime.SpawnDelayTimer += deltaTime;
        return runtime.SpawnDelayTimer >= Mathf.Max(0f, spawnEvent.StartDelay);
    }

    private void TickSpawnEvent(EnemySpawnEventDefinition spawnEvent, SpawnEventRuntime runtime, float deltaTime)
    {
        int groupCount = Mathf.Max(0, spawnEvent.GroupCount);
        if (runtime.SpawnedGroupCount >= groupCount)
        {
            MarkSpawnFinished(runtime);
            return;
        }

        runtime.SpawnIntervalTimer -= deltaTime;
        if (runtime.SpawnIntervalTimer > 0f)
        {
            return;
        }

        float groupInterval = Mathf.Max(0f, spawnEvent.GroupInterval);
        while (runtime.SpawnedGroupCount < groupCount)
        {
            SpawnEnemyGroup(spawnEvent, runtime);
            runtime.SpawnedGroupCount++;

            if (runtime.SpawnedGroupCount >= groupCount)
            {
                MarkSpawnFinished(runtime);
                return;
            }

            if (groupInterval > 0f)
            {
                runtime.SpawnIntervalTimer = groupInterval;
                return;
            }
        }
    }

    private void SpawnEnemyGroup(EnemySpawnEventDefinition spawnEvent, SpawnEventRuntime runtime)
    {
        for (int i = 0; i < Mathf.Max(0, spawnEvent.EnemiesPerGroup); i++)
        {
            EnemyRuntime enemy = enemySpawner.SpawnEnemy(spawnEvent);
            if (enemy == null)
            {
                continue;
            }

            runtime.ActiveEnemyCount++;
            trackedEnemies.Add(enemy, runtime);
            enemy.OnDestroyed += HandleEnemyDestroyed;
            enemy.OnEscaped += HandleEnemyEscaped;
        }
    }

    private void MarkSpawnFinished(SpawnEventRuntime runtime)
    {
        if (runtime.State == EnemySpawnEventState.Finished || runtime.State == EnemySpawnEventState.Resolved)
        {
            return;
        }

        if (runtime.ActiveEnemyCount > 0)
        {
            runtime.State = EnemySpawnEventState.Finished;
            return;
        }

        runtime.State = EnemySpawnEventState.Resolved;
    }

    private void HandleEnemyDestroyed(UnitRuntime unitRuntime)
    {
        if (unitRuntime is EnemyRuntime enemy)
        {
            ResolveTrackedEnemy(enemy);
        }
    }

    private void HandleEnemyEscaped(EnemyRuntime enemy)
    {
        ResolveTrackedEnemy(enemy);
    }

    private void HandleEnemyReplaced(EnemyRuntime source, EnemyRuntime replacement)
    {
        if (source == null || replacement == null || trackedEnemies.ContainsKey(replacement) ||
            !trackedEnemies.TryGetValue(source, out SpawnEventRuntime runtime))
        {
            return;
        }

        source.OnDestroyed -= HandleEnemyDestroyed;
        source.OnEscaped -= HandleEnemyEscaped;
        trackedEnemies.Remove(source);

        trackedEnemies.Add(replacement, runtime);
        replacement.OnDestroyed += HandleEnemyDestroyed;
        replacement.OnEscaped += HandleEnemyEscaped;
    }

    private void ResolveTrackedEnemy(EnemyRuntime enemy)
    {
        if (enemy == null || !trackedEnemies.TryGetValue(enemy, out SpawnEventRuntime runtime))
        {
            return;
        }

        enemy.OnDestroyed -= HandleEnemyDestroyed;
        enemy.OnEscaped -= HandleEnemyEscaped;
        trackedEnemies.Remove(enemy);
        runtime.ActiveEnemyCount = Mathf.Max(0, runtime.ActiveEnemyCount - 1);

        if (runtime.State == EnemySpawnEventState.Finished && runtime.ActiveEnemyCount == 0)
        {
            runtime.State = EnemySpawnEventState.Resolved;
        }
    }

    private void ClearTracking()
    {
        foreach (EnemyRuntime enemy in trackedEnemies.Keys)
        {
            if (enemy != null)
            {
                enemy.OnDestroyed -= HandleEnemyDestroyed;
                enemy.OnEscaped -= HandleEnemyEscaped;
            }
        }

        trackedEnemies.Clear();
    }
}
