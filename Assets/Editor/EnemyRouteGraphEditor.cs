using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(EnemyRouteGraph))]
public class EnemyRouteGraphEditor : Editor
{
    private SerializedProperty routesProperty;
    private ReorderableList routesList;

    private void OnEnable()
    {
        routesProperty = serializedObject.FindProperty("routes");
        routesList = new ReorderableList(serializedObject, routesProperty, true, true, true, true);
        routesList.drawHeaderCallback = DrawRoutesHeader;
        routesList.drawElementCallback = DrawRouteElement;
        routesList.elementHeightCallback = GetRouteElementHeight;
        routesList.onSelectCallback = SelectRoute;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        routesList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRoutesHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Routes");
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
        EnemyRouteGraph routeGraph = (EnemyRouteGraph)target;
        routeGraph.SetSelectedRouteIndex(list.index);
        SceneView.RepaintAll();
    }
}
