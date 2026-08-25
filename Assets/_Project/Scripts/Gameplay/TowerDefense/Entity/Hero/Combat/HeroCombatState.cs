using System;
using UnityEngine;

public class HeroCombatState
{
    private HeroInstance heroInstance;
    private UnitStats stats = new UnitStats();
    private int awakenRank;
    private int deployCost;
    private float redeployTime;
    private HeroDeployState deployState = HeroDeployState.Available;

    private CountdownTimer redeployTimer = new CountdownTimer(0f);

    public UnitStats Stats => stats;
    public HeroInstance HeroInstance => heroInstance;
    public UnitBreakdownStats FinalStats => stats.FinalStats;
    public int AwakenRank => awakenRank;
    public int DeployCost => deployCost;
    public float RedeployTime => redeployTime;
    public float RedeployCountdownTime => redeployTimer.RemainingTime;
    public HeroDeployState DeployState => deployState;
    public bool IsReadyDeploy => redeployTimer.IsFinished;
    public bool IsValid => heroInstance != null && heroInstance.IsValid;
    public HeroDefinition Definition => IsValid ? heroInstance.Definition : null;

    public event Action<HeroCombatState, HeroDeployState> OnDeployStateChanged;

    public HeroCombatState() { }

    public HeroCombatState(HeroInstance heroInstance)
    {
        Initialize(heroInstance);
    }

    public bool Initialize(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogError("[HeroCombatState] A valid HeroInstance is required.");
            return false;
        }

        this.heroInstance = heroInstance;
        stats = new UnitStats(heroInstance.Stats);
        awakenRank = 0;
        deployCost = Mathf.Max(0, heroInstance.Definition.BaseDeployCost);
        redeployTime = Mathf.Max(0f, heroInstance.Definition.BaseRedeployTime);
        deployState = HeroDeployState.Available;
        redeployTimer = new CountdownTimer(redeployTime);
        return true;
    }

    public void SetAwakenRank(int rank)
    {
        awakenRank = Mathf.Max(0, rank);
    }

    public void SetDeployCost(int value)
    {
        deployCost = Mathf.Max(0, value);
    }

    public void SetRedeployTime(float value)
    {
        redeployTime = Mathf.Max(0f, value);
        redeployTimer.Reset(redeployTime);
    }

    public void StartRedeployCountdown()
    {
        if (redeployTime <= 0f)
        {
            SetDeployState(HeroDeployState.Available);
            return;
        }

        SetDeployState(HeroDeployState.Countdown);
        redeployTimer.Reset(redeployTime);
        redeployTimer.StartTimer();
    }

    public bool TickRedeployTimer(float deltaTime)
    {
        redeployTimer.Tick(deltaTime);
        return redeployTimer.IsFinished;
    }

    public void SetDeployState(HeroDeployState state)
    {
        if (deployState == state)
        {
            return;
        }

        deployState = state;
        OnDeployStateChanged?.Invoke(this, deployState);
    }
}
