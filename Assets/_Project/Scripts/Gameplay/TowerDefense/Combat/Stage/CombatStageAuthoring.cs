using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class CombatGridTilemapSource
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private CombatGridCellStates tilemapStates;
    [SerializeField] private bool isRequired = true;

    public Tilemap Tilemap => tilemap;
    public CombatGridCellStates TilemapStates => tilemapStates;
    public bool IsRequired => isRequired;
}

public class CombatStageAuthoring : MonoBehaviour
{
    private const float routeGizmoLineWidth = 4f;
    private const float routeCheckpointRadius = 0.25f;
    private const float routeArrowLength = 0.45f;
    private const float routeArrowAngle = 30f;

    [Header("Identity")]
    [SerializeField] private string stageId;
    [SerializeField] private string stageName;

    [Header("Map")]
    [SerializeField] private CombatMapView mapView;
    [SerializeField] private List<CombatGridTilemapSource> tilemapSources = new List<CombatGridTilemapSource>();

    [Header("Routes Settings")]
    [SerializeField] private List<EnemyRouteAuthoring> routes = new List<EnemyRouteAuthoring>();
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

    [Header("Stage")]
    [SerializeField] private CombatStageStartConfig startConfig = new CombatStageStartConfig();
    [SerializeField] private List<EnemySpawnEvent> spawnEvents = new List<EnemySpawnEvent>();

    [Header("Export Stage Definition Data")]
    [SerializeField] private CombatStageDefinition outputDefinition;

    private int selectedRouteIndex;

    public string StageId => stageId;
    public string StageName => stageName;
    public CombatMapView MapView => mapView;
    public CombatStageStartConfig StartConfig => startConfig;
    public CombatStageDefinition OutputDefinition => outputDefinition;

    public bool TryCreateBootstrapData(IReadOnlyList<HeroInstance> squad, out CombatBootstrapData bootstrapData)
    {
        bootstrapData = null;
        if (!TryCreateStageData(out CombatMapData mapData, out List<EnemySpawnEventDefinition> spawnEventDefinitions))
        {
            return false;
        }

        bootstrapData = new CombatBootstrapData(stageId, stageName, mapView, mapData, startConfig, spawnEventDefinitions, squad);
        return bootstrapData.IsValid;
    }

    public bool TryCreateStageData(out CombatMapData mapData, out List<EnemySpawnEventDefinition> spawnEventDefinitions)
    {
        mapData = null;
        spawnEventDefinitions = null;

        if (string.IsNullOrWhiteSpace(stageId) || string.IsNullOrWhiteSpace(stageName) || startConfig == null)
        {
            Debug.LogError("[CombatStageAuthoring] Stage id, stage name and start config are required.", this);
            return false;
        }

        if (mapView == null || mapView.Grid == null || mapView.TileOverlayRenderer == null)
        {
            Debug.LogError("[CombatStageAuthoring] CombatMapView with Grid and TileOverlayRenderer references is required.", this);
            return false;
        }

        if (!mapView.Grid.transform.IsChildOf(mapView.transform) ||
            !mapView.TileOverlayRenderer.transform.IsChildOf(mapView.transform))
        {
            Debug.LogError("[CombatStageAuthoring] Grid and TileOverlayRenderer must be inside the CombatMapView export root.", this);
            return false;
        }

        if (!TryCreateGridCells(out List<CombatGridCellDefinition> gridCells))
        {
            return false;
        }

        HashSet<Vector3Int> validCells = new HashSet<Vector3Int>();
        for (int i = 0; i < gridCells.Count; i++)
        {
            validCells.Add(gridCells[i].CellPosition);
        }

        if (!TryCreateRoutes(validCells, out List<EnemyRouteDefinition> routeDefinitions) ||
            !TryCreateSpawnPoints(validCells, out List<CombatSpawnPointDefinition> spawnPointDefinitions) ||
            !TryCreateSpawnEvents(routeDefinitions, spawnPointDefinitions, out spawnEventDefinitions))
        {
            return false;
        }

        mapData = new CombatMapData(gridCells, routeDefinitions, spawnPointDefinitions);
        return true;
    }

