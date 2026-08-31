using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class TooltipHelper
{
    public static string GetSkillTooltipText(SkillDefinition skill)
    {
        if (skill == null)
        {
            return string.Empty;
        }

        string skillDetail = skill.SkillType == SkillType.Passive ? "Passive" : $"Cooldown: {FormatValue(skill.Cooldown)}s";

        string header = $"{skill.SkillName}. {skillDetail}";
        if (string.IsNullOrWhiteSpace(skill.SkillDescription))
        {
            return header;
        }

        return $"{header}\n{skill.SkillDescription.Trim()}";
    }

    public static string GetGearTooltipText(GearInstance gear)
    {
        if (gear == null || !gear.IsValid)
        {
            return string.Empty;
        }

        GearDefinition definition = gear.Definition;
        List<string> sections = new List<string>
        {
            $"{definition.GearName}\n{definition.GearRarity} \u2022 {definition.GearType}"
        };

        string statsText = GetGearStatsText(definition.StatModifiers);
        if (!string.IsNullOrEmpty(statsText))
        {
            sections.Add(statsText);
        }

        if (!string.IsNullOrWhiteSpace(definition.PassiveDescription))
        {
            sections.Add(definition.PassiveDescription.Trim());
        }

        HeroInstance equippedHero = gear.EquippedHero;
        if (equippedHero != null && equippedHero.IsValid)
        {
            sections.Add($"Equipped: {equippedHero.Definition.HeroName}");
        }

        return string.Join("\n\n", sections);
    }

    private static string GetGearStatsText(IReadOnlyList<UnitStatModifier> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < modifiers.Count; i++)
        {
            UnitStatModifier modifier = modifiers[i];
            if (!modifier.IsValid)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(GetStatModifierText(modifier));
        }

        return builder.ToString();
    }

    private static string GetStatModifierText(UnitStatModifier modifier)
    {
        string statName = GetStatName(modifier.StatType);

        switch (modifier.ModifierType)
        {
            case UnitStatModifierType.FlatBase:
                return $"{GetSignedValue(modifier.Value)} Base {statName}";
            case UnitStatModifierType.FlatFinal:
                return $"{GetSignedValue(modifier.Value)} {statName}";
            case UnitStatModifierType.AdditivePercent:
                return $"{GetSignedValue(modifier.Value * 100f)}% {statName}";
            case UnitStatModifierType.FinalMultiplier:
                return $"x{FormatValue(modifier.Value)} {statName}";
            default:
                return string.Empty;
        }
    }

    private static string GetStatName(UnitStatType statType)
    {
        switch (statType)
        {
            case UnitStatType.MaxHealth:
                return "Max Health";
            case UnitStatType.Attack:
                return "Attack";
            case UnitStatType.AttackInterval:
                return "Attack Interval";
            case UnitStatType.Defense:
                return "Defense";
            case UnitStatType.SpecialDefense:
                return "Special Defense";
            case UnitStatType.MoveSpeed:
                return "Move Speed";
            case UnitStatType.BlockCount:
                return "Block Count";
            default:
                return statType.ToString();
        }
    }

    private static string GetSignedValue(float value)
    {
        string sign = value >= 0f ? "+" : "-";
        return sign + FormatValue(System.Math.Abs(value));
    }

    private static string FormatValue(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
