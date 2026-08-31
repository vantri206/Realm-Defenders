using System;

[Serializable]
public abstract class BaseSkill
{
    [NonSerialized] private HeroRuntime owner;
    [NonSerialized] private SkillDefinition definition;

    protected HeroRuntime Owner => owner;
    protected SkillDefinition Definition => definition;

    public virtual void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        this.owner = owner;
        this.definition = definition;
    }

    public virtual void Tick(float deltaTime)
    {
        if (CanActivate())
        {
            Activate();
        }
    }

    public abstract bool CanActivate();
    public abstract void Activate();

    public virtual void ClearData()
    {
        owner = null;
        definition = null;
    }
}
