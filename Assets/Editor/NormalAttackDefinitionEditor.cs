using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NormalAttackDefinition))]
public sealed class NormalAttackDefinitionEditor : Editor
{
    private const int GridSize = 9;
    private const int GridCenter = GridSize / 2;
    private const float CellSize = 32f;

    private static readonly Color UnselectedCellColor = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color SelectedCellColor = new Color(0.25f, 0.75f, 0.38f);

    private SerializedProperty attackTypeProperty;
    private SerializedProperty targetPriorityModeProperty;
    private SerializedProperty attackPatternProperty;
    private SerializedProperty targetSideProperty;
    private SerializedProperty attackEffectProperty;
    private SerializedProperty attackMethodProperty;
    private SerializedProperty attackDamageTypeProperty;
    private SerializedProperty effectMultiplierProperty;
    private SerializedProperty projectilePrefabProperty;
    private SerializedProperty aoeHitPrefabProperty;
    private SerializedProperty hitVFXPrefabProperty;
    private SerializedProperty healVFXPrefabProperty;

    private GUIStyle unselectedCellStyle;
    private GUIStyle selectedCellStyle;

    private void OnEnable()
    {
        attackTypeProperty = serializedObject.FindProperty("attackType");
        targetPriorityModeProperty = serializedObject.FindProperty("targetPriorityMode");
        attackPatternProperty = serializedObject.FindProperty("attackPattern");
        targetSideProperty = serializedObject.FindProperty("targetSide");
        attackEffectProperty = serializedObject.FindProperty("attackEffect");
        attackMethodProperty = serializedObject.FindProperty("attackMethod");
        attackDamageTypeProperty = serializedObject.FindProperty("attackDamageType");
        effectMultiplierProperty = serializedObject.FindProperty("normalAttackEffectMultiplier");
        projectilePrefabProperty = serializedObject.FindProperty("normalAttackProjectilePrefab");
        aoeHitPrefabProperty = serializedObject.FindProperty("normalAttackAOEHitPrefab");
        hitVFXPrefabProperty = serializedObject.FindProperty("normalAttackHitVFXPrefab");
        healVFXPrefabProperty = serializedObject.FindProperty("normalAttackHealVFXPrefab");
    }

    public override void OnInspectorGUI()
    {
        EnsureStyles();
        serializedObject.Update();

        DrawAttackSection();
        EditorGUILayout.Space(8f);
        DrawDeliverySection();
        EditorGUILayout.Space(8f);
        DrawVFXSection();
        EditorGUILayout.Space(8f);
        DrawPatternSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAttackSection()
    {
        EditorGUILayout.LabelField("Attack", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(attackTypeProperty, new GUIContent("Attack Type"));
        EditorGUILayout.PropertyField(targetPriorityModeProperty, new GUIContent("Target Priority"));
        EditorGUILayout.PropertyField(targetSideProperty, new GUIContent("Target Side"));
        EditorGUILayout.PropertyField(attackEffectProperty, new GUIContent("Attack Effect"));
        EditorGUILayout.PropertyField(attackMethodProperty, new GUIContent("Attack Method"));

        if (GetAttackEffect() == AttackEffect.Damage)
        {
            EditorGUILayout.PropertyField(attackDamageTypeProperty, new GUIContent("Damage Type"));
        }

        EditorGUILayout.PropertyField(effectMultiplierProperty, new GUIContent("Effect Multiplier"));

        if (effectMultiplierProperty.floatValue < 0f)
        {
            EditorGUILayout.HelpBox("Effect Multiplier cannot be negative.", MessageType.Error);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDeliverySection()
    {
        EditorGUILayout.LabelField("Delivery", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        switch (GetAttackMethod())
        {
            case AttackMethod.Projectile:
                EditorGUILayout.PropertyField(projectilePrefabProperty, new GUIContent("Projectile Prefab"));
                if (projectilePrefabProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Projectile attack requires an AttackProjectile prefab.", MessageType.Error);
                }
                break;

            case AttackMethod.AOEHit:
                EditorGUILayout.PropertyField(aoeHitPrefabProperty, new GUIContent("AOE Hit Prefab"));
                if (aoeHitPrefabProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("AOE attack requires an AttackAOEHit prefab.", MessageType.Error);
                }
                break;

            case AttackMethod.DirectTarget:
                EditorGUILayout.LabelField("The effect is resolved directly on the selected target.", EditorStyles.miniLabel);
                break;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawVFXSection()
    {
        EditorGUILayout.LabelField("VFX", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (GetAttackEffect() == AttackEffect.Heal)
        {
            EditorGUILayout.PropertyField(healVFXPrefabProperty, new GUIContent("Heal VFX Prefab"));
        }
        else
        {
            EditorGUILayout.PropertyField(hitVFXPrefabProperty, new GUIContent("Hit VFX Prefab"));
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPatternSection()
    {
        EditorGUILayout.LabelField("Attack Pattern", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "Center is offset (0, 0). Top is positive Y. Author patterns facing left.",
            EditorStyles.miniLabel);

        HashSet<Vector2Int> selectedOffsets = GetSelectedGridOffsets();
        DrawAttackPatternGrid(selectedOffsets);
        DrawAttackPatternFooter(selectedOffsets.Count);

        EditorGUILayout.EndVertical();
    }

    private void DrawAttackPatternGrid(HashSet<Vector2Int> selectedOffsets)
    {
        const float labelWidth = 22f;

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
                GUIStyle cellStyle = isSelected ? selectedCellStyle : unselectedCellStyle;
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

        if (attackPatternProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Attack Pattern cannot be empty.", MessageType.Error);
        }

        if (outsideCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"{outsideCount} attack pattern offset(s) are outside the {GridSize}x{GridSize} editor grid and are preserved.",
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

    private AttackEffect GetAttackEffect()
    {
        return (AttackEffect)attackEffectProperty.enumValueIndex;
    }

    private AttackMethod GetAttackMethod()
    {
        return (AttackMethod)attackMethodProperty.enumValueIndex;
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
        if (unselectedCellStyle != null)
        {
            return;
        }

        unselectedCellStyle = CreateCellStyle(UnselectedCellColor, Color.white);
        selectedCellStyle = CreateCellStyle(SelectedCellColor, Color.white);
    }

    private static GUIStyle CreateCellStyle(Color backgroundColor, Color textColor)
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
            fixedWidth = CellSize,
            fixedHeight = CellSize,
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
}
