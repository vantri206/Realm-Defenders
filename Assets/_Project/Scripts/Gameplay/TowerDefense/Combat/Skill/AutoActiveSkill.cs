using System;

[Serializable]
public abstract class AutoActiveSkill : BaseSkill
{
    [NonSerialized] private CountdownTimer cooldownTimer;
    [NonSerialized] private bool isActiving;

    public float CooldownRemaining => cooldownTimer.RemainingTime;
    public float CooldownTime => cooldownTimer.TotalTime;
    public bool IsActiving => isActiving;
    public bool ActivatedThisTick { get; private set; }

    protected bool CanCastSkill => Owner != null && Owner.IsInitialized && Owner.CanUseSkill && !isActiving && cooldownTimer.IsFinished;
    protected virtual bool InterruptsNormalAttack => true;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);

        cooldownTimer = new CountdownTimer(definition.Cooldown);
        isActiving = false;
        ActivatedThisTick = false;
        StartCooldown();
    }

    public override void Tick(float deltaTime)
    {
        ActivatedThisTick = false;

        if (cooldownTimer.IsRunning)
        {
            cooldownTimer.Tick(deltaTime);
        }

        if (!CanActivate())
        {
            return;
        }

        isActiving = true;
        ActivatedThisTick = true;
        if (InterruptsNormalAttack)
        {
            Owner.CancelNormalAttackForSkill();
        }

        Owner.PlayActionSound();
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
        if (!isActiving)
        {
            return;
        }

        isActiving = false;

        if (InterruptsNormalAttack && Owner != null && Owner.NormalAttackController != null)
        {
            Owner.NormalAttackController.ResumeNormalAttack();
        }
    }

    public override void ClearData()
    {
        cooldownTimer.StopTimer();
        isActiving = false;
        ActivatedThisTick = false;

        base.ClearData();
    }
}