    private bool TryCreateGridCells(out List<CombatGridCellDefinition> definitions)
    {
        definitions = new List<CombatGridCellDefinition>();
        Dictionary<Vector3Int, CombatGridCellStates> cellStates = new Dictionary<Vector3Int, CombatGridCellStates>();

        for (int i = 0; i < tilemapSources.Count; i++)
        {
            CombatGridTilemapSource source = tilemapSources[i];
            if (source == null || source.Tilemap == null)
            {
                if (source != null && source.IsRequired)
                {
                    Debug.LogError($"[CombatStageAuthoring] Required tilemap source at index {i} has no Tilemap.", this);
                    return false;
                }

                continue;
            }

            source.Tilemap.CompressBounds();
            foreach (Vector3Int cellPosition in source.Tilemap.cellBounds.allPositionsWithin)
            {
                if (!source.Tilemap.HasTile(cellPosition))
                {
                    continue;
                }

                cellStates.TryGetValue(cellPosition, out CombatGridCellStates states);
                cellStates[cellPosition] = states | source.TilemapStates;
            }
        }

        foreach (KeyValuePair<Vector3Int, CombatGridCellStates> cell in cellStates)
        {
            definitions.Add(new CombatGridCellDefinition(cell.Key, cell.Value));
        }

        if (definitions.Count == 0)
        {
            Debug.LogError("[CombatStageAuthoring] No combat grid cells were resolved from tilemap sources.", this);
            return false;
        }

        definitions.Sort((left, right) =>
        {
            int yComparison = left.CellPosition.y.CompareTo(right.CellPosition.y);
            return yComparison != 0 ? yComparison : left.CellPosition.x.CompareTo(right.CellPosition.x);
        });
        return true;
    }

    private bool TryCreateRoutes(HashSet<Vector3Int> validCells, out List<EnemyRouteDefinition> definitions)
    {
        definitions = new List<EnemyRouteDefinition>();
        HashSet<string> routeIds = new HashSet<string>();

        for (int i = 0; i < routes.Count; i++)
        {
            EnemyRouteAuthoring route = routes[i];
            if (route == null || string.IsNullOrWhiteSpace(route.RouteId) || !routeIds.Add(route.RouteId))
            {
                Debug.LogError($"[CombatStageAuthoring] Route at index {i} is null or has an empty/duplicate id.", this);
                return false;
            }

            if (route.CheckpointCount < 2)
            {
                Debug.LogError($"[CombatStageAuthoring] Route '{route.RouteId}' requires at least two checkpoints.", this);
                return false;
            }

            List<EnemyRouteCheckpointDefinition> checkpoints = new List<EnemyRouteCheckpointDefinition>();
            for (int checkpointIndex = 0; checkpointIndex < route.CheckpointCount; checkpointIndex++)
            {
                EnemyRouteCheckpointAuthoring checkpoint = route.Checkpoints[checkpointIndex];
                if (checkpoint == null)
                {
                    Debug.LogError($"[CombatStageAuthoring] Route '{route.RouteId}' has a null checkpoint.", this);
                    return false;
                }

                Vector3Int cellPosition = mapView.Grid.WorldToCell(checkpoint.transform.position);
                if (!validCells.Contains(cellPosition))
                {
                    Debug.LogError($"[CombatStageAuthoring] Checkpoint '{checkpoint.CheckpointId}' does not resolve to a combat grid cell.", checkpoint);
                    return false;
                }

                checkpoints.Add(new EnemyRouteCheckpointDefinition(checkpoint.CheckpointId, checkpoint.CheckpointType, cellPosition));
            }

            definitions.Add(new EnemyRouteDefinition(route.RouteId, checkpoints));
        }

        return true;
    }

