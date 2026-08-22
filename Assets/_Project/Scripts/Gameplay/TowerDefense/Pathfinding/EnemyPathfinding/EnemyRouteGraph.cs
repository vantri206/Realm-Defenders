using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyRouteGraph : MonoBehaviour
{
    [SerializeField] private List<EnemyRouteDefinition> routes = new List<EnemyRouteDefinition>();

    private List<RouteCheckpoint> checkpoints = new List<RouteCheckpoint>();
    public IReadOnlyList<RouteCheckpoint> Checkpoints => checkpoints;

    private int selectedRouteIndex = 0;
    private Color selectedRouteGizmoColor = Color.white;
    private float selectedRouteGizmoLineWidth = 4f;
    private float selectedRouteCheckpointRadius = 0.25f;
    private float selectedRouteArrowLength = 0.45f;
    private float selectedRouteArrowAngle = 30f;

    public void InitializeRoutes(CombatGrid combatGrid)
    {
        checkpoints.Clear();

        for (int i = 0; i < routes.Count; i++)
        {
            var route = routes[i];
            if (route == null)
            {
                Debug.LogError($"Route at index {i} is null.");
                continue;
            }

            foreach (var checkpoint in route.Checkpoints)
            {
                if (checkpoint != null && !checkpoints.Contains(checkpoint))
                {
                    if (checkpoint.Initialize(combatGrid))
                    {
                        checkpoints.Add(checkpoint);
                    }
                }
            }
        }
    }

    public EnemyRouteDefinition GetRouteById(string routeId)
    {
        foreach (var route in routes)
        if (route != null)
        {
            if (route.RouteId == routeId)
            {
                return route;
            }
        }

        return null;
    }

    public EnemyRouteDefinition GetDefaultRoute()
    {
        if (routes == null || routes.Count == 0)
        {
            return null;
        }
        return routes.FirstOrDefault();
    }

    private void OnDrawGizmos()
    {
        EnemyRouteDefinition selectedRoute = GetSelectedRoute();
        if (selectedRoute == null || selectedRoute.CheckpointCount < 2)
        {
            return;
        }

        for (int i = 0; i < selectedRoute.CheckpointCount - 1; i++)
        {
            if (!selectedRoute.TryGetCheckpoint(i, out RouteCheckpoint from) ||
                !selectedRoute.TryGetCheckpoint(i + 1, out RouteCheckpoint to))
            {
                continue;
            }

            Vector3 fromPosition = from.transform.position;
            Vector3 toPosition = to.transform.position;

            DrawRouteLine(fromPosition, toPosition);
            DrawRouteArrow(fromPosition, toPosition);
            DrawCheckpointLine(fromPosition, toPosition);
        }

        for (int i = 0; i < selectedRoute.CheckpointCount; i++)
        {
            if (!selectedRoute.TryGetCheckpoint(i, out RouteCheckpoint checkpoint))
            {
                continue;
            }

            DrawCheckpointMarker(checkpoint.transform.position);
        }
    }

    private EnemyRouteDefinition GetSelectedRoute()
    {
        if (routes == null || routes.Count == 0)
        {
            return null;
        }

        int routeIndex = Mathf.Clamp(selectedRouteIndex, 0, routes.Count - 1);
        return routes[routeIndex];
    }

#if UNITY_EDITOR
    public void SetSelectedRouteIndex(int index)
    {
        selectedRouteIndex = Mathf.Max(0, index);
    }
#endif

    private void DrawRouteLine(Vector3 from, Vector3 to)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = selectedRouteGizmoColor;
        UnityEditor.Handles.DrawAAPolyLine(selectedRouteGizmoLineWidth, from, to);
#else
        Gizmos.color = selectedRouteGizmoColor;
        Gizmos.DrawLine(from, to);
#endif
    }

    private void DrawCheckpointMarker(Vector3 position)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = selectedRouteGizmoColor;
        UnityEditor.Handles.DrawSolidDisc(position, Vector3.forward, selectedRouteCheckpointRadius);
#else
        Gizmos.color = selectedRouteGizmoColor;
        Gizmos.DrawSphere(position, selectedRouteCheckpointRadius);
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
        Vector3 leftDirection = Quaternion.Euler(0f, 0f, 180f + selectedRouteArrowAngle) * direction;
        Vector3 rightDirection = Quaternion.Euler(0f, 0f, 180f - selectedRouteArrowAngle) * direction;
        Vector3 leftPoint = arrowPosition + leftDirection * selectedRouteArrowLength;
        Vector3 rightPoint = arrowPosition + rightDirection * selectedRouteArrowLength;

#if UNITY_EDITOR
        UnityEditor.Handles.color = selectedRouteGizmoColor;
        UnityEditor.Handles.DrawAAPolyLine(selectedRouteGizmoLineWidth, leftPoint, arrowPosition, rightPoint);
#else
        Gizmos.color = selectedRouteGizmoColor;
        Gizmos.DrawLine(leftPoint, arrowPosition);
        Gizmos.DrawLine(rightPoint, arrowPosition);
#endif
    }

    private void DrawCheckpointLine(Vector3 from, Vector3 to)
    {
        Vector3 segmentDirection = to - from;
        if (segmentDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        segmentDirection.Normalize();
        Vector3 checkpointTangent = new Vector3(-segmentDirection.y, segmentDirection.x, 0f);
        float checkpointHalfWidth = GameplayConstants.CELL_SIZE * 0.5f;
        DrawRouteLine(to - checkpointTangent * checkpointHalfWidth,
                      to + checkpointTangent * checkpointHalfWidth);
    }
}
