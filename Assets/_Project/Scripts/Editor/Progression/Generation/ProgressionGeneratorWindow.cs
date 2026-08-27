using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class ProgressionGeneratorWindow : EditorWindow
{
    private enum GeneratorTab
    {
        Experience,
        UnitStats
    }

    private static readonly string[] TabLabels = { "Experience", "Unit Stats" };
    private static readonly UnitStatType[] DisplayStatOrder =
    {
        UnitStatType.MaxHealth,
        UnitStatType.Attack,
        UnitStatType.Defense,
        UnitStatType.SpecialDefense,
        UnitStatType.AttackInterval,
        UnitStatType.BlockCount,
        UnitStatType.MoveSpeed
    };
    private static readonly string[] DisplayStatLabels =
    {
        "Max Health",
        "Attack",
        "Defense",
        "Special Defense",
        "Attack Interval",
        "Block Count",
        "Move Speed"
    };

    [SerializeField] private GeneratorTab selectedTab;
    [SerializeField] private Vector2 scrollPosition;

    [SerializeField] private int experienceMaxLevel = 30;
    [SerializeField] private int totalExperienceForMaxLevel = 10000;
    [SerializeField] private AnimationCurve experienceGrowthCurve;
    [SerializeField] private float experienceRandomness = 0.025f;
    [SerializeField] private DefaultAsset experienceOutputFolder;
    [SerializeField] private string experienceOutputName = "ExperienceProgressionTable";

    [SerializeField] private ScriptableObject unitDefinition;
    [SerializeField] private ExperienceProgressionTable unitLevelTable;
    [SerializeField] private AnimationCurve unitStatGrowthCurve;
    [SerializeField] private float unitStatRandomness = 0.025f;
    [SerializeField] private UnitStatFinalBreakdown finalBreakdown = new UnitStatFinalBreakdown();
    [SerializeField] private List<UnitStatLevelBreakdown> levelBreakdowns = new List<UnitStatLevelBreakdown>();
    [SerializeField] private DefaultAsset unitStatOutputFolder;
    [SerializeField] private string unitStatOutputName = "UnitStatProgressionTable";

    private string statusMessage;
    private MessageType statusMessageType = MessageType.Info;

    [MenuItem("Tools/Progression/Generator")]
    private static void OpenWindow()
    {
        GetWindow<ProgressionGeneratorWindow>("Progression Generator");
    }

    private void OnEnable()
    {
        experienceGrowthCurve ??= CreateDefaultExperienceGrowthCurve();
        unitStatGrowthCurve ??= CreateDefaultGrowthCurve();
        finalBreakdown ??= new UnitStatFinalBreakdown();
        levelBreakdowns ??= new List<UnitStatLevelBreakdown>();
        minSize = new Vector2(560f, 520f);
    }

    private void OnGUI()
    {
        selectedTab = (GeneratorTab)GUILayout.Toolbar((int)selectedTab, TabLabels);
        EditorGUILayout.Space(6f);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        switch (selectedTab)
        {
            case GeneratorTab.Experience:
                DrawExperienceGenerator();
                break;
            case GeneratorTab.UnitStats:
                DrawUnitStatGenerator();
                break;
        }

        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(statusMessage, statusMessageType);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawExperienceGenerator()
    {
        EditorGUILayout.LabelField("Experience Progression", EditorStyles.boldLabel);
        experienceMaxLevel = Mathf.Max(1, EditorGUILayout.IntField("Max Level", experienceMaxLevel));
        totalExperienceForMaxLevel = Mathf.Max(0,
            EditorGUILayout.IntField("Total EXP For Max Level", totalExperienceForMaxLevel));
        experienceGrowthCurve = EditorGUILayout.CurveField(
            "Growth Curve",
            experienceGrowthCurve,
            Color.green,
            new Rect(0f, 0f, 1f, 1f));
        if (GUILayout.Button("Reset EXP Curve To Default"))
        {
            experienceGrowthCurve = CreateDefaultExperienceGrowthCurve();
        }
        experienceRandomness = EditorGUILayout.Slider(
            "Randomness",
            experienceRandomness,
            0f,
            ProgressionGenerationLimits.MaxRandomness);

        EditorGUILayout.Space(8f);
        DrawOutputFields(ref experienceOutputFolder, ref experienceOutputName);

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Generate Experience Table", GUILayout.Height(28f)))
        {
            GenerateExperienceTable();
        }
    }

    private void DrawUnitStatGenerator()
    {
        EditorGUILayout.LabelField("Unit Stat Progression", EditorStyles.boldLabel);

        ScriptableObject newDefinition = EditorGUILayout.ObjectField(
            "Unit Definition",
            unitDefinition,
            typeof(ScriptableObject),
            false) as ScriptableObject;

        if (newDefinition != unitDefinition)
        {
            unitDefinition = newDefinition;
            OnUnitDefinitionChanged();
        }

        unitLevelTable = (ExperienceProgressionTable)EditorGUILayout.ObjectField(
            "EXP Level Table",
            unitLevelTable,
            typeof(ExperienceProgressionTable),
            false);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Max Level", unitLevelTable != null ? unitLevelTable.MaxLevel : 0);
        }

        unitStatGrowthCurve = EditorGUILayout.CurveField(
            "Growth Curve",
            unitStatGrowthCurve,
            Color.green,
            new Rect(0f, 0f, 1f, 1f));
        unitStatRandomness = EditorGUILayout.Slider(
            "Randomness",
            unitStatRandomness,
            0f,
            ProgressionGenerationLimits.MaxRandomness);

        UnitBaseStats baseStats = null;
        if (unitDefinition != null
            && !UnitStatProgressionGenerator.TryGetBaseStats(unitDefinition, out baseStats, out string definitionError))
        {
            EditorGUILayout.HelpBox(definitionError, MessageType.Error);
        }

        EditorGUILayout.Space(8f);
        DrawLevelBreakdowns(baseStats);
        EditorGUILayout.Space(8f);
        DrawFinalBreakdown(baseStats);
        EditorGUILayout.Space(8f);
        DrawOutputFields(ref unitStatOutputFolder, ref unitStatOutputName);

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Generate Unit Stat Table", GUILayout.Height(28f)))
        {
            GenerateUnitStatTable();
        }
    }

    private void DrawLevelBreakdowns(UnitBaseStats baseStats)
    {
        EditorGUILayout.LabelField("Level Breakdowns", EditorStyles.boldLabel);

        for (int breakdownIndex = 0; breakdownIndex < levelBreakdowns.Count; breakdownIndex++)
        {
            UnitStatLevelBreakdown breakdown = levelBreakdowns[breakdownIndex];
            if (breakdown == null)
            {
                breakdown = new UnitStatLevelBreakdown();
                levelBreakdowns[breakdownIndex] = breakdown;
            }

            if (breakdown.targets == null)
            {
                breakdown.targets = new List<UnitStatBreakdownTarget>();
            }

            bool removeBreakdown = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            breakdown.level = EditorGUILayout.IntField("Level", breakdown.level);
            GUIContent removeContent = EditorGUIUtility.IconContent("Toolbar Minus");
            removeContent.tooltip = "Remove breakdown";
            if (GUILayout.Button(removeContent, GUILayout.Width(26f), GUILayout.Height(20f)))
            {
                removeBreakdown = true;
            }
            EditorGUILayout.EndHorizontal();

            for (int targetIndex = 0; targetIndex < breakdown.targets.Count; targetIndex++)
            {
                UnitStatBreakdownTarget target = breakdown.targets[targetIndex];
                if (target == null)
                {
                    target = new UnitStatBreakdownTarget();
                    breakdown.targets[targetIndex] = target;
                }

                bool removeTarget = DrawBreakdownTarget(target, baseStats);
                if (removeTarget)
                {
                    breakdown.targets.RemoveAt(targetIndex);
                    targetIndex--;
                }
            }

            using (new EditorGUI.DisabledScope(breakdown.targets.Count >= Enum.GetValues(typeof(UnitStatType)).Length))
            {
                if (GUILayout.Button("Add Stat"))
                {
                    AddMissingStatTarget(breakdown, baseStats);
                }
            }

            EditorGUILayout.EndVertical();

            if (removeBreakdown)
            {
                levelBreakdowns.RemoveAt(breakdownIndex);
                breakdownIndex--;
            }
        }

        if (GUILayout.Button("Add Level Breakdown"))
        {
            int suggestedLevel = levelBreakdowns.Count == 0 ? 10 : levelBreakdowns[levelBreakdowns.Count - 1].level + 5;
            levelBreakdowns.Add(new UnitStatLevelBreakdown { level = suggestedLevel });
        }
    }

    private static bool DrawBreakdownTarget(UnitStatBreakdownTarget target, UnitBaseStats baseStats)
    {
        EditorGUILayout.BeginHorizontal();
        UnitStatType previousType = target.statType;
        int selectedIndex = Array.IndexOf(DisplayStatOrder, target.statType);
        selectedIndex = EditorGUILayout.Popup(Mathf.Max(0, selectedIndex), DisplayStatLabels, GUILayout.Width(135f));
        target.statType = DisplayStatOrder[selectedIndex];

        if (target.statType != previousType && target.statType == UnitStatType.BlockCount && baseStats != null)
        {
            target.directValue = baseStats.BlockCount;
        }

        if (target.statType == UnitStatType.BlockCount)
        {
            target.directValue = Mathf.Max(0, EditorGUILayout.IntField(target.directValue, GUILayout.Width(80f)));
            GUILayout.Label($"= {target.directValue}", GUILayout.Width(90f));
        }
        else
        {
            target.multiplier = Mathf.Max(0f, EditorGUILayout.FloatField(target.multiplier, GUILayout.Width(80f)));
            string result = baseStats != null
                ? FormatGeneratedValue(target.statType,
                    UnitStatGenerationRules.GetBaseValue(baseStats, target.statType) * target.multiplier)
                : "-";
            GUILayout.Label($"= {result}", GUILayout.Width(90f));
        }

        GUIContent removeContent = EditorGUIUtility.IconContent("Toolbar Minus");
        removeContent.tooltip = "Remove stat";
        bool remove = GUILayout.Button(removeContent, GUILayout.Width(26f), GUILayout.Height(20f));
        EditorGUILayout.EndHorizontal();
        return remove;
    }

    private void DrawFinalBreakdown(UnitBaseStats baseStats)
    {
        EditorGUILayout.LabelField("Final Breakdown", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawFinalMultiplier("Max Health", UnitStatType.MaxHealth, ref finalBreakdown.maxHealthMultiplier, baseStats);
        DrawFinalMultiplier("Attack", UnitStatType.Attack, ref finalBreakdown.attackMultiplier, baseStats);
        DrawFinalMultiplier("Defense", UnitStatType.Defense, ref finalBreakdown.defenseMultiplier, baseStats);
        DrawFinalMultiplier("Special Defense", UnitStatType.SpecialDefense,
            ref finalBreakdown.specialDefenseMultiplier, baseStats);
        DrawFinalMultiplier("Attack Interval", UnitStatType.AttackInterval,
            ref finalBreakdown.attackIntervalMultiplier, baseStats);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Block Count", GUILayout.Width(130f));
        finalBreakdown.blockCount = Mathf.Max(0,
            EditorGUILayout.IntField(finalBreakdown.blockCount, GUILayout.Width(80f)));
        GUILayout.Label($"= {finalBreakdown.blockCount}");
        EditorGUILayout.EndHorizontal();

        DrawFinalMultiplier("Move Speed", UnitStatType.MoveSpeed, ref finalBreakdown.moveSpeedMultiplier, baseStats);

        EditorGUILayout.EndVertical();
    }

    private static void DrawFinalMultiplier(
        string label,
        UnitStatType statType,
        ref float multiplier,
        UnitBaseStats baseStats)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(130f));
        multiplier = Mathf.Max(0f, EditorGUILayout.FloatField(multiplier, GUILayout.Width(80f)));
        string result = baseStats != null
            ? FormatGeneratedValue(statType,
                UnitStatGenerationRules.GetBaseValue(baseStats, statType) * multiplier)
            : "-";
        GUILayout.Label($"= {result}");
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawOutputFields(ref DefaultAsset outputFolder, ref string outputName)
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Folder",
            outputFolder,
            typeof(DefaultAsset),
            false);
        outputName = EditorGUILayout.TextField("Output Name", outputName);
    }

    private void GenerateExperienceTable()
    {
        if (!ExperienceProgressionGenerator.TryGenerate(
                experienceMaxLevel,
                totalExperienceForMaxLevel,
                experienceGrowthCurve,
                experienceRandomness,
                out int[] thresholds,
                out string error))
        {
            SetStatus(error, true);
            return;
        }

        if (!ProgressionAssetWriter.TryWriteExperienceTable(
                experienceOutputFolder,
                experienceOutputName,
                thresholds,
                out ExperienceProgressionTable table,
                out error))
        {
            SetStatus(error, true);
            return;
        }

        SetStatus($"Generated {table.MaxLevel} levels in {table.name}.", false);
    }

    private void GenerateUnitStatTable()
    {
        if (!UnitStatProgressionGenerator.TryGetBaseStats(unitDefinition, out UnitBaseStats baseStats, out string error))
        {
            SetStatus(error, true);
            return;
        }

        if (unitLevelTable == null)
        {
            SetStatus("An EXP Level Table is required.", true);
            return;
        }

        if (!UnitStatProgressionGenerator.TryGenerate(
                baseStats,
                unitLevelTable.MaxLevel,
                unitStatGrowthCurve,
                finalBreakdown,
                levelBreakdowns,
                unitStatRandomness,
                out UnitBaseStats[] statsByLevel,
                out error))
        {
            SetStatus(error, true);
            return;
        }

        if (!ProgressionAssetWriter.TryWriteUnitStatTable(
                unitStatOutputFolder,
                unitStatOutputName,
                statsByLevel,
                unitDefinition,
                out UnitStatProgressionTable table,
                out error))
        {
            SetStatus(error, true);
            return;
        }

        SetStatus($"Generated {table.MaxLevel} stat levels in {table.name} and assigned it to {unitDefinition.name}.", false);
    }

    private void OnUnitDefinitionChanged()
    {
        if (unitDefinition == null)
        {
            return;
        }

        if (UnitStatProgressionGenerator.TryGetBaseStats(unitDefinition, out UnitBaseStats baseStats, out _))
        {
            finalBreakdown.blockCount = baseStats.BlockCount;
            unitStatOutputName = $"{unitDefinition.name}StatProgressionTable";
        }
    }

    private static void AddMissingStatTarget(UnitStatLevelBreakdown breakdown, UnitBaseStats baseStats)
    {
        for (int valueIndex = 0; valueIndex < DisplayStatOrder.Length; valueIndex++)
        {
            UnitStatType statType = DisplayStatOrder[valueIndex];
            bool alreadyExists = false;

            for (int targetIndex = 0; targetIndex < breakdown.targets.Count; targetIndex++)
            {
                if (breakdown.targets[targetIndex].statType == statType)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
            {
                continue;
            }

            breakdown.targets.Add(new UnitStatBreakdownTarget
            {
                statType = statType,
                multiplier = 1f,
                directValue = statType == UnitStatType.BlockCount && baseStats != null ? baseStats.BlockCount : 0
            });
            return;
        }
    }

    private static string FormatGeneratedValue(UnitStatType statType, float value)
    {
        value = UnitStatGenerationRules.NormalizeGeneratedValue(statType, value);
        return UnitStatGenerationRules.IsRoundedStat(statType) ? value.ToString("0") : value.ToString("0.###");
    }

    private void SetStatus(string message, bool isError)
    {
        statusMessage = message;
        statusMessageType = isError ? MessageType.Error : MessageType.Info;
        Repaint();
    }

    private static AnimationCurve CreateDefaultGrowthCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.75f, 0.65f),
            new Keyframe(1f, 1f));

        for (int keyIndex = 0; keyIndex < curve.length - 1; keyIndex++)
        {
            AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex + 1, AnimationUtility.TangentMode.Linear);
        }

        return curve;
    }

    private static AnimationCurve CreateDefaultExperienceGrowthCurve()
    {
        const float startingSlope = 0.2f;
        const float endingSlope = 1.8f;

        return new AnimationCurve(
            new Keyframe(0f, 0f, startingSlope, startingSlope),
            new Keyframe(1f, 1f, endingSlope, endingSlope));
    }
}
