using System;
using UnityEngine;

public enum UnitMovementType
{
    Ground,
    Flying,
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
    LowestHealthPercent,
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
    SelectedDeployedHero
}

public enum HeroDeployState
{
    Available,
    Unavailable,
    Countdown,
    Deployed,
}

public enum HeroBlockState
{
    NonBlocking,
    Blocking,
}

public enum HeroActionType
{
    None,
    Retreat,
    Skill,
    Upgrade,
}

public enum UnitRuntimeState
{
    Idle,
    Moving,
    Attacking,
    Dead,
    SkillCasting
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
        Vector2Int.right,        // 0°
        new Vector2Int(1, 1),    // 45°
        Vector2Int.up,           // 90°
        new Vector2Int(-1, 1),   // 135°
        Vector2Int.left,         // 180°
        new Vector2Int(-1, -1),  // 225°
        Vector2Int.down,         // 270°
        new Vector2Int(1, -1),   // 315°
    };

    public static readonly EightWayDirection[] DirectionsByAngle =
    {
        EightWayDirection.Right,
        EightWayDirection.UpRight,
        EightWayDirection.Up,
        EightWayDirection.UpLeft,
        EightWayDirection.Left,
        EightWayDirection.DownLeft,
        EightWayDirection.Down,
        EightWayDirection.DownRight,
    };

    public static Vector2Int ToVector2Int(this EightWayDirection direction)
    {
        return EightWayOffsets[direction.GetAngleIndex()];
    }

    public static EightWayDirection FromVector2(Vector2 vector)
    {
        if (vector.sqrMagnitude <= Mathf.Epsilon)
        {
            return EightWayDirection.Right;
        }

        float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        int index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % EightWayDirectionCount;

        return DirectionsByAngle[index];
    }

    public static EightWayDirection Rotate45(this EightWayDirection direction, int stepCount)
    {
        int currentIndex = GetAngleIndex(direction);
        int nextIndex = WrapAngleIndex(currentIndex + stepCount);

        return DirectionsByAngle[nextIndex];
    }

    public static int GetAngleIndex(this EightWayDirection direction)
    {
        return direction switch
        {
            EightWayDirection.Right     => 0,
            EightWayDirection.UpRight   => 1,
            EightWayDirection.Up        => 2,
            EightWayDirection.UpLeft    => 3,
            EightWayDirection.Left      => 4,
            EightWayDirection.DownLeft  => 5,
            EightWayDirection.Down      => 6,
            EightWayDirection.DownRight => 7,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    public static int WrapAngleIndex(int index)
    {
        index %= EightWayDirectionCount;

        if (index < 0)
        {
            index += EightWayDirectionCount;
        }

        return index;
    }


    public static bool IsDiagonalMove(Vector2Int offset)
    {
        return Mathf.Abs(offset.x) == 1 && Mathf.Abs(offset.y) == 1;
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

public enum UnitAttackType
{
    Melee,
    Ranged,
}

public enum AttackDamageType
{
    PhysicalDamage,
    MagicalDamage,
    TrueDamage,
}

public enum AttackMethod
{
    DirectTarget,
    Projectile,
    AOEHit,
}

public enum ProjectileMode
{
    Linear,
    Chase,
}

public enum TargetSide
{
    Enemy,
    Ally,
}

public enum AttackEffect
{
    Damage,
    Heal,
}

public enum EnemyResolveReason
{
    Killed,
    Escaped,
}

public enum SkillType
{
    Active,
    Passive,
}

public enum SkillTargetType
{
    Self,
    Ally,
    Enemy,
    Area,
}

public enum GearType
{
    Weapon,
    Armor,
}

public enum HeroRarity
{
    Normal,
    Special,
}

public enum GearRarity
{
    Common,
    Rare,
    Epic,
}