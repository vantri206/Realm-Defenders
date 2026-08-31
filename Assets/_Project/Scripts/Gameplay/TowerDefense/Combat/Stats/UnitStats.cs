using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnitStats
{
    public const float MinAttackInterval = 0.01f;

    [SerializeField] private UnitBaseStats baseStats = new UnitBaseStats();
    [SerializeField] private List<UnitStatModifier> modifiers = new List<UnitStatModifier>();

    private UnitBreakdownStats finalStats;
    private bool needsResolve;

    public UnitBaseStats BaseStats => baseStats;
    public IReadOnlyList<UnitStatModifier> Modifiers => modifiers;
    public UnitBreakdownStats FinalStats
    {
        get
        {
            if (needsResolve || finalStats == null)
            {
                ResolveFinalStats();
            }

            return finalStats;
        }
    }

    public float MaxHealth => FinalStats.MaxHealth;
    public float Attack => FinalStats.Attack;
    public float AttackInterval => FinalStats.AttackInterval;
    public float Defense => FinalStats.Defense;
    public float SpecialDefense => FinalStats.SpecialDefense;
    public float MoveSpeed => FinalStats.MoveSpeed;
    public int BlockCount => FinalStats.BlockCount;

    public event Action OnStatsChanged;

    public UnitStats()
    {
        SetBaseStats(new UnitBaseStats());
    }

    public UnitStats(UnitBaseStats baseStats)
    {
        SetBaseStats(baseStats);
    }

    public UnitStats(UnitStats source)
    {
        if (source == null)
        {
            return;
        }

        baseStats = new UnitBaseStats(source.baseStats);
        modifiers = source.modifiers != null ? new List<UnitStatModifier>(source.modifiers) : new List<UnitStatModifier>();
        needsResolve = true;
    }

    public void SetBaseStats(UnitBaseStats value)
    {
        baseStats = value != null ? new UnitBaseStats(value) : new UnitBaseStats();
        needsResolve = true;
        OnStatsChanged?.Invoke();
    }

    public bool AddModifier(UnitStatModifier modifier)
    {
        if (modifiers == null)
        {
            modifiers = new List<UnitStatModifier>();
        }

        if (!modifier.IsValid)
        {
            return false;
        }

        modifiers.Add(modifier);
        needsResolve = true;
        OnStatsChanged?.Invoke();
        return true;
    }

    public bool RemoveModifier(UnitStatModifier modifier)
    {
        if (modifiers == null || modifiers.Count == 0 || !modifier.IsValid)
        {
            return false;
        }

        bool isRemoved = modifiers.Remove(modifier);

        if (isRemoved)
        {
            needsResolve = true;
            OnStatsChanged?.Invoke();
        }

        return isRemoved;
    }

    public void RemoveModifiers(IReadOnlyList<UnitStatModifier> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        int removedCount = 0;

        for (int i = 0; i < modifiers.Count; i++)
        {
            if (RemoveModifier(modifiers[i]))
            {
                removedCount++;
            }
        }
    }

    public void RemoveModifiersById(string modifierId)
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(modifierId))
        {
            return;
        }

        int removedCount = modifiers.RemoveAll(modifier => string.Equals(modifier.ModifierId, modifierId, StringComparison.Ordinal));

        if (removedCount > 0)
        {
            needsResolve = true;
            OnStatsChanged?.Invoke();
        }
    }

    public void ClearModifiers()
    {
        if (modifiers == null)
        {
            return;
        }

        if (modifiers.Count == 0)
        {
            return;
        }

        modifiers.Clear();
        needsResolve = true;
        OnStatsChanged?.Invoke();
    }

    public UnitBreakdownStats ResolveFinalStats()
    {
        if (baseStats == null)
        {
            baseStats = new UnitBaseStats();
        }

        if (modifiers == null)
        {
            modifiers = new List<UnitStatModifier>();
        }

        finalStats = new UnitBreakdownStats(
            ResolveStat(UnitStatType.MaxHealth),
            ResolveStat(UnitStatType.Attack),
            ResolveStat(UnitStatType.AttackInterval),
            ResolveStat(UnitStatType.Defense),
            ResolveStat(UnitStatType.SpecialDefense),
            ResolveStat(UnitStatType.MoveSpeed),
            ResolveStat(UnitStatType.BlockCount)
        );

        needsResolve = false;
        return finalStats;
    }

    private UnitStatBreakdown ResolveStat(UnitStatType statType)
    {
        float baseValue = baseStats.GetValue(statType);
        float flatBase = 0f;
        float additivePercent = 0f;
        float finalMultiplier = 1f;
        float flatFinal = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            UnitStatModifier modifier = modifiers[i];
            if (!modifier.IsValid || modifier.StatType != statType)
            {
                continue;
            }

            switch (modifier.ModifierType)
            {
                case UnitStatModifierType.FlatBase:
                    flatBase += modifier.Value;
                    break;

                case UnitStatModifierType.AdditivePercent:
                    additivePercent += modifier.Value;
                    break;

                case UnitStatModifierType.FinalMultiplier:
                    finalMultiplier *= modifier.Value;
                    break;
                case UnitStatModifierType.FlatFinal:
                    flatFinal += modifier.Value;
                    break;
                default:
                    Debug.LogWarning($"[UnitStats] Unknown modifier type: {modifier.ModifierType}");
                    break;
            }
        }

        float rawValue = (baseValue + flatBase) * (1f + additivePercent) * finalMultiplier + flatFinal;
        float finalValue = NormalizeFinalValue(statType, rawValue);

        return new UnitStatBreakdown(baseValue, flatBase, additivePercent, finalMultiplier, finalValue);
    }

    private static float NormalizeFinalValue(UnitStatType statType, float value)
    {
        if (float.IsNaN(value))
        {
            return 0f;
        }

        return statType switch
        {
            UnitStatType.AttackInterval => Mathf.Max(MinAttackInterval, value),
            UnitStatType.BlockCount => Mathf.Max(0f, Mathf.FloorToInt(value)),
            UnitStatType.MaxHealth => Mathf.Max(0f, value),
            UnitStatType.Attack => Mathf.Max(0f, value),
            UnitStatType.Defense => Mathf.Max(0f, value),
            UnitStatType.SpecialDefense => Mathf.Max(0f, value),
            UnitStatType.MoveSpeed => Mathf.Max(0f, value),
            _ => value
        };
    }
}
