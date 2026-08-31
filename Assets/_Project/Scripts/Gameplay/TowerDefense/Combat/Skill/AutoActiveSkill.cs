using System;

[Serializable]
public abstract class AutoActiveSkill : BaseSkill
{
    [NonSerialized] private CountdownTimer cooldownTimer;
    [NonSerialized] private bool isActiving;

    public float CooldownRemaining => cooldownTimer.RemainingTime;
    public float CooldownTime => cooldownTimer.TotalTime;
    public bool IsActiving => isActiving;

    protected bool CanCastSkill => Owner != null && Owner.IsInitialized && Owner.CanUseSkill && !isActiving && cooldownTimer.IsFinished;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);

        cooldownTimer = new CountdownTimer(definition.Cooldown);
        isActiving = false;
        StartCooldown();
    }

    public override void Tick(float deltaTime)
    {
        if (cooldownTimer.IsRunning)
        {
            cooldownTimer.Tick(deltaTime);
        }

        if (!CanActivate())
        {
            return;
        }

        isActiving = true;
        Activate();
        StartCooldown();
    }

    protected void StartCooldown()
    {
        cooldownTimer.Reset(Definition.Cooldown);
        cooldownTimer.StartTimer();
    }

    protected void FinishSkill()
    {
        isActiving = false;
    }

    public override void ClearData()
    {
        cooldownTimer.StopTimer();
        isActiving = false;

        base.ClearData();
    }
}
