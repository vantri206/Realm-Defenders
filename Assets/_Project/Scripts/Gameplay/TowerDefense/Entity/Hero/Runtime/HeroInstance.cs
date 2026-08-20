using System;
using UnityEngine;

[Serializable]
public class HeroInstance
{
    private HeroDefinition definition;
    private UnitStats unitStats;
    private UnitSpeed unitSpeed;
    private UnitBlock heroBlock;
    private int level = 1;
    private int star = 0;
    private int deployCost = 15;
    private float redeployTime = 20f;

    private HeroDeployState deployState = HeroDeployState.Available;

    public event Action<HeroInstance, HeroDeployState> OnDeployStateChanged;

    public HeroDefinition Definition => definition;
    public int Level => level;
    public int Star => star;
    public UnitStats Stats => unitStats;
    public UnitSpeed Speed => unitSpeed;
    public UnitBlock Block => heroBlock;
    public float RedeployTime => redeployTime;
    public int DeployCost => deployCost;
    public float RedeployCountdownTime => redeployTimer.RemainingTime;
    public HeroDeployState DeployState => deployState;

    public bool IsReadyDeploy => redeployTimer.IsFinished;
    public bool IsValid => definition != null;

    private CountdownTimer redeployTimer = new CountdownTimer(0f);

    public void Initialize(HeroDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogError("[HeroInstance] HeroDefinition cannot be null.");
            return;
        }

        this.definition = definition;
        level = 1;
        star = 0;
        deployCost = Mathf.Max(0, definition.BaseDeployCost);
        redeployTime = Mathf.Max(0f, definition.BaseRedeployTime);
        unitStats = new UnitStats(definition.MaxHealth, definition.Attack, definition.AttackInterval, definition.Defense, definition.SpecialDefense);
        unitSpeed = new UnitSpeed(definition.MoveSpeed);
        heroBlock = new UnitBlock(definition.BlockCount);

        redeployTimer = new CountdownTimer(redeployTime);
    }

    public void SetLevel(int value)
    {
        level = Mathf.Max(1, value);
    }

    public void SetStar(int value)
    {
        star = Mathf.Max(0, value);
    }
    
    public void SetRedeployTime(float value)
    {
        redeployTime = Mathf.Max(0f, value);
        redeployTimer.Reset(redeployTime);
    }

    public void SetDeployCost(int value)
    {
        deployCost = Mathf.Max(0, value);
    }

    public void StartRedeployCountdown()
    {
        if (redeployTime <= 0f)
        {
            Debug.LogWarning("[HeroInstance] Redeploy time is zero or negative!");
            return;
        }

        redeployTimer.StartTimer();
    }

    public bool TickRedeployTimer(float deltaTime)
    {
        redeployTimer.Tick(deltaTime);

        if (redeployTimer.IsFinished)
        {
            return true;
        }

        return false;
    }

    public void SetDeployState(HeroDeployState state)
    {
        deployState = state;
        OnDeployStateChanged?.Invoke(this, deployState);
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        deployCost = Mathf.Max(0, deployCost);
        redeployTime = Mathf.Max(0f, redeployTime);
    }
#endif
}
