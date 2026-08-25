using System;
using UnityEngine;

public enum UnitStatModifierType
{
    FlatBase,
    AdditivePercent,
    FinalMultiplier,
    FlatFinal
}

[Serializable]
public struct UnitStatModifier
{
    [SerializeField] private UnitStatType statType;
    [SerializeField] private UnitStatModifierType modifierType;
    [SerializeField] private float value;
    [SerializeField] private string modifierId;

    public UnitStatType StatType => statType;
    public UnitStatModifierType ModifierType => modifierType;
    public float Value => value;
    public string ModifierId => modifierId;

    public bool IsValid => !string.IsNullOrWhiteSpace(modifierId) && !float.IsNaN(value);

    public UnitStatModifier(UnitStatType statType, UnitStatModifierType modifierType, float value, string modifierId)
    {
        this.statType = statType;
        this.modifierType = modifierType;
        this.value = value;
        this.modifierId = modifierId;
    }
}
