using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnitStatProgressionTable))]
public sealed class UnitStatProgressionTableEditor : Editor
{
    private const float LevelColumnWidth = 70f;
    private const float DefaultColumnWidth = 90f;
    private const float AttackIntervalColumnWidth = 110f;
    private const float SpecialDefenseColumnWidth = 115f;
    private const float RowHeight = 22f;
    private const float MaxTableHeight = 500f;

    private SerializedProperty statsByLevelProperty;
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        statsByLevelProperty = serializedObject.FindProperty("statsByLevel");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        int rowCount = statsByLevelProperty?.arraySize ?? 0;
        EditorGUILayout.LabelField("Max Level", rowCount.ToString());
        EditorGUILayout.Space(4f);

        if (rowCount == 0)
        {
            EditorGUILayout.HelpBox("No generated stat data.", MessageType.Info);
            return;
        }

        float tableHeight = Mathf.Min(MaxTableHeight, (rowCount + 1) * RowHeight + 8f);
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            true,
            true,
            GUILayout.Height(tableHeight));

        EditorGUILayout.BeginVertical(GUILayout.Width(GetTableWidth()));
        DrawTableHeader();

        for (int index = 0; index < rowCount; index++)
        {
            DrawRow(index);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private static void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(RowHeight));
        DrawHeaderCell("Level", LevelColumnWidth);
        DrawHeaderCell("Max Health", DefaultColumnWidth);
        DrawHeaderCell("Attack", DefaultColumnWidth);
        DrawHeaderCell("Defense", DefaultColumnWidth);
        DrawHeaderCell("Special Defense", SpecialDefenseColumnWidth);
        DrawHeaderCell("Attack Interval", AttackIntervalColumnWidth);
        DrawHeaderCell("Block Count", DefaultColumnWidth);
        DrawHeaderCell("Move Speed", DefaultColumnWidth);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRow(int index)
    {
        SerializedProperty statsProperty = statsByLevelProperty.GetArrayElementAtIndex(index);

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(RowHeight));
        DrawCell($"LV.{index + 1}", LevelColumnWidth);
        DrawFloatCell(statsProperty, "maxHealth", DefaultColumnWidth);
        DrawFloatCell(statsProperty, "attack", DefaultColumnWidth);
        DrawFloatCell(statsProperty, "defense", DefaultColumnWidth);
        DrawFloatCell(statsProperty, "specialDefense", SpecialDefenseColumnWidth);
        DrawFloatCell(statsProperty, "attackInterval", AttackIntervalColumnWidth);
        DrawIntCell(statsProperty, "blockCount", DefaultColumnWidth);
        DrawFloatCell(statsProperty, "moveSpeed", DefaultColumnWidth);
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawHeaderCell(string label, float width)
    {
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(width));
    }

    private static void DrawFloatCell(SerializedProperty parent, string propertyName, float width)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        DrawCell(property != null ? property.floatValue.ToString("0.###") : "-", width);
    }

    private static void DrawIntCell(SerializedProperty parent, string propertyName, float width)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        DrawCell(property != null ? property.intValue.ToString() : "-", width);
    }

    private static void DrawCell(string value, float width)
    {
        GUILayout.Label(value, GUILayout.Width(width));
    }

    private static float GetTableWidth()
    {
        return LevelColumnWidth
               + DefaultColumnWidth * 5f
               + AttackIntervalColumnWidth
               + SpecialDefenseColumnWidth;
    }
}
