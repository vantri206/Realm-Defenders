using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitSeparationResolver : MonoBehaviour
{
    [SerializeField] private UnitSeparationSettings defaultSettings = new UnitSeparationSettings();

    private readonly List<UnitRuntime> nearbyUnits = new List<UnitRuntime>();

    public Vector2 GetSeparationDirection(UnitRuntime self, CombatGrid combatGrid)
    {
        return GetSeparationDirection(self, combatGrid, defaultSettings);
    }

    public Vector2 GetSeparationDirection(UnitRuntime self, CombatGrid combatGrid, UnitSeparationSettings settings)
    {
        if (settings == null)
        {
            settings = defaultSettings;
        }

        if (combatGrid == null || self == null || !self.IsInitialized || self.ActiveCell == null)
        {
            return Vector2.zero;
        }

        IReadOnlyList<UnitRuntime> nearby = CollectNearby(combatGrid, self.ActiveCellPosition, settings.CellSearchRadius);
        return ResolveDirection(self, nearby, settings);
    }
    
    public Vector2 ResolveDirection(UnitRuntime self, IReadOnlyList<UnitRuntime> nearby, UnitSeparationSettings settings)
    {
        if (self == null || nearby == null)
        {
            return Vector2.zero;
        }

        if (settings == null)
        {
            return Vector2.zero;
        }

        TeamIdentity selfTeam = self.BattleTeam;
        if (selfTeam == null)
        {
            return Vector2.zero;
        }

        float radius = Mathf.Max(0f, settings.Radius);
        float radiusSqr = radius * radius;
        
        Vector2 selfCenter = self.CenterPosition;
        Vector2 separation = Vector2.zero;

        for (int i = 0; i < nearby.Count; i++)
        {
            UnitRuntime other = nearby[i];
            if (other == null || other == self || !other.IsInitialized)
            {
                continue;
            }

            if (other.MovementType != self.MovementType)
            {
                continue;
            }

            TeamIdentity otherTeam = other.BattleTeam;
            if (otherTeam == null || otherTeam.Team != selfTeam.Team)
            {
                continue;
            }

            Vector2 otherCenter = other.CenterPosition;
            Vector2 offset = selfCenter - otherCenter;
            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr > radiusSqr)
            {
                continue;
            }

            if (distanceSqr <= Mathf.Epsilon)  // distanceSqr = 0 -> avoid division by zero and apply random separation direction
            {
                int directionSign = Random.value < 0.5f ? -1 : 1; // random direction 1 or -1
                separation += new Vector2(directionSign, 0f);
                continue;
            }

            float distance = Mathf.Sqrt(distanceSqr);
            float strength = 1f - Mathf.Clamp01(distance / radius);
            separation += offset / distance * strength;
        }

        if (separation.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        float maxForce = Mathf.Max(0f, settings.MaxForce);
        if (maxForce > 0f && separation.sqrMagnitude > maxForce * maxForce)
        {
            separation = separation.normalized * maxForce;
        }

        return separation;
    }

    public IReadOnlyList<UnitRuntime> CollectNearby(CombatGrid combatGrid, Vector3Int centerCellPosition, int cellSearchRange)
    {
        nearbyUnits.Clear();
        if (combatGrid == null)
        {
            return nearbyUnits;
        }

        int searchRange = Mathf.Max(0, cellSearchRange);
        for (int x = -searchRange; x <= searchRange; x++)
        {
            for (int y = -searchRange; y <= searchRange; y++)
            {
                Vector3Int cellPosition = centerCellPosition + new Vector3Int(x, y, 0);
                if (!combatGrid.TryGetCell(cellPosition, out CombatGridCell cell) || cell == null)
                {
                    continue;
                }

                IReadOnlyList<UnitRuntime> cellUnits = cell.Units;
                for (int i = 0; i < cellUnits.Count; i++)
                {
                    UnitRuntime unit = cellUnits[i];
                    if (unit != null && !nearbyUnits.Contains(unit))
                    {
                        nearbyUnits.Add(unit);
                    }
                }
            }
        }

        return nearbyUnits;
    }

    public Vector2 ApplyWeight(Vector2 separationDirection, UnitSeparationSettings settings)
    {
        if (settings == null)
        {
            settings = defaultSettings;
        }

        if (settings == null || separationDirection == Vector2.zero)
        {
            return Vector2.zero;
        }

        float weight = Mathf.Max(0f, settings.Weight);
        if (weight <= Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        return separationDirection * weight;
    }
}
