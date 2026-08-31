using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillDefinition))]
public class SkillDefinitionEditor : Editor
{
    private const int PatternGridSize = 9;
    private const int PatternGridCenter = PatternGridSize / 2;
    private const float PatternCellSize = 32f;

    private static readonly Color UnselectedPatternCellColor = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color SelectedPatternCellColor = new Color(0.25f, 0.75f, 0.38f);

    private readonly List<Type> skillTypes = new List<Type>();

    private SerializedProperty skillIdProperty;
    private SerializedProperty skillNameProperty;
    private SerializedProperty skillIconProperty;
    private SerializedProperty skillDescriptionProperty;
    private SerializedProperty skillTypeProperty;
    private SerializedProperty targetTypeProperty;
    private SerializedProperty cooldownProperty;
    private SerializedProperty skillProperty;

    private string[] skillTypeNames;
    private GUIStyle unselectedPatternCellStyle;
    private GUIStyle selectedPatternCellStyle;

    private void OnEnable()
    {
        skillIdProperty = serializedObject.FindProperty("skillId");
        skillNameProperty = serializedObject.FindProperty("skillName");
        skillIconProperty = serializedObject.FindProperty("skillIcon");
        skillDescriptionProperty = serializedObject.FindProperty("skillDescription");
        skillTypeProperty = serializedObject.FindProperty("skillType");
        targetTypeProperty = serializedObject.FindProperty("targetType");
        cooldownProperty = serializedObject.FindProperty("cooldown");
        skillProperty = serializedObject.FindProperty("skill");

        CacheSkillTypes();
    }

