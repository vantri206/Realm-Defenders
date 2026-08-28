using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CombatStageAuthoring))]
public class CombatStageAuthoringEditor : Editor
{
    private SerializedProperty routesProperty;
    private ReorderableList routesList;

    private void OnEnable()
    {
        routesProperty = serializedObject.FindProperty("routes");
        if (routesProperty == null)
        {
            return;
        }

        routesList = new ReorderableList(serializedObject, routesProperty, true, true, true, true);
        routesList.drawHeaderCallback = DrawRoutesHeader;
        routesList.drawElementCallback = DrawRouteElement;
        routesList.elementHeightCallback = GetRouteElementHeight;
        routesList.onSelectCallback = SelectRoute;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "routes");

        if (routesList != null)
        {
            EditorGUILayout.Space();
            routesList.DoLayoutList();
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        CombatStageAuthoring authoring = (CombatStageAuthoring)target;

        if (GUILayout.Button("Validate Stage"))
        {
            if (authoring.TryCreateStageData(out _, out _))
            {
                Debug.Log($"[CombatStageAuthoring] Stage '{authoring.StageId}' is valid.", authoring);
            }
        }

        if (GUILayout.Button("Export Stage Definition"))
        {
            CombatStageExporter.Export(authoring);
        }
    }

    private void DrawRoutesHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Enemy Routes");
    }

    private void DrawRouteElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty routeProperty = routesProperty.GetArrayElementAtIndex(index);
        rect.y += 2f;
        rect.height = EditorGUI.GetPropertyHeight(routeProperty, true);
        EditorGUI.PropertyField(rect, routeProperty, new GUIContent($"Route {index}"), true);
    }

    private float GetRouteElementHeight(int index)
    {
        SerializedProperty routeProperty = routesProperty.GetArrayElementAtIndex(index);
        return EditorGUI.GetPropertyHeight(routeProperty, true) + 6f;
    }

    private void SelectRoute(ReorderableList list)
    {
        serializedObject.ApplyModifiedProperties();

        CombatStageAuthoring authoring = (CombatStageAuthoring)target;
        authoring.SetSelectedRouteIndex(list.index);
        serializedObject.Update();
        SceneView.RepaintAll();
    }
}
