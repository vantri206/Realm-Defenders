using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTrackingData
{
    public bool IsObjectiveEnemy;
    public int LivesDamage;
    public int FoodReward;

    public EnemyTrackingData(bool isObjectiveEnemy, int livesDamage, int foodReward)
    {
        IsObjectiveEnemy = isObjectiveEnemy;
        LivesDamage = livesDamage;
        FoodReward = foodReward;
    }
}

public class LevelSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelStatsUI levelStatsUI;
    
    [Header("Level Config")]
    [SerializeField] private int startingFood = 20;
    [SerializeField] private int startingLives = 10;
    [SerializeField] private int foodNaturalPerSecond = 1;

    // Level states
    private CountdownTimer foodNaturalTimer;
    private int levelEnemyCount;
    private float foodNaturalSpeedMultiplier = 1f;
    private bool isLevelEnded = false;

    // Level stats
    private int currentFood;
    private int currentLives;
    private int resolvedObjectiveEnemies;
    private int spawnedObjectiveEnemies;
    private Dictionary<EnemyRuntime, EnemyTrackingData> activeEnemies;
    private CombatTimeController combatTime;

    private bool isInitialized = false;

    public int StartingFood => startingFood;
    public int StartingLives => startingLives;
    public int CurrentFood => currentFood;
    public int CurrentLives => currentLives;
    public int ResolvedObjectiveEnemies => resolvedObjectiveEnemies;
    public int SpawnedObjectiveEnemies => spawnedObjectiveEnemies;
    public int LevelEnemyCount => levelEnemyCount;
    public CountdownTimer FoodNaturalTimer => foodNaturalTimer;
    public IReadOnlyDictionary<EnemyRuntime, EnemyTrackingData> ActiveEnemies => activeEnemies;

    public bool IsAllEnemiesSpawned => spawnedObjectiveEnemies >= levelEnemyCount;

    public event Action OnLevelStatsChanged;

    public bool IsInitialized => isInitialized;

    public void Initialize(int startingFood, int startingLives, int totalEnemyCount, CombatTimeController combatTime)
    {
        if (isInitialized)
        {
            Debug.LogWarning("[LevelSystem] LevelSystem is already initialized.");
            return;
        }

        if (combatTime == null)
        {
            Debug.LogError("[LevelSystem] CombatTimeController is required to initialize level system.", this);
            return;
        }

        this.combatTime = combatTime;

        currentFood = Mathf.Clamp(startingFood, 0, GameplayConstants.MAX_FOOD);
        currentLives = startingLives;
        levelEnemyCount = totalEnemyCount;

        resolvedObjectiveEnemies = 0;
        spawnedObjectiveEnemies = 0;
        activeEnemies = new Dictionary<EnemyRuntime, EnemyTrackingData>();

        if (levelStatsUI != null)
        {
            levelStatsUI.Initialize(this);
        }

        foodNaturalTimer = new CountdownTimer(GameplayConstants.SECOND);
        foodNaturalTimer.OnTimeStop += OnGainFoodNatural;
        foodNaturalTimer.StartTimer();

        isInitialized = true;
    }

    private void Update()
    {
        if (isLevelEnded || !isInitialized)
        {
            return;
        }

        if (IsLevelCompleted())
        {
            Debug.Log("Level Completed!");
            isLevelEnded = true;
        }
        else if (IsLevelFailed())
        {
            Debug.Log("Level Failed!");
            isLevelEnded = true;
        }

        if (foodNaturalTimer != null)
        {
            if (foodNaturalTimer.IsRunning)
            {
                foodNaturalTimer.Tick(combatTime.CombatDeltaTime * foodNaturalSpeedMultiplier);
            }

            if (!foodNaturalTimer.IsRunning)
            {
                foodNaturalTimer.Reset();
                foodNaturalTimer.StartTimer();
            }
        }
    }

    private void OnGainFoodNatural()
    {
        OnGainFood(foodNaturalPerSecond);
    }

    public void SetFoodNaturalSpeedMultiplier(float multiplier)
    {
        foodNaturalSpeedMultiplier = Mathf.Max(multiplier, 0f);
    }

    public void RegisterEnemy(EnemyRuntime enemy, EnemyTrackingData trackingData)
    {
        if (activeEnemies.ContainsKey(enemy))
        {
            Debug.LogWarning($"Enemy {enemy.name} is already registered in the level system.");
            return;
        }

        if (trackingData.IsObjectiveEnemy)
        {
            spawnedObjectiveEnemies++;
        }

        activeEnemies.Add(enemy, trackingData);
       
        NotifyLevelStatsChanged();
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
            OnGainFood(trackingData.FoodReward, false);
        }

        NotifyLevelStatsChanged();
    }

    public void OnLoseLife(int lives, bool isNotify = true)
    {
        currentLives -= lives;

        if (isNotify)
        {
            NotifyLevelStatsChanged();
        }
    }

    public void OnGainFood(int food, bool isNotify = true)
    {
        currentFood = Mathf.Min(currentFood + Mathf.Max(0, food), GameplayConstants.MAX_FOOD);

        if (isNotify)
        {
            NotifyLevelStatsChanged();
        }
    }

    public void RefundRetreatFood(int deployCost)
    {
        int refundFood = Mathf.FloorToInt(Mathf.Max(0, deployCost) * GameplayConstants.RETREAT_FOOD_REFUND_RATE);
        int previousFood = currentFood;

        OnGainFood(refundFood);
    }

    public bool TrySpendFood(int food)
    {
        if (currentFood >= food)
        {
            currentFood -= food;
            NotifyLevelStatsChanged();
            return true;
        }
        return false;
    }

    public bool IsLevelCompleted()
    {
        return IsAllEnemiesSpawned && resolvedObjectiveEnemies >= spawnedObjectiveEnemies && activeEnemies.Count == 0 && currentLives > 0;
    }

    public bool IsLevelFailed()
    {
        return currentLives <= 0;
    }

    public bool CanDeployHero(int deployCost)
    {
        if (currentFood >= deployCost)
        {
            return true;
        }
        return false;
    }

    private void NotifyLevelStatsChanged()
    {
        OnLevelStatsChanged?.Invoke();
    }
}
