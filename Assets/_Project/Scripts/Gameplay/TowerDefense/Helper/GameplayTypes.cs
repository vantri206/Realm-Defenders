using System;
using UnityEngine;

public enum UnitAttackType
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
    LowestHealth,
    RangedPriority
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

public enum EightWayDirection
{
    Left,
    Right,
    Down,
    Up,
    DownLeft,
    DownRight,
    UpLeft,
    UpRight,
}

public static class GridDirectionHelpers
{
    public const int CardinalDirectionCount = 4;
    public const int EightWayDirectionCount = 8;

    public static readonly Vector2Int[] EightWayOffsets =
    {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.up,
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, 1),
    };

    public static Vector2Int ToVector2Int(this EightWayDirection direction)
    {
        return EightWayOffsets[(int)direction];
    }
}

public enum RouteCheckpointType
{
    Spawn,
    Checkpoint,
    End,
}

public enum RouteCheckpointDirection
{
    Left,
    Right,
    Up,
    Down,
}

public enum PathfindingMode
{
    None,
    FlowField,
    BFS
}

public enum SearchNodeState
{
    Unvisited,
    Open,
    Closed
}

public enum EnemySpawnEventStartCondition
{
    AfterDelay,
    AfterSpawnEventFinished,
}

public enum EnemySpawnEventState
{
    Waiting,
    Spawning,
    Finished,
    Resolved,
}