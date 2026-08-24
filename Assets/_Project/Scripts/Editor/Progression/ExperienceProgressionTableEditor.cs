using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExperienceProgressionTable))]
public sealed class ExperienceProgressionTableEditor : Editor
{
    private const float LevelColumnWidth = 80f;
    private const float ExperienceColumnWidth = 180f;
    private const float RowHeight = 22f;
    private const float MaxTableHeight = 500f;

    private SerializedProperty experienceThresholdsProperty;
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        experienceThresholdsProperty = serializedObject.FindProperty("experienceThresholds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        int thresholdCount = experienceThresholdsProperty?.arraySize ?? 0;
        int maxLevel = thresholdCount + 1;
        EditorGUILayout.LabelField("Max Level", maxLevel.ToString());
        EditorGUILayout.Space(4f);

        float tableHeight = Mathf.Min(MaxTableHeight, (maxLevel + 1) * RowHeight + 8f);
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            true,
            true,
            GUILayout.Height(tableHeight));

        float tableWidth = LevelColumnWidth + ExperienceColumnWidth;
        EditorGUILayout.BeginVertical(GUILayout.Width(tableWidth));
        DrawTableHeader();

        for (int index = 0; index < thresholdCount; index++)
        {
            DrawRow(index);
        }

        DrawMaxLevelRow(maxLevel);

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private static void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(RowHeight));
        GUILayout.Label("Level", EditorStyles.boldLabel, GUILayout.Width(LevelColumnWidth));
        GUILayout.Label("EXP", EditorStyles.boldLabel, GUILayout.Width(ExperienceColumnWidth));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRow(int index)
    {
        SerializedProperty experienceProperty = experienceThresholdsProperty.GetArrayElementAtIndex(index);

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(RowHeight));
        GUILayout.Label($"LV.{index + 1}", GUILayout.Width(LevelColumnWidth));
        GUILayout.Label(experienceProperty.intValue.ToString(), GUILayout.Width(ExperienceColumnWidth));
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawMaxLevelRow(int maxLevel)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(RowHeight));
        GUILayout.Label($"LV.{maxLevel}", GUILayout.Width(LevelColumnWidth));
        GUILayout.Label("MAX", EditorStyles.boldLabel, GUILayout.Width(ExperienceColumnWidth));
        EditorGUILayout.EndHorizontal();
    }
}
