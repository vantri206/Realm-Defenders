using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeroDefinition))]
public sealed class HeroDefinitionEditor : Editor
{
    private const int GridSize = 7;
    private const int GridCenter = GridSize / 2;
    private const float CellSize = 32f;

    private static readonly Color SelectedCellColor = new Color(0.25f, 0.75f, 0.38f);

    private SerializedProperty heroIdProperty;
    private SerializedProperty heroNameProperty;
    private SerializedProperty iconProperty;
    private SerializedProperty prefabProperty;
    private SerializedProperty maxHealthProperty;
    private SerializedProperty attackProperty;
    private SerializedProperty attackIntervalProperty;
    private SerializedProperty targetPriorityModeProperty;
    private SerializedProperty attackPatternProperty;

    private GUIStyle centeredCellStyle;
    private GUIStyle selectedCellStyle;

    private void OnEnable()
    {
        heroIdProperty = serializedObject.FindProperty("heroId");
        heroNameProperty = serializedObject.FindProperty("heroName");
        iconProperty = serializedObject.FindProperty("icon");
        prefabProperty = serializedObject.FindProperty("prefab");
        maxHealthProperty = serializedObject.FindProperty("maxHealth");
        attackProperty = serializedObject.FindProperty("attack");
        attackIntervalProperty = serializedObject.FindProperty("attackInterval");
        targetPriorityModeProperty = serializedObject.FindProperty("targetPriorityMode");
        attackPatternProperty = serializedObject.FindProperty("attackPattern");
    }

