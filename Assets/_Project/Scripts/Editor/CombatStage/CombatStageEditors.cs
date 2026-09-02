using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CombatStageAuthoring))]
public class CombatStageAuthoringEditor : Editor
{
    private SerializedProperty stageIdProperty;
    private SerializedProperty stageNameProperty;
    private SerializedProperty mapViewProperty;
    private SerializedProperty tilemapSourcesProperty;
    private SerializedProperty routesProperty;
    private SerializedProperty spawnPointsProperty;
    private SerializedProperty startConfigProperty;
    private SerializedProperty spawnEventsProperty;
    private SerializedProperty outputDefinitionProperty;
    private ReorderableList routesList;
    private ReorderableList spawnEventList;
    private string importError;

    private void OnEnable()
    {
        stageIdProperty = serializedObject.FindProperty("stageId");
        stageNameProperty = serializedObject.FindProperty("stageName");
        mapViewProperty = serializedObject.FindProperty("mapView");
        tilemapSourcesProperty = serializedObject.FindProperty("tilemapSources");
        routesProperty = serializedObject.FindProperty("routes");
        spawnPointsProperty = serializedObject.FindProperty("spawnPoints");
        startConfigProperty = serializedObject.FindProperty("startConfig");
        spawnEventsProperty = serializedObject.FindProperty("spawnEvents");
        outputDefinitionProperty = serializedObject.FindProperty("outputDefinition");

        routesList = new ReorderableList(serializedObject, routesProperty, true, true, true, true);
        routesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Enemy Routes");
        routesList.elementHeightCallback = GetRouteElementHeight;
        routesList.drawElementCallback = DrawRouteElement;
        routesList.onSelectCallback = SelectRoute;

        spawnEventList = new ReorderableList(serializedObject, spawnEventsProperty, true, true, true, true);
        spawnEventList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Spawn Timeline");
        spawnEventList.elementHeightCallback = GetSpawnEventHeight;
        spawnEventList.drawElementCallback = DrawSpawnEvent;
        spawnEventList.onAddCallback = AddSpawnEvent;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Identity");
        EditorGUILayout.PropertyField(stageIdProperty);
        EditorGUILayout.PropertyField(stageNameProperty);

        DrawSection("Map");
        EditorGUILayout.PropertyField(mapViewProperty);
        EditorGUILayout.PropertyField(tilemapSourcesProperty, true);

        DrawSection("Routes & Spawn Points");
        routesList.DoLayoutList();
        EditorGUILayout.PropertyField(spawnPointsProperty, true);

        DrawSection("Stage Config");
        EditorGUILayout.PropertyField(startConfigProperty, true);

        EditorGUILayout.Space(8f);
        spawnEventList.DoLayoutList();
        DrawSelectedSpawnEventDetails();

        EditorGUILayout.Space(8f);
        DrawSection("Stage Definition");
        CombatStageDefinition currentDefinition = outputDefinitionProperty.objectReferenceValue as CombatStageDefinition;
        EditorGUI.BeginChangeCheck();
        CombatStageDefinition selectedDefinition = EditorGUILayout.ObjectField(
            new GUIContent("Output Definition"),
            currentDefinition,
            typeof(CombatStageDefinition),
            false) as CombatStageDefinition;
        bool definitionChanged = EditorGUI.EndChangeCheck();
        if (!string.IsNullOrEmpty(importError))
        {
            EditorGUILayout.HelpBox(importError, MessageType.Error);
        }

        serializedObject.ApplyModifiedProperties();

        if (definitionChanged)
        {
            ImportSelectedDefinition(selectedDefinition);
        }

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

    private void DrawSpawnEvent(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty spawnEvent = spawnEventsProperty.GetArrayElementAtIndex(index);
        CombatStageSpawnEventInspectorGUI.DrawAuthoringTimelineRow(rect, spawnEvent);
    }

    private float GetSpawnEventHeight(int index)
    {
        return CombatStageSpawnEventInspectorGUI.GetTimelineRowHeight();
    }

    private void AddSpawnEvent(ReorderableList list)
    {
        int index = spawnEventsProperty.arraySize;
        spawnEventsProperty.arraySize++;
        SerializedProperty spawnEvent = spawnEventsProperty.GetArrayElementAtIndex(index);
        spawnEvent.FindPropertyRelative("eventId").stringValue = string.Empty;
        spawnEvent.FindPropertyRelative("enemyDefinition").objectReferenceValue = null;
        spawnEvent.FindPropertyRelative("spawnPoint").objectReferenceValue = null;
        spawnEvent.FindPropertyRelative("spawnPointId").stringValue = string.Empty;
        spawnEvent.FindPropertyRelative("routeId").stringValue = string.Empty;
        spawnEvent.FindPropertyRelative("groupCount").intValue = 1;
        spawnEvent.FindPropertyRelative("enemyCount").intValue = 1;
        spawnEvent.FindPropertyRelative("interval").floatValue = 0.5f;
        spawnEvent.FindPropertyRelative("startCondition").enumValueIndex = (int)EnemySpawnEventStartCondition.AfterDelay;
        spawnEvent.FindPropertyRelative("requiredEventId").stringValue = string.Empty;
        spawnEvent.FindPropertyRelative("startDelay").floatValue = 0f;
        spawnEvent.isExpanded = true;
        list.index = index;
    }

    private void DrawSelectedSpawnEventDetails()
    {
        int selectedIndex = spawnEventList.index;
        if (selectedIndex < 0 || selectedIndex >= spawnEventsProperty.arraySize)
        {
            if (spawnEventsProperty.arraySize > 0)
            {
                selectedIndex = 0;
                spawnEventList.index = selectedIndex;
            }
            else
            {
                return;
            }
        }

        SerializedProperty spawnEvent = spawnEventsProperty.GetArrayElementAtIndex(selectedIndex);
        List<string> routeIds = GetRouteIds();
        List<string> dependencyIds = GetDependencyIds(selectedIndex);

        EditorGUILayout.Space(4f);
        CombatStageSpawnEventInspectorGUI.DrawAuthoringEventFields(spawnEvent, routeIds, dependencyIds);
    }

    private List<string> GetRouteIds()
    {
        List<string> routeIds = new List<string>();
        for (int i = 0; i < routesProperty.arraySize; i++)
        {
            SerializedProperty routeIdProperty = routesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("routeId");
            string routeId = routeIdProperty.stringValue;
            if (!string.IsNullOrWhiteSpace(routeId) && !routeIds.Contains(routeId))
            {
                routeIds.Add(routeId);
            }
        }

        CombatStageDefinition outputDefinition = outputDefinitionProperty.objectReferenceValue as CombatStageDefinition;
        if (outputDefinition != null && outputDefinition.MapData != null && outputDefinition.MapData.Routes != null)
        {
            IReadOnlyList<EnemyRouteDefinition> definitionRoutes = outputDefinition.MapData.Routes;
            for (int i = 0; i < definitionRoutes.Count; i++)
            {
                EnemyRouteDefinition route = definitionRoutes[i];
                if (route != null && !string.IsNullOrWhiteSpace(route.RouteId) && !routeIds.Contains(route.RouteId))
                {
                    routeIds.Add(route.RouteId);
                }
            }
        }

        return routeIds;
    }

    private List<string> GetDependencyIds(int excludedIndex)
    {
        List<string> eventIds = new List<string>();
        for (int i = 0; i < spawnEventsProperty.arraySize; i++)
        {
            if (i == excludedIndex)
            {
                continue;
            }

            SerializedProperty eventIdProperty = spawnEventsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("eventId");
            string eventId = eventIdProperty.stringValue;
            if (!string.IsNullOrWhiteSpace(eventId) && !eventIds.Contains(eventId))
            {
                eventIds.Add(eventId);
            }
        }

        return eventIds;
    }

    private void ImportSelectedDefinition(CombatStageDefinition definition)
    {
        CombatStageAuthoring authoring = (CombatStageAuthoring)target;
        Undo.RecordObject(authoring, "Import Combat Stage Definition");

        if (definition == null)
        {
            importError = string.Empty;
            authoring.SetOutputDefinition(null);
            EditorUtility.SetDirty(authoring);
            PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
            serializedObject.Update();
            return;
        }

        if (!authoring.TryImportDefinition(definition, out string error))
        {
            importError = error;
            Debug.LogError($"[CombatStageAuthoringEditor] Could not import '{definition.name}': {error}", authoring);
            serializedObject.Update();
            return;
        }

        importError = string.Empty;
        EditorUtility.SetDirty(authoring);
        PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
        serializedObject.Update();
        SceneView.RepaintAll();
        Debug.Log($"[CombatStageAuthoringEditor] Imported '{definition.name}' into '{authoring.name}'.", authoring);
    }

    private static void DrawSection(string title)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
}

[CustomEditor(typeof(CombatStageDefinition))]
public class CombatStageDefinitionEditor : Editor
{
    private SerializedProperty stageIdProperty;
    private SerializedProperty stageNameProperty;
    private SerializedProperty mapPrefabProperty;
    private SerializedProperty mapPreviewProperty;
    private SerializedProperty rewardDefinitionProperty;
    private SerializedProperty spawnEventsProperty;
    private ReorderableList spawnEventList;