    public override void OnInspectorGUI()
    {
        EnsurePatternStyles();
        serializedObject.Update();

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(skillIdProperty);
        EditorGUILayout.PropertyField(skillNameProperty);
        EditorGUILayout.PropertyField(skillIconProperty);
        EditorGUILayout.PropertyField(skillDescriptionProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Skill", EditorStyles.boldLabel);

        bool hasSkill = skillProperty.managedReferenceValue != null;
        using (new EditorGUI.DisabledScope(hasSkill))
        {
            EditorGUILayout.PropertyField(skillTypeProperty);
        }

        EditorGUILayout.PropertyField(targetTypeProperty);
        EditorGUILayout.PropertyField(cooldownProperty);

        DrawSkillPicker();

        if (skillProperty.managedReferenceValue != null)
        {
            EditorGUILayout.Space();
            DrawSkillSettings();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSkillSettings()
    {
        EditorGUILayout.PropertyField(skillProperty, new GUIContent("Skill Settings"), false);
        if (!skillProperty.isExpanded)
        {
            return;
        }

        SerializedProperty areaPatternProperty = skillProperty.FindPropertyRelative("areaPattern");

        EditorGUI.indentLevel++;
        DrawSkillPropertiesExcept(areaPatternProperty);
        EditorGUI.indentLevel--;

        if (areaPatternProperty != null)
        {
            EditorGUILayout.Space(8f);
            DrawPatternSection(areaPatternProperty);
        }
    }

    private void DrawSkillPropertiesExcept(SerializedProperty excludedProperty)
    {
        SerializedProperty property = skillProperty.Copy();
        SerializedProperty endProperty = property.GetEndProperty();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren) && !SerializedProperty.EqualContents(property, endProperty))
        {
            enterChildren = false;

            if (property.depth != skillProperty.depth + 1)
            {
                continue;
            }

            if (excludedProperty != null && property.propertyPath == excludedProperty.propertyPath)
            {
                continue;
            }

            EditorGUILayout.PropertyField(property, true);
        }
    }

    private void DrawPatternSection(SerializedProperty patternProperty)
    {
        EditorGUILayout.LabelField("Area Pattern", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "Center is offset (0, 0). Top is positive Y. Author patterns facing left.",
            EditorStyles.miniLabel);

        HashSet<Vector2Int> selectedOffsets = GetSelectedGridOffsets(patternProperty);
        DrawPatternGrid(patternProperty, selectedOffsets);
        DrawPatternFooter(patternProperty, selectedOffsets.Count);

        EditorGUILayout.EndVertical();
    }

    private void DrawPatternGrid(SerializedProperty patternProperty, HashSet<Vector2Int> selectedOffsets)
    {
        const float labelWidth = 22f;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(labelWidth);

        for (int column = 0; column < PatternGridSize; column++)
        {
            int x = column - PatternGridCenter;
            GUILayout.Label(x.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(PatternCellSize));
        }

        EditorGUILayout.EndHorizontal();

        for (int row = 0; row < PatternGridSize; row++)
        {
            int y = PatternGridCenter - row;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(y.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(labelWidth), GUILayout.Height(PatternCellSize));

            for (int column = 0; column < PatternGridSize; column++)
            {
                int x = column - PatternGridCenter;
                Vector2Int offset = new Vector2Int(x, y);
                bool isSelected = selectedOffsets.Contains(offset);
                GUIStyle cellStyle = isSelected ? selectedPatternCellStyle : unselectedPatternCellStyle;
                string label = offset == Vector2Int.zero ? "C" : string.Empty;

                if (GUILayout.Button(label, cellStyle, GUILayout.Width(PatternCellSize), GUILayout.Height(PatternCellSize)))
                {
                    TogglePatternOffset(patternProperty, offset);
                    selectedOffsets = GetSelectedGridOffsets(patternProperty);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawPatternFooter(SerializedProperty patternProperty, int selectedGridOffsetCount)
    {
        int outsideCount = CountOutsideGridOffsets(patternProperty);
        int duplicateCount = CountDuplicateOffsets(patternProperty);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(
            $"Selected cells: {selectedGridOffsetCount} / {PatternGridSize * PatternGridSize}",
            EditorStyles.miniLabel);

        if (patternProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Area Pattern cannot be empty.", MessageType.Error);
        }

        if (outsideCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"{outsideCount} area pattern offset(s) are outside the {PatternGridSize}x{PatternGridSize} editor grid and are preserved.",
                MessageType.Warning);
        }

        if (duplicateCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"{duplicateCount} duplicate area pattern offset(s) were found.",
                MessageType.Warning);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear Grid"))
        {
            ClearGridOffsets(patternProperty);
        }

        if (GUILayout.Button("Center Only"))
        {
            SetGridOffsets(patternProperty, new[] { Vector2Int.zero });
        }

        if (GUILayout.Button("Clean Duplicates"))
        {
            RemoveDuplicateOffsets(patternProperty);
        }

        EditorGUILayout.EndHorizontal();
    }

    private static HashSet<Vector2Int> GetSelectedGridOffsets(SerializedProperty patternProperty)
    {
        HashSet<Vector2Int> offsets = new HashSet<Vector2Int>();

        for (int index = 0; index < patternProperty.arraySize; index++)
        {
            Vector2Int offset = patternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (IsInsidePatternGrid(offset))
            {
                offsets.Add(offset);
            }
        }

        return offsets;
    }

    private static void TogglePatternOffset(SerializedProperty patternProperty, Vector2Int offset)
    {
        bool removedOffset = false;

        for (int index = patternProperty.arraySize - 1; index >= 0; index--)
        {
            SerializedProperty element = patternProperty.GetArrayElementAtIndex(index);
            if (element.vector2IntValue != offset)
            {
                continue;
            }

            patternProperty.DeleteArrayElementAtIndex(index);
            removedOffset = true;
        }

        if (removedOffset)
        {
            return;
        }

        int newIndex = patternProperty.arraySize;
        patternProperty.InsertArrayElementAtIndex(newIndex);
        patternProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = offset;
    }

    private static void ClearGridOffsets(SerializedProperty patternProperty)
    {
        for (int index = patternProperty.arraySize - 1; index >= 0; index--)
        {
            Vector2Int offset = patternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (IsInsidePatternGrid(offset))
            {
                patternProperty.DeleteArrayElementAtIndex(index);
            }
        }
    }

    private static void SetGridOffsets(SerializedProperty patternProperty, IEnumerable<Vector2Int> offsets)
    {
        ClearGridOffsets(patternProperty);

        foreach (Vector2Int offset in offsets)
        {
            int newIndex = patternProperty.arraySize;
            patternProperty.InsertArrayElementAtIndex(newIndex);
            patternProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = offset;
        }
    }

    private static void RemoveDuplicateOffsets(SerializedProperty patternProperty)
    {
        HashSet<Vector2Int> seenOffsets = new HashSet<Vector2Int>();

        for (int index = patternProperty.arraySize - 1; index >= 0; index--)
        {
            Vector2Int offset = patternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (!seenOffsets.Add(offset))
            {
                patternProperty.DeleteArrayElementAtIndex(index);
            }
        }
    }

    private static int CountOutsideGridOffsets(SerializedProperty patternProperty)
    {
        int count = 0;

        for (int index = 0; index < patternProperty.arraySize; index++)
        {
            Vector2Int offset = patternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (!IsInsidePatternGrid(offset))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountDuplicateOffsets(SerializedProperty patternProperty)
    {
        int count = 0;
        HashSet<Vector2Int> seenOffsets = new HashSet<Vector2Int>();

        for (int index = 0; index < patternProperty.arraySize; index++)
        {
            Vector2Int offset = patternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (!seenOffsets.Add(offset))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsInsidePatternGrid(Vector2Int offset)
    {
        return offset.x >= -PatternGridCenter &&
               offset.x <= PatternGridCenter &&
               offset.y >= -PatternGridCenter &&
               offset.y <= PatternGridCenter;
    }

    private void EnsurePatternStyles()
    {
        if (unselectedPatternCellStyle != null)
        {
            return;
        }

        unselectedPatternCellStyle = CreatePatternCellStyle(UnselectedPatternCellColor, Color.white);
        selectedPatternCellStyle = CreatePatternCellStyle(SelectedPatternCellColor, Color.white);
    }

    private static GUIStyle CreatePatternCellStyle(Color backgroundColor, Color textColor)
    {
        Texture2D background = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        background.SetPixel(0, 0, backgroundColor);
        background.Apply();

        GUIStyle style = new GUIStyle(GUIStyle.none)
        {
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = PatternCellSize,
            fixedHeight = PatternCellSize,
            margin = new RectOffset(1, 1, 1, 1),
            padding = new RectOffset(0, 0, 0, 0)
        };

        style.normal.background = background;
        style.hover.background = background;
        style.active.background = background;
        style.focused.background = background;
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.active.textColor = textColor;
        style.focused.textColor = textColor;

        return style;
    }

    private void DrawSkillPicker()
    {
        int currentIndex = GetCurrentSkillIndex();
        int selectedIndex = EditorGUILayout.Popup("Skill Runtime", currentIndex, skillTypeNames);

        if (selectedIndex == currentIndex)
        {
            return;
        }

        if (selectedIndex == 0)
        {
            skillProperty.managedReferenceValue = null;
            return;
        }

        Type selectedType = skillTypes[selectedIndex - 1];
        skillProperty.managedReferenceValue = Activator.CreateInstance(selectedType);
        skillTypeProperty.enumValueIndex = typeof(AutoActiveSkill).IsAssignableFrom(selectedType)
            ? (int)SkillType.Active
            : (int)SkillType.Passive;
    }

    private int GetCurrentSkillIndex()
    {
        object currentSkill = skillProperty.managedReferenceValue;
        if (currentSkill == null)
        {
            return 0;
        }

        Type currentType = currentSkill.GetType();
        for (int i = 0; i < skillTypes.Count; i++)
        {
            if (skillTypes[i] == currentType)
            {
                return i + 1;
            }
        }

        return 0;
    }

    private void CacheSkillTypes()
    {
        skillTypes.Clear();

        foreach (Type skillType in TypeCache.GetTypesDerivedFrom<BaseSkill>())
        {
            if (!skillType.IsAbstract && !skillType.IsGenericType)
            {
                skillTypes.Add(skillType);
            }
        }

        skillTypes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));

        skillTypeNames = new string[skillTypes.Count + 1];
        skillTypeNames[0] = "None";

        for (int i = 0; i < skillTypes.Count; i++)
        {
            string typeName = skillTypes[i].Name;
            if (typeName.EndsWith("Skill", StringComparison.Ordinal))
            {
                typeName = typeName.Substring(0, typeName.Length - "Skill".Length);
            }

            skillTypeNames[i + 1] = ObjectNames.NicifyVariableName(typeName);
        }
    }
}
