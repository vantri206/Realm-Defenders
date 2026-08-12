using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveDirector : MonoBehaviour
{
    private EnemySpawner enemySpawner;
    private IReadOnlyList<EnemySpawnEvent> spawnEvents;

    private Coroutine waveRoutine;
    private float waveStartTime;

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
            Debug.LogError("[EnemyWaveDirector] EnemySpawner must be initialized before starting director.", this);
            return;
        }

        if (waveRoutine != null)
        {
            Debug.LogWarning("[EnemyWaveDirector] Wave director is already running.", this);
            return;
        }

        ResetSpawnEvents();
        ValidateSpawnEvents();
        waveStartTime = Time.time;
        waveRoutine = StartCoroutine(RunDirectorRoutine());
    }

    public void StopDirector()
    {
        if (waveRoutine != null)
        {
            StopAllCoroutines();
            waveRoutine = null;
        }
    }

    public void ClearDirector()
    {
        StopDirector();
        waveStartTime = 0f;

        if (spawnEvents != null)
        {
            foreach (var spawnEvent in spawnEvents)
            {
                spawnEvent?.ResetState();
            }
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

    private IEnumerator RunDirectorRoutine()
    {
        while (!CheckAllSpawnFinished())
        {
            if (spawnEvents != null)
            {
                foreach (var spawnEvent in spawnEvents)
                {
                    if (!CanStartSpawnEvent(spawnEvent))
                    {
                        continue;
                    }

                    StartCoroutine(RunSpawnEventRoutine(spawnEvent));
                }
            }

            yield return null;
        }

        waveRoutine = null;
    }

    private void ResetSpawnEvents()
    {
        if (spawnEvents == null)
        {
            return;
        }

        foreach (var spawnEvent in spawnEvents)
        {
            spawnEvent?.ResetState();
        }
    }

    private void ValidateSpawnEvents()
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
                Debug.LogWarning($"[EnemyWaveDirector] Spawn event '{spawnEvent.EventId}' requires another event but has no required event id.", this);
                continue;
            }

            if (!TryGetSpawnEventById(spawnEvent.RequiredEventId, out _))
            {
                Debug.LogWarning($"[EnemyWaveDirector] Spawn event '{spawnEvent.EventId}' requires missing event '{spawnEvent.RequiredEventId}'.", this);
            }
        }
    }

    private IEnumerator RunSpawnEventRoutine(EnemySpawnEvent spawnEvent)
    {
        if (spawnEvent == null)
        {
            yield break;
        }

        spawnEvent.MarkSpawning();

        int spawnCount = Mathf.Max(0, spawnEvent.Count);
        float spawnInterval = Mathf.Max(0f, spawnEvent.Interval);

        for (int i = 0; i < spawnCount; i++)
        {
            enemySpawner.SpawnEnemy(spawnEvent);

            if (i < spawnCount - 1 && spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        spawnEvent.MarkFinished();
    }

    private bool CanStartSpawnEvent(EnemySpawnEvent spawnEvent)
    {
        if (spawnEvent == null || !spawnEvent.IsWaiting)
        {
            return false;
        }

        switch (spawnEvent.StartCondition)
        {
            case EnemySpawnEventStartCondition.AfterDelay:
                return IsDelayConditionCompleted(spawnEvent);

            case EnemySpawnEventStartCondition.AfterSpawnEventFinished:
                return IsRequiredConditionCompleted(spawnEvent) && IsDelayConditionCompleted(spawnEvent);

            default:
                return false;
        }
    }

    private bool IsDelayConditionCompleted(EnemySpawnEvent spawnEvent)
    {
        if (spawnEvent == null)
        {
            return false;
        }

        return Time.time - waveStartTime >= Mathf.Max(0f, spawnEvent.StartDelay);
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
