using System;
using UnityEngine;

[Serializable]
public class IronGuardSkill : BaseSkill
{
    private const string defenseModifierId = "SK02_Defense";
    private const string specialDefenseModifierId = "SK02_SpecialDefense";

    [Header("Iron Guard")]
    [SerializeField] private int minimumBlockedEnemyCount = 1;
    [SerializeField] private float defenseBonus = 0.2f;
    [SerializeField] private float specialDefenseBonus = 0.2f;

    [NonSerialized] private UnitStatModifier defenseModifier;
    [NonSerialized] private UnitStatModifier specialDefenseModifier;
    [NonSerialized] private bool isApplied;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        defenseModifier = new UnitStatModifier(UnitStatType.Defense, UnitStatModifierType.AdditivePercent, defenseBonus, defenseModifierId);
        specialDefenseModifier = new UnitStatModifier(UnitStatType.SpecialDefense, UnitStatModifierType.AdditivePercent, specialDefenseBonus, specialDefenseModifierId);
        isApplied = false;
    }

    public override void Tick(float deltaTime)
    {
        if (CanActivate())
        {
            Activate();
            return;
        }

        RemoveBonuses();
    }

    public override bool CanActivate()
    {
        return Owner != null && !Owner.IsDead && Owner.CurrentBlock >= minimumBlockedEnemyCount;
    }

    public override void Activate()
    {
        if (isApplied)
        {
            return;
        }

        ApplyBonuses();
    }

    public override void ClearData()
    {
        RemoveBonuses();
        base.ClearData();
    }

    private void ApplyBonuses()
    {
        if (isApplied)
        {
            return;
        }

        Owner.Stats.AddModifier(defenseModifier);
        Owner.Stats.AddModifier(specialDefenseModifier);
        isApplied = true;
    }

    private void RemoveBonuses()
    {
        if (!isApplied)
        {
            return;
        }

        Owner.Stats.RemoveModifier(defenseModifier);
        Owner.Stats.RemoveModifier(specialDefenseModifier);
        isApplied = false;
    }
}
