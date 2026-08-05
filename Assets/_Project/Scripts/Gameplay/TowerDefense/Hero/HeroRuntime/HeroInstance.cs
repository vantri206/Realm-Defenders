using System;
using UnityEngine;

[Serializable]
public class HeroInstance
{
    [SerializeField] private HeroDefinition definition;
    [SerializeField] private int level = 1;
    [SerializeField] private int star = 1;
    [SerializeField] private int deployCost = 15;
    [SerializeField] private float redeployTime = 20f;

    public HeroDefinition Definition => definition;
    public int Level => level;
    public int Star => star;
    public float RedeployTime => redeployTime;
    public int DeployCost => deployCost;
    public float RedeployCountdownTime => redeployTimer.RemainingTime;
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
        star = 1;
        deployCost = Mathf.Max(0, definition.BaseDeployCost);
        redeployTime = Mathf.Max(0f, definition.BaseRedeployTime);

        redeployTimer = new CountdownTimer(redeployTime);
    }

    public void SetLevel(int value)
    {
        level = Mathf.Max(1, value);
    }

    public void SetStar(int value)
    {
        star = Mathf.Max(1, value);
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

    public void TickRedeployTimer(float deltaTime)
    {
        redeployTimer.Tick(deltaTime);
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        deployCost = Mathf.Max(0, deployCost);
        redeployTime = Mathf.Max(0f, redeployTime);
    }
#endif
}