    private bool TryCreateSpawnPoints(HashSet<Vector3Int> validCells, out List<CombatSpawnPointDefinition> definitions)
    {
        definitions = new List<CombatSpawnPointDefinition>();
        HashSet<string> spawnPointIds = new HashSet<string>();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            EnemySpawnPoint spawnPoint = spawnPoints[i];
            if (spawnPoint == null || string.IsNullOrWhiteSpace(spawnPoint.SpawnPointId) || !spawnPointIds.Add(spawnPoint.SpawnPointId))
            {
                Debug.LogError($"[CombatStageAuthoring] Spawn point at index {i} is null or has an empty/duplicate id.", this);
                return false;
            }

            Vector3Int cellPosition = mapView.Grid.WorldToCell(spawnPoint.WorldPosition);
            if (!validCells.Contains(cellPosition))
            {
                Debug.LogError($"[CombatStageAuthoring] Spawn point '{spawnPoint.SpawnPointId}' does not resolve to a combat grid cell.", spawnPoint);
                return false;
            }

            definitions.Add(new CombatSpawnPointDefinition(spawnPoint.SpawnPointId, cellPosition));
        }

        return true;
    }

    private bool TryCreateSpawnEvents(IReadOnlyList<EnemyRouteDefinition> routeDefinitions, IReadOnlyList<CombatSpawnPointDefinition> spawnPointDefinitions,
                                      out List<EnemySpawnEventDefinition> definitions)
    {
        definitions = new List<EnemySpawnEventDefinition>();
        HashSet<string> routeIds = new HashSet<string>();
        HashSet<string> spawnPointIds = new HashSet<string>();
        HashSet<string> eventIds = new HashSet<string>();
        Dictionary<string, EnemyRouteDefinition> routesById = new Dictionary<string, EnemyRouteDefinition>();
        Dictionary<string, CombatSpawnPointDefinition> spawnPointsById = new Dictionary<string, CombatSpawnPointDefinition>();

        for (int i = 0; i < routeDefinitions.Count; i++)
        {
            routeIds.Add(routeDefinitions[i].RouteId);
            routesById.Add(routeDefinitions[i].RouteId, routeDefinitions[i]);
        }

        for (int i = 0; i < spawnPointDefinitions.Count; i++)
        {
            spawnPointIds.Add(spawnPointDefinitions[i].SpawnPointId);
            spawnPointsById.Add(spawnPointDefinitions[i].SpawnPointId, spawnPointDefinitions[i]);
        }

        for (int i = 0; i < spawnEvents.Count; i++)
        {
            EnemySpawnEvent spawnEvent = spawnEvents[i];
            string spawnPointId = spawnEvent?.SpawnPoint != null ? spawnEvent.SpawnPoint.SpawnPointId : null;
            if (spawnEvent == null || string.IsNullOrWhiteSpace(spawnEvent.EventId) || !eventIds.Add(spawnEvent.EventId) ||
                spawnEvent.EnemyDefinition == null || !spawnEvent.EnemyDefinition.IsValid ||
                !spawnPointIds.Contains(spawnPointId) || !routeIds.Contains(spawnEvent.RouteId) ||
                spawnEvent.GroupCount <= 0 || spawnEvent.EnemiesPerGroup <= 0)
            {
                Debug.LogError($"[CombatStageAuthoring] Spawn event at index {i} has invalid identity, actor, route, spawn point, or count.", this);
                return false;
            }

            EnemyRouteDefinition route = routesById[spawnEvent.RouteId];
            CombatSpawnPointDefinition spawnPoint = spawnPointsById[spawnPointId];
            if (route.Checkpoints[0].CellPosition != spawnPoint.CellPosition)
            {
                return false;
            }

            definitions.Add(new EnemySpawnEventDefinition(
                spawnEvent.EventId,
                spawnEvent.EnemyDefinition,
                spawnPointId,
                spawnEvent.RouteId,
                spawnEvent.GroupCount,
                spawnEvent.EnemiesPerGroup,
                Mathf.Max(0f, spawnEvent.GroupInterval),
                spawnEvent.StartCondition,
                spawnEvent.RequiredEventId,
                Mathf.Max(0f, spawnEvent.StartDelay)));
        }

        return ValidateSpawnEventDependencies(definitions);
    }

    private bool ValidateSpawnEventDependencies(IReadOnlyList<EnemySpawnEventDefinition> definitions)
    {
        Dictionary<string, EnemySpawnEventDefinition> eventsById = new Dictionary<string, EnemySpawnEventDefinition>();
        for (int i = 0; i < definitions.Count; i++)
        {
            eventsById.Add(definitions[i].EventId, definitions[i]);
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            EnemySpawnEventDefinition spawnEvent = definitions[i];
            if (spawnEvent.StartCondition != EnemySpawnEventStartCondition.AfterSpawnEventResolved)
            {
                continue;
            }

            HashSet<string> dependencyChain = new HashSet<string> { spawnEvent.EventId };
            EnemySpawnEventDefinition currentEvent = spawnEvent;
            while (currentEvent.StartCondition == EnemySpawnEventStartCondition.AfterSpawnEventResolved)
            {
                if (string.IsNullOrWhiteSpace(currentEvent.RequiredEventId) ||
                    !eventsById.TryGetValue(currentEvent.RequiredEventId, out currentEvent))
                {
                    Debug.LogError($"[CombatStageAuthoring] Spawn event '{spawnEvent.EventId}' has a missing dependency.", this);
                    return false;
                }

                if (!dependencyChain.Add(currentEvent.EventId))
                {
                    Debug.LogError($"[CombatStageAuthoring] Spawn event '{spawnEvent.EventId}' has a circular dependency.", this);
                    return false;
                }
            }
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        if (routes == null || routes.Count == 0)
        {
            return;
        }

        EnemyRouteAuthoring route = routes[Mathf.Clamp(selectedRouteIndex, 0, routes.Count - 1)];
        if (route == null || route.CheckpointCount < 2)
        {
            return;
        }

        for (int i = 0; i < route.CheckpointCount - 1; i++)
        {
            EnemyRouteCheckpointAuthoring from = route.Checkpoints[i];
            EnemyRouteCheckpointAuthoring to = route.Checkpoints[i + 1];
            if (from == null || to == null)
            {
                continue;
            }

            DrawRouteLine(from.transform.position, to.transform.position);
            DrawRouteArrow(from.transform.position, to.transform.position);
        }

        for (int i = 0; i < route.CheckpointCount; i++)
        {
            if (route.Checkpoints[i] != null)
            {
                DrawCheckpointMarker(route.Checkpoints[i].transform.position);
            }
        }
    }

    private void DrawRouteLine(Vector3 from, Vector3 to)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawAAPolyLine(routeGizmoLineWidth, from, to);
#else
        Gizmos.color = Color.red;
        Gizmos.DrawLine(from, to);
#endif
    }

    private void DrawCheckpointMarker(Vector3 position)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawSolidDisc(position, Vector3.forward, routeCheckpointRadius);
#else
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(position, routeCheckpointRadius);
#endif
    }

    private void DrawRouteArrow(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        direction.Normalize();
        Vector3 arrowPosition = Vector3.Lerp(from, to, 0.5f);
        Vector3 leftPoint = arrowPosition + Quaternion.Euler(0f, 0f, 180f + routeArrowAngle) * direction * routeArrowLength;
        Vector3 rightPoint = arrowPosition + Quaternion.Euler(0f, 0f, 180f - routeArrowAngle) * direction * routeArrowLength;
        DrawRouteLine(leftPoint, arrowPosition);
        DrawRouteLine(arrowPosition, rightPoint);
    }

#if UNITY_EDITOR
    public void SetSelectedRouteIndex(int index)
    {
        selectedRouteIndex = Mathf.Max(0, index);
    }

    public void SetOutputDefinition(CombatStageDefinition definition)
    {
        outputDefinition = definition;
    }
#endif
}