    private void OnEnable()
    {
        stageIdProperty = serializedObject.FindProperty("stageId");
        stageNameProperty = serializedObject.FindProperty("stageName");
        mapPrefabProperty = serializedObject.FindProperty("mapPrefab");
        mapPreviewProperty = serializedObject.FindProperty("mapPreview");
        rewardDefinitionProperty = serializedObject.FindProperty("rewardDefinition");
        spawnEventsProperty = serializedObject.FindProperty("spawnEvents");

        spawnEventList = new ReorderableList(serializedObject, spawnEventsProperty, false, true, false, false);
        spawnEventList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Spawn Events (Read Only)");
        spawnEventList.elementHeightCallback = GetSpawnEventHeight;
        spawnEventList.drawElementCallback = DrawSpawnEvent;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        CombatStageDefinition definition = (CombatStageDefinition)target;
        EditorGUILayout.HelpBox(
            "Exported stage data is read only. Assign this asset to CombatStageAuthoring to import and edit it.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(true))
        {
            DrawSection("Identity");
            EditorGUILayout.PropertyField(stageIdProperty);
            EditorGUILayout.PropertyField(stageNameProperty);

            DrawSection("Map");
            EditorGUILayout.PropertyField(mapPrefabProperty);
        }

        EditorGUILayout.PropertyField(mapPreviewProperty);
        if (GUILayout.Button("Regenerate Map Preview"))
        {
            serializedObject.ApplyModifiedProperties();
            CombatStagePreviewExporter.GenerateMapPreview(definition);
            serializedObject.Update();
        }

        CombatMapData mapData = definition.MapData;
        if (mapData != null)
        {
            EditorGUILayout.LabelField("Grid Cells", mapData.GridCells.Count.ToString());
            EditorGUILayout.LabelField("Routes", mapData.Routes.Count.ToString());
            EditorGUILayout.LabelField("Spawn Points", mapData.SpawnPoints.Count.ToString());
        }

        DrawSection("Stage Config");
        CombatStageStartConfig startConfig = definition.StartConfig;
        if (startConfig != null)
        {
            EditorGUILayout.LabelField("Starting Meat", startConfig.StartingMeat.ToString());
            EditorGUILayout.LabelField("Starting Lives", startConfig.StartingLives.ToString());
            EditorGUILayout.LabelField("Natural Meat / Second", startConfig.NaturalMeatPerSecond.ToString());
        }

        DrawSection("Reward");
        EditorGUILayout.PropertyField(rewardDefinitionProperty);

        EditorGUILayout.Space(8f);
        spawnEventList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpawnEvent(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty spawnEvent = spawnEventsProperty.GetArrayElementAtIndex(index);
        CombatStageSpawnEventInspectorGUI.DrawDefinitionEvent(rect, spawnEvent);
    }

    private float GetSpawnEventHeight(int index)
    {
        SerializedProperty spawnEvent = spawnEventsProperty.GetArrayElementAtIndex(index);
        return CombatStageSpawnEventInspectorGUI.GetElementHeight(spawnEvent);
    }

    private static void DrawSection(string title)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
}

internal static class CombatStageSpawnEventInspectorGUI
{
    private const float Padding = 4f;

    private static float LineHeight => EditorGUIUtility.singleLineHeight;
    private static float LineSpacing => EditorGUIUtility.standardVerticalSpacing;

    public static float GetTimelineRowHeight()
    {
        return LineHeight + Padding * 2f;
    }

    public static float GetElementHeight(SerializedProperty spawnEvent)
    {
        if (!spawnEvent.isExpanded)
        {
            return LineHeight + Padding * 2f;
        }

        SerializedProperty startCondition = spawnEvent.FindPropertyRelative("startCondition");
        int lineCount = UsesRequiredEvent(startCondition) ? 15 : 14;
        return Padding * 2f + lineCount * LineHeight + (lineCount - 1) * LineSpacing;
    }

    public static void DrawAuthoringTimelineRow(Rect rect, SerializedProperty spawnEvent)
    {
        Rect line = new Rect(rect.x, rect.y + Padding, rect.width, LineHeight);
        EditorGUI.LabelField(line, GetAuthoringSummary(spawnEvent), EditorStyles.boldLabel);
    }

    public static void DrawAuthoringEventFields(SerializedProperty spawnEvent, List<string> routeIds, List<string> dependencyIds)
    {
        SerializedProperty eventId = spawnEvent.FindPropertyRelative("eventId");
        SerializedProperty enemyDefinition = spawnEvent.FindPropertyRelative("enemyDefinition");
        SerializedProperty spawnPoint = spawnEvent.FindPropertyRelative("spawnPoint");
        SerializedProperty spawnPointId = spawnEvent.FindPropertyRelative("spawnPointId");
        SerializedProperty routeId = spawnEvent.FindPropertyRelative("routeId");
        SerializedProperty groupCount = spawnEvent.FindPropertyRelative("groupCount");
        SerializedProperty enemyCount = spawnEvent.FindPropertyRelative("enemyCount");
        SerializedProperty interval = spawnEvent.FindPropertyRelative("interval");
        SerializedProperty startCondition = spawnEvent.FindPropertyRelative("startCondition");
        SerializedProperty requiredEventId = spawnEvent.FindPropertyRelative("requiredEventId");
        SerializedProperty startDelay = spawnEvent.FindPropertyRelative("startDelay");

        EditorGUILayout.LabelField("Selected Spawn Event", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Identity", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(eventId);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Actor & Route", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(enemyDefinition);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(spawnPoint, new GUIContent("Spawn Point"));
        if (EditorGUI.EndChangeCheck())
        {
            EnemySpawnPoint selectedSpawnPoint = spawnPoint.objectReferenceValue as EnemySpawnPoint;
            if (selectedSpawnPoint != null && !string.IsNullOrWhiteSpace(selectedSpawnPoint.SpawnPointId))
            {
                spawnPointId.stringValue = selectedSpawnPoint.SpawnPointId;
            }
        }

        EditorGUILayout.PropertyField(spawnPointId, new GUIContent("Spawn Point Id"));
        DrawStringPopupLayout(new GUIContent("Route"), routeId, routeIds);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Wave", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(groupCount, new GUIContent("Group Count"));
        EditorGUILayout.PropertyField(enemyCount, new GUIContent("Enemies Per Group"));
        EditorGUILayout.PropertyField(interval, new GUIContent("Group Interval"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Start", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(startCondition, new GUIContent("Condition"));
        if (UsesRequiredEvent(startCondition))
        {
            DrawStringPopupLayout(new GUIContent("Required Event"), requiredEventId, dependencyIds);
        }

        EditorGUILayout.PropertyField(startDelay, new GUIContent("Delay"));
        EditorGUILayout.EndVertical();
    }

    public static void DrawDefinitionEvent(Rect rect, SerializedProperty spawnEvent)
    {
        SerializedProperty eventId = spawnEvent.FindPropertyRelative("eventId");
        SerializedProperty enemyDefinition = spawnEvent.FindPropertyRelative("enemyDefinition");
        SerializedProperty spawnPointId = spawnEvent.FindPropertyRelative("spawnPointId");
        SerializedProperty routeId = spawnEvent.FindPropertyRelative("routeId");
        SerializedProperty groupCount = spawnEvent.FindPropertyRelative("groupCount");
        SerializedProperty enemiesPerGroup = spawnEvent.FindPropertyRelative("enemiesPerGroup");
        SerializedProperty groupInterval = spawnEvent.FindPropertyRelative("groupInterval");
        SerializedProperty startCondition = spawnEvent.FindPropertyRelative("startCondition");
        SerializedProperty requiredEventId = spawnEvent.FindPropertyRelative("requiredEventId");
        SerializedProperty startDelay = spawnEvent.FindPropertyRelative("startDelay");

        float y = rect.y + Padding;
        DrawFoldoutSummary(rect, ref y, spawnEvent, GetDefinitionSummary(spawnEvent));
        if (!spawnEvent.isExpanded)
        {
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            DrawSectionLabel(NextLine(rect, ref y), "Identity");
            EditorGUI.PropertyField(NextLine(rect, ref y), eventId);

            DrawSectionLabel(NextLine(rect, ref y), "Actor & Route");
            EditorGUI.PropertyField(NextLine(rect, ref y), enemyDefinition);
            EditorGUI.PropertyField(NextLine(rect, ref y), spawnPointId, new GUIContent("Spawn Point"));
            EditorGUI.PropertyField(NextLine(rect, ref y), routeId, new GUIContent("Route"));

            DrawSectionLabel(NextLine(rect, ref y), "Wave");
            EditorGUI.PropertyField(NextLine(rect, ref y), groupCount, new GUIContent("Group Count"));
            EditorGUI.PropertyField(NextLine(rect, ref y), enemiesPerGroup, new GUIContent("Enemies Per Group"));
            EditorGUI.PropertyField(NextLine(rect, ref y), groupInterval, new GUIContent("Group Interval"));

            DrawSectionLabel(NextLine(rect, ref y), "Start");
            EditorGUI.PropertyField(NextLine(rect, ref y), startCondition, new GUIContent("Condition"));
            if (UsesRequiredEvent(startCondition))
            {
                EditorGUI.PropertyField(NextLine(rect, ref y), requiredEventId, new GUIContent("Required Event"));
            }

            EditorGUI.PropertyField(NextLine(rect, ref y), startDelay, new GUIContent("Delay"));
        }
    }

    private static void DrawFoldoutSummary(Rect rect, ref float y, SerializedProperty spawnEvent, string summary)
    {
        Rect line = NextLine(rect, ref y);
        Rect foldoutRect = new Rect(line.x, line.y, 16f, line.height);
        Rect summaryRect = new Rect(line.x + 16f, line.y, line.width - 16f, line.height);
        spawnEvent.isExpanded = EditorGUI.Foldout(foldoutRect, spawnEvent.isExpanded, GUIContent.none, true);
        EditorGUI.LabelField(summaryRect, summary, EditorStyles.boldLabel);
    }

    private static string GetAuthoringSummary(SerializedProperty spawnEvent)
    {
        string eventId = GetDisplayString(spawnEvent.FindPropertyRelative("eventId"), "<Event>");
        string enemyName = GetObjectName(spawnEvent.FindPropertyRelative("enemyDefinition"), "<Enemy>");
        SerializedProperty spawnPointProperty = spawnEvent.FindPropertyRelative("spawnPoint");
        EnemySpawnPoint spawnPoint = spawnPointProperty.objectReferenceValue as EnemySpawnPoint;
        string spawnPointId = GetDisplayString(spawnEvent.FindPropertyRelative("spawnPointId"), "<Spawn>");
        if (spawnPoint != null)
        {
            if (!string.IsNullOrWhiteSpace(spawnPoint.SpawnPointId))
            {
                spawnPointId = spawnPoint.SpawnPointId;
            }
            else
            {
                spawnPointId = spawnPoint.name;
            }
        }

        string routeId = GetDisplayString(spawnEvent.FindPropertyRelative("routeId"), "<Route>");
        int groups = spawnEvent.FindPropertyRelative("groupCount").intValue;
        int enemies = spawnEvent.FindPropertyRelative("enemyCount").intValue;
        return $"{eventId} | {enemyName} | {groups} x {enemies} | {spawnPointId} -> {routeId}";
    }

    private static string GetDefinitionSummary(SerializedProperty spawnEvent)
    {
        string eventId = GetDisplayString(spawnEvent.FindPropertyRelative("eventId"), "<Event>");
        string enemyName = GetObjectName(spawnEvent.FindPropertyRelative("enemyDefinition"), "<Enemy>");
        string spawnPointId = GetDisplayString(spawnEvent.FindPropertyRelative("spawnPointId"), "<Spawn>");
        string routeId = GetDisplayString(spawnEvent.FindPropertyRelative("routeId"), "<Route>");
        int groups = spawnEvent.FindPropertyRelative("groupCount").intValue;
        int enemies = spawnEvent.FindPropertyRelative("enemiesPerGroup").intValue;
        return $"{eventId} | {enemyName} | {groups} x {enemies} | {spawnPointId} -> {routeId}";
    }

    private static string GetDisplayString(SerializedProperty property, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(property.stringValue))
        {
            return property.stringValue;
        }

        return fallback;
    }

    private static string GetObjectName(SerializedProperty property, string fallback)
    {
        if (property.objectReferenceValue != null)
        {
            return property.objectReferenceValue.name;
        }

        return fallback;
    }

    private static bool UsesRequiredEvent(SerializedProperty startCondition)
    {
        return startCondition.enumValueIndex == (int)EnemySpawnEventStartCondition.AfterSpawnEventResolved;
    }

    private static void DrawStringPopup(Rect rect, GUIContent label, SerializedProperty property, List<string> availableIds)
    {
        List<string> values;
        int currentIndex;
        GUIContent[] displayOptions = BuildStringPopupOptions(property, availableIds, out values, out currentIndex);
        int newIndex = EditorGUI.Popup(rect, label, currentIndex, displayOptions);
        property.stringValue = values[newIndex];
    }

    private static void DrawStringPopupLayout(GUIContent label, SerializedProperty property, List<string> availableIds)
    {
        List<string> values;
        int currentIndex;
        GUIContent[] displayOptions = BuildStringPopupOptions(property, availableIds, out values, out currentIndex);
        Rect rect = EditorGUILayout.GetControlRect();
        int newIndex = EditorGUI.Popup(rect, label, currentIndex, displayOptions);
        property.stringValue = values[newIndex];
    }

    private static GUIContent[] BuildStringPopupOptions(SerializedProperty property, List<string> availableIds, out List<string> values, out int selectedIndex)
    {
        values = new List<string> { string.Empty };
        for (int i = 0; i < availableIds.Count; i++)
        {
            if (!values.Contains(availableIds[i]))
            {
                values.Add(availableIds[i]);
            }
        }

        string currentValue = property.stringValue;
        bool currentValueIsMissing = !string.IsNullOrWhiteSpace(currentValue) && !values.Contains(currentValue);
        if (currentValueIsMissing)
        {
            values.Add(currentValue);
        }

        GUIContent[] displayOptions = new GUIContent[values.Count];
        displayOptions[0] = new GUIContent("<None>");
        for (int i = 1; i < values.Count; i++)
        {
            if (currentValueIsMissing && values[i] == currentValue)
            {
                displayOptions[i] = new GUIContent($"<Missing> {values[i]}");
            }
            else
            {
                displayOptions[i] = new GUIContent(values[i]);
            }
        }

        selectedIndex = values.IndexOf(currentValue);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        return displayOptions;
    }

    private static void DrawSectionLabel(Rect rect, string label)
    {
        EditorGUI.LabelField(rect, label, EditorStyles.miniBoldLabel);
    }

    private static Rect NextLine(Rect rect, ref float y)
    {
        Rect line = new Rect(rect.x, y, rect.width, LineHeight);
        y += LineHeight + LineSpacing;
        return line;
    }

}
