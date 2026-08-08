using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetSelector : MonoBehaviour
{
    private TargetPriorityMode priorityMode = TargetPriorityMode.Nearest;

    public TargetPriorityMode PriorityMode => priorityMode;

    public void Initialize(TargetPriorityMode priorityMode)
    {
        this.priorityMode = priorityMode;
    }

    public Hurtbox SelectTarget(IReadOnlyList<Hurtbox> validTargets, Vector3 origin)
    {
        if (validTargets == null || validTargets.Count == 0)
        {
            return null;
        }

        switch (priorityMode)
        {
            case TargetPriorityMode.HighestPathProgress:
                return SelectHighestPathProgress(validTargets, origin);

            case TargetPriorityMode.Nearest:
            default:
                return SelectNearest(validTargets, origin);
        }
    }

    private Hurtbox SelectNearest(IReadOnlyList<Hurtbox> validTargets, Vector3 position)
    {
        Hurtbox selectedTarget = null;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < validTargets.Count; i++)
        {
            Hurtbox target = validTargets[i];
            if (target == null)
            {
                continue;
            }

            float distance = ((Vector2)position - target.Position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                selectedTarget = target;
            }
        }

        return selectedTarget;
    }

    private Hurtbox SelectHighestPathProgress(IReadOnlyList<Hurtbox> candidates, Vector3 fallbackPosition)
    {
        // TODO: Select by enemy route progress once EnemyRuntime/path progress data exists.
        return SelectNearest(candidates, fallbackPosition);
    }
}
