using System;
using UnityEngine;

[Serializable]
public class GuardianOfDeadSkill : AutoActiveSkill
{
    private const string DefenseStatusId = "SK11_GuardianOfDead";
    private const string DefenseModifierId = "SK11_Defense";
    private const string SpecialDefenseModifierId = "SK11_SpecialDefense";

    [Header("Guardian of the Dead")]
    [SerializeField] private float defenseBonus = 0.3f;
    [SerializeField] private float specialDefenseBonus = 0.3f;
    [SerializeField] private float duration = 6f;

    protected override bool InterruptsNormalAttack => false;

    public override bool CanActivate()
    {
        return CanCastSkill && Owner.CurrentBlock > 0;
    }

    public override void Activate()
    {
        UnitStatModifier[] modifiers =
        {
            new UnitStatModifier(UnitStatType.Defense, UnitStatModifierType.AdditivePercent, defenseBonus, DefenseModifierId),
            new UnitStatModifier(UnitStatType.SpecialDefense, UnitStatModifierType.AdditivePercent, specialDefenseBonus, SpecialDefenseModifierId)
        };

        Owner.ApplyTemporaryStatModifiers(DefenseStatusId, Owner.gameObject, modifiers, Mathf.Max(0f, duration));
        FinishSkill();
    }
}
