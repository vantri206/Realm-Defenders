using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTrackingData
{
    public bool IsObjectiveEnemy;
    public int LivesDamage;
    public int MeatReward;

    public EnemyTrackingData(bool isObjectiveEnemy, int livesDamage, int meatReward)
    {
        IsObjectiveEnemy = isObjectiveEnemy;
        LivesDamage = livesDamage;
        MeatReward = meatReward;
    }
}

public class StageSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStageStatsView stageStatsUI;
    
    [Header("Stage Config")]
    [SerializeField] private int startingMeat = 20;
    [SerializeField] private int startingLives = 10;
    [SerializeField] private int meatNaturalPerSecond = 1;

    // Stage states
    private CountdownTimer meatNaturalTimer;
    private int stageEnemyCount;
    private float meatNaturalSpeedMultiplier = 1f;
    private bool isStageEnded = false;

    // Stage stats
    private int currentMeat;
    private int currentLives;
    private int resolvedObjectiveEnemies;
    private int spawnedObjectiveEnemies;
    private Dictionary<EnemyRuntime, EnemyTrackingData> activeEnemies;
    private CombatTimeController combatTime;

    private bool isInitialized = false;

    public int StartingMeat => startingMeat;
    public int StartingLives => startingLives;
    public int CurrentMeat => currentMeat;
    public int CurrentLives => currentLives;
    public int ResolvedObjectiveEnemies => resolvedObjectiveEnemies;
    public int SpawnedObjectiveEnemies => spawnedObjectiveEnemies;
    public int StageEnemyCount => stageEnemyCount;
    public CountdownTimer MeatNaturalTimer => meatNaturalTimer;
    public IReadOnlyDictionary<EnemyRuntime, EnemyTrackingData> ActiveEnemies => activeEnemies;

    public bool IsAllEnemiesSpawned => spawnedObjectiveEnemies >= stageEnemyCount;

    public event Action OnStageStatsChanged;

    public bool IsInitialized => isInitialized;

    public void Initialize(int startingMeat, int startingLives, int totalEnemyCount, CombatTimeController combatTime)
    {
        if (isInitialized)
        {
            Debug.LogWarning("[StageSystem] StageSystem is already initialized.");
            return;
        }

        if (combatTime == null)
        {
            Debug.LogError("[StageSystem] CombatTimeController is required to initialize stage system.", this);
            return;
        }

        this.combatTime = combatTime;

        currentMeat = Mathf.Clamp(startingMeat, 0, GameplayConstants.MAX_MEAT);
        currentLives = startingLives;
        stageEnemyCount = totalEnemyCount;

        resolvedObjectiveEnemies = 0;
        spawnedObjectiveEnemies = 0;
        activeEnemies = new Dictionary<EnemyRuntime, EnemyTrackingData>();

        if (stageStatsUI != null)
        {
            stageStatsUI.Initialize(this);
        }

        meatNaturalTimer = new CountdownTimer(GameplayConstants.SECOND);
        meatNaturalTimer.OnTimeStop += OnGainMeatNatural;
        meatNaturalTimer.StartTimer();

        isInitialized = true;
    }

    private void Update()
    {
        if (isStageEnded || !isInitialized)
        {
            return;
        }

        if (IsStageCompleted())
        {
            Debug.Log("[StageSystem] Stage Completed!");
            isStageEnded = true;
        }
        else if (IsStageFailed())
        {
            Debug.Log("[StageSystem] Stage Failed!");
            isStageEnded = true;
        }

        if (meatNaturalTimer != null)
        {
            if (meatNaturalTimer.IsRunning)
            {
                meatNaturalTimer.Tick(combatTime.CombatDeltaTime * meatNaturalSpeedMultiplier);
            }

            if (!meatNaturalTimer.IsRunning)
            {
                meatNaturalTimer.Reset();
                meatNaturalTimer.StartTimer();
            }
        }
    }

    private void OnGainMeatNatural()
    {
        OnGainMeat(meatNaturalPerSecond);
    }

    public void SetMeatNaturalSpeedMultiplier(float multiplier)
    {
        meatNaturalSpeedMultiplier = Mathf.Max(multiplier, 0f);
    }

    public void RegisterEnemy(EnemyRuntime enemy, EnemyTrackingData trackingData)
    {
        if (activeEnemies.ContainsKey(enemy))
        {
            Debug.LogWarning($"[StageSystem] Enemy {enemy.name} is already registered in the stage system.");
            return;
        }

        if (trackingData.IsObjectiveEnemy)
        {
            spawnedObjectiveEnemies++;
        }

        activeEnemies.Add(enemy, trackingData);
       
        NotifyStageStatsChanged();
    }

    public void ResolveEnemy(EnemyRuntime enemy, EnemyResolveReason resolveReason)
    {
        if(!activeEnemies.TryGetValue(enemy, out var trackingData))
        {
            return;
        }

        activeEnemies.Remove(enemy);

        if (trackingData.IsObjectiveEnemy)
        {
            resolvedObjectiveEnemies++;
        }
        if (resolveReason == EnemyResolveReason.Escaped)
        {
            OnLoseLife(trackingData.LivesDamage, false);
        }
        else if (resolveReason == EnemyResolveReason.Killed)
        {
            OnGainMeat(trackingData.MeatReward, false);
        }

        NotifyStageStatsChanged();
    }

    public void OnLoseLife(int lives, bool isNotify = true)
    {
        currentLives -= lives;

        if (isNotify)
        {
            NotifyStageStatsChanged();
        }
    }

    public void OnGainMeat(int meat, bool isNotify = true)
    {
        currentMeat = Mathf.Min(currentMeat + Mathf.Max(0, meat), GameplayConstants.MAX_MEAT);

        if (isNotify)
        {
            NotifyStageStatsChanged();
        }
    }

    public void RefundRetreatMeat(int deployCost)
    {
        int refundMeat = Mathf.FloorToInt(Mathf.Max(0, deployCost) * GameplayConstants.RETREAT_MEAT_REFUND_RATE);
        int previousMeat = currentMeat;

        OnGainMeat(refundMeat);
    }

    public bool TrySpendMeat(int meat)
    {
        if (currentMeat >= meat)
        {
            currentMeat -= meat;
            NotifyStageStatsChanged();
            return true;
        }
        return false;
    }

    public bool IsStageCompleted()
    {
        return IsAllEnemiesSpawned && resolvedObjectiveEnemies >= spawnedObjectiveEnemies && activeEnemies.Count == 0 && currentLives > 0;
    }

    public bool IsStageFailed()
    {
        return currentLives <= 0;
    }

    public bool CanDeployHero(int deployCost)
    {
        if (currentMeat >= deployCost)
        {
            return true;
        }
        return false;
    }

    private void NotifyStageStatsChanged()
    {
        OnStageStatsChanged?.Invoke();
    }
}
