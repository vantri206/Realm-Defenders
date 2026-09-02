using System;
using UnityEngine;

[Serializable]
public class ArcaneFocusSkill : BaseSkill
{
    [Header("Arcane Focus")]
    [SerializeField] private float damageBonusPerStack = 0.08f;
    [SerializeField] private int maxStackCount = 3;

    [NonSerialized] private Hurtbox focusedTarget;
    [NonSerialized] private int stackCount;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        focusedTarget = null;
        stackCount = 0;
        Owner.NormalAttackController.OnNormalAttackFired += HandleNormalAttackFired;
    }

    public override bool CanActivate()
    {
        return false;
    }

    public override void Activate()
    {
    }

    public override void ClearData()
    {
        if (Owner != null && Owner.NormalAttackController != null)
        {
            Owner.NormalAttackController.OnNormalAttackFired -= HandleNormalAttackFired;
        }

        focusedTarget = null;
        stackCount = 0;
        base.ClearData();
    }

    private void HandleNormalAttackFired(NormalAttackFiredData firedData)
    {
        if (firedData == null || firedData.Target == null)
        {
            return;
        }

        if (focusedTarget == firedData.Target)
        {
            stackCount = Mathf.Min(Mathf.Max(0, maxStackCount), stackCount + 1);
        }
        else
        {
            focusedTarget = firedData.Target;
            stackCount = 0;
        }

        firedData.RawEffectValue *= 1f + Mathf.Max(0f, damageBonusPerStack) * stackCount;
    }
}
