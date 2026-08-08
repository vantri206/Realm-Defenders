using System;
using UnityEngine;

public enum HeroAttackType
{
    Melee,
    Ranged,
}

public enum Team
{
    Environment,    // Neutral entities can damage all other factions, and only be damaged by player
    Player,
    Enemy
}

public enum TargetPriorityMode
{
    Nearest,
    HighestPathProgress,
}

[Flags]
public enum CombatGridCellStates
{
    None = 0,
    Walkable = 1 << 0,
    Deployable = 1 << 1,
    Blocked = 1 << 2,
}

public enum PlayerCombatActionMode
{
    None,
    DeployingHero,
    SelectingDeployDirection,
    SelectedDeployedHero,
    RelocatingHero,
}

public enum HeroDeployState
{
    Available,
    Unavailable,
    Countdown,
    Deployed,
}

public enum GridDirection
{
    Left,
    Right,
    Up,
    Down,
}

public static class GridDirectionHelpers
{
    public static Vector2Int ToVector2Int(this GridDirection direction)
    {
        switch (direction)
        {
            case GridDirection.Left:
                return Vector2Int.left;
            case GridDirection.Right:
                return Vector2Int.right;
            case GridDirection.Up:
                return Vector2Int.up;
            case GridDirection.Down:
                return Vector2Int.down;
            default:
                return Vector2Int.zero;
        }
    }
}
