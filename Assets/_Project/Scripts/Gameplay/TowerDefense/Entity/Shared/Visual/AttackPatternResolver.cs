using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AttackPatternResolver
{

    public static List<Vector2Int> RefreshAttackPattern(IReadOnlyList<Vector2Int> defaultAttackPattern, Vector2Int facingDirection)
    {
        List<Vector2Int> resolvedAttackPattern = new List<Vector2Int>();

        if (defaultAttackPattern == null)
        {
            return resolvedAttackPattern;
        }

        IReadOnlyList<Vector2Int> patternOffsets = defaultAttackPattern.ToList();
        List<Vector2Int> rotatedPattern = RotateAttackPattern(patternOffsets, facingDirection);
        for (int i = 0; i < rotatedPattern.Count; i++)
        {
            resolvedAttackPattern.Add(rotatedPattern[i]);
        }

        return resolvedAttackPattern;
    }

    private static List<Vector2Int> RotateAttackPattern(IReadOnlyList<Vector2Int> attackPattern, Vector2Int direction)
    {
        List<Vector2Int> rotatedPattern = new List<Vector2Int>();

        if (attackPattern == null)
        {
            return rotatedPattern;
        }

        for (int i = 0; i < attackPattern.Count; i++)
        {
            Vector2Int offset = attackPattern[i];
            Vector2Int rotatedOffset = offset;

            if (direction == Vector2Int.right)
            {
                rotatedOffset = new Vector2Int(-offset.x, -offset.y);
            }
            else if (direction == Vector2Int.up)
            {
                rotatedOffset = new Vector2Int(offset.y, -offset.x);
            }
            else if (direction == Vector2Int.down)
            {
                rotatedOffset = new Vector2Int(-offset.y, offset.x);
            }

            rotatedPattern.Add(rotatedOffset);
        }

        return rotatedPattern;
    }

}