    public override void OnInspectorGUI()
    {
        EnsureStyles();
        serializedObject.Update();

        DrawIdentitySection();
        EditorGUILayout.Space(8f);
        DrawStatsSection();
        EditorGUILayout.Space(8f);
        DrawAttackSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentitySection()
    {
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(heroIdProperty, new GUIContent("Hero ID"));
        EditorGUILayout.PropertyField(heroNameProperty, new GUIContent("Hero Name"));
        EditorGUILayout.PropertyField(iconProperty);
        EditorGUILayout.PropertyField(prefabProperty);
        EditorGUILayout.EndVertical();
    }

    private void DrawStatsSection()
    {
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(maxHealthProperty, new GUIContent("Max Health"));
        EditorGUILayout.PropertyField(attackProperty);
        EditorGUILayout.PropertyField(attackIntervalProperty, new GUIContent("Attack Interval"));
        EditorGUILayout.EndVertical();
    }

    private void DrawAttackSection()
    {
        EditorGUILayout.LabelField("Attack", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(targetPriorityModeProperty, new GUIContent("Target Priority"));
        EditorGUILayout.Space(5f);

        EditorGUILayout.LabelField("Attack Pattern", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(
            "Center cell is offset (0, 0). Top row is positive Y. Default direction is left.",
            EditorStyles.miniLabel);

        HashSet<Vector2Int> selectedOffsets = GetSelectedGridOffsets();
        DrawAttackPatternGrid(selectedOffsets);
        DrawAttackPatternFooter(selectedOffsets.Count);

        EditorGUILayout.EndVertical();
    }

    private void DrawAttackPatternGrid(HashSet<Vector2Int> selectedOffsets)
    {
        float labelWidth = 22f;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(labelWidth);

        for (int column = 0; column < GridSize; column++)
        {
            int x = column - GridCenter;
            GUILayout.Label(x.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(CellSize));
        }

        EditorGUILayout.EndHorizontal();

        for (int row = 0; row < GridSize; row++)
        {
            int y = GridCenter - row;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(y.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(labelWidth), GUILayout.Height(CellSize));

            for (int column = 0; column < GridSize; column++)
            {
                int x = column - GridCenter;
                Vector2Int offset = new Vector2Int(x, y);
                bool isSelected = selectedOffsets.Contains(offset);
                GUIStyle cellStyle = isSelected ? selectedCellStyle : centeredCellStyle;

                string label = offset == Vector2Int.zero ? "C" : string.Empty;
                if (GUILayout.Button(label, cellStyle, GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                {
                    TogglePatternOffset(offset);
                    selectedOffsets = GetSelectedGridOffsets();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawAttackPatternFooter(int selectedGridOffsetCount)
    {
        int outsideCount = CountOutsideGridOffsets();
        int duplicateCount = CountDuplicateOffsets();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(
            $"Selected cells: {selectedGridOffsetCount} / {GridSize * GridSize}",
            EditorStyles.miniLabel);

        if (outsideCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"{outsideCount} attack pattern offset(s) are outside the 7x7 editor grid and are preserved.",
                MessageType.Warning);
        }

        if (duplicateCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"{duplicateCount} duplicate attack pattern offset(s) were found.",
                MessageType.Warning);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Grid"))
        {
            ClearGridOffsets();
        }

        if (GUILayout.Button("Center Only"))
        {
            SetGridOffsets(new[] { Vector2Int.zero });
        }

        if (GUILayout.Button("Clean Duplicates"))
        {
            RemoveDuplicateOffsets();
        }

        EditorGUILayout.EndHorizontal();
    }

    private HashSet<Vector2Int> GetSelectedGridOffsets()
    {
        HashSet<Vector2Int> offsets = new HashSet<Vector2Int>();

        for (int index = 0; index < attackPatternProperty.arraySize; index++)
        {
            Vector2Int offset = attackPatternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (IsInsideGrid(offset))
            {
                offsets.Add(offset);
            }
        }

        return offsets;
    }

    private void TogglePatternOffset(Vector2Int offset)
    {
        bool removedOffset = false;

        for (int index = attackPatternProperty.arraySize - 1; index >= 0; index--)
        {
            SerializedProperty element = attackPatternProperty.GetArrayElementAtIndex(index);
            if (element.vector2IntValue != offset)
            {
                continue;
            }

            attackPatternProperty.DeleteArrayElementAtIndex(index);
            removedOffset = true;
        }

        if (removedOffset)
        {
            return;
        }

        int newIndex = attackPatternProperty.arraySize;
        attackPatternProperty.InsertArrayElementAtIndex(newIndex);
        attackPatternProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = offset;
    }

    private void ClearGridOffsets()
    {
        for (int index = attackPatternProperty.arraySize - 1; index >= 0; index--)
        {
            Vector2Int offset = attackPatternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (IsInsideGrid(offset))
            {
                attackPatternProperty.DeleteArrayElementAtIndex(index);
            }
        }
    }

    private void SetGridOffsets(IEnumerable<Vector2Int> offsets)
    {
        ClearGridOffsets();

        foreach (Vector2Int offset in offsets)
        {
            int newIndex = attackPatternProperty.arraySize;
            attackPatternProperty.InsertArrayElementAtIndex(newIndex);
            attackPatternProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = offset;
        }
    }

    private void RemoveDuplicateOffsets()
    {
        HashSet<Vector2Int> seenOffsets = new HashSet<Vector2Int>();

        for (int index = attackPatternProperty.arraySize - 1; index >= 0; index--)
        {
            Vector2Int offset = attackPatternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (!seenOffsets.Add(offset))
            {
                attackPatternProperty.DeleteArrayElementAtIndex(index);
            }
        }
    }

    private int CountOutsideGridOffsets()
    {
        int count = 0;

        for (int index = 0; index < attackPatternProperty.arraySize; index++)
        {
            Vector2Int offset = attackPatternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (!IsInsideGrid(offset))
            {
                count++;
            }
        }

        return count;
    }

    private int CountDuplicateOffsets()
    {
        int count = 0;
        HashSet<Vector2Int> seenOffsets = new HashSet<Vector2Int>();

        for (int index = 0; index < attackPatternProperty.arraySize; index++)
        {
            Vector2Int offset = attackPatternProperty.GetArrayElementAtIndex(index).vector2IntValue;
            if (!seenOffsets.Add(offset))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsInsideGrid(Vector2Int offset)
    {
        return offset.x >= -GridCenter &&
               offset.x <= GridCenter &&
               offset.y >= -GridCenter &&
               offset.y <= GridCenter;
    }

    private void EnsureStyles()
    {
        if (centeredCellStyle != null)
        {
            return;
        }

        centeredCellStyle = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = CellSize,
            fixedHeight = CellSize
        };

        selectedCellStyle = CreateColoredCellStyle(SelectedCellColor, Color.white);
    }

    private static GUIStyle CreateColoredCellStyle(Color backgroundColor, Color textColor)
    {
        Texture2D background = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        background.SetPixel(0, 0, backgroundColor);
        background.Apply();

        GUIStyle style = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = CellSize,
            fixedHeight = CellSize
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
}
