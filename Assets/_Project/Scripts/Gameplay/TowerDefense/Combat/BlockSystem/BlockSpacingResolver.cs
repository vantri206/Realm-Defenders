using System.Collections.Generic;
using UnityEngine;

public static class BlockSpacingResolver
{
    public const int SlotCount = 8;

    private const float slotRadiusInCells = 0.8f;

    private static readonly Vector2[] slotDirections =
    {
        EightWayDirection.Right.ToVector2Int(),
        EightWayDirection.UpRight.ToVector2Int(),
        EightWayDirection.Up.ToVector2Int(),
        EightWayDirection.UpLeft.ToVector2Int(),
        EightWayDirection.Left.ToVector2Int(),
        EightWayDirection.DownLeft.ToVector2Int(),
        EightWayDirection.Down.ToVector2Int(),
        EightWayDirection.DownRight.ToVector2Int(),
    };

    public static bool TryResolveBlockSlot(UnitRuntime blockerOwner, IBlockable target, IReadOnlyList<bool> slots, out int slotIndex)
    {
        slotIndex = -1;
        if (blockerOwner == null || target == null || target.Owner == null || slots == null || slots.Count < SlotCount)
        {
            return false;
        }

        int mainSlotIndex = ResolveDirectionIndex(ResolveBlockDirection(blockerOwner, target.Owner));
        int nextSlotIndex = WrapSlotIndex(mainSlotIndex + 1);
        int prevSlotIndex = WrapSlotIndex(mainSlotIndex - 1);

        if (!slots[mainSlotIndex])
        {
            slotIndex = mainSlotIndex;
            return true;
        }

        bool isNextAvailable = !slots[nextSlotIndex];
        bool isPrevAvailable = !slots[prevSlotIndex];
        if (!isNextAvailable && !isPrevAvailable)
        {
            return false;
        }

        if (isNextAvailable && isPrevAvailable)
        {
            Vector2 targetPosition = target.Owner.WorldPosition;
            Vector2 nextPosition = ResolveBlockSlotWorldPosition(blockerOwner, nextSlotIndex);
            Vector2 prevPosition = ResolveBlockSlotWorldPosition(blockerOwner, prevSlotIndex);
            slotIndex = (targetPosition - nextPosition).sqrMagnitude <= (targetPosition - prevPosition).sqrMagnitude ? nextSlotIndex : prevSlotIndex;
            return true;
        }

        slotIndex = isNextAvailable ? nextSlotIndex : prevSlotIndex;
        return true;
    }

    public static void ApplyBlockSlot(UnitRuntime blockerOwner, IBlockable target, int slotIndex)
    {
        if (blockerOwner == null || target == null || target.Owner == null ||
            slotIndex < 0 || slotIndex >= SlotCount)
        {
            return;
        }

        if (!target.Owner.TryGetComponent(out UnitMovement movement))
        {
            Debug.LogError("[BlockSpacingResolver] UnitMovement is required to align a blocked target.", target.Owner);
            return;
        }

        movement.SetMovementOverride(ResolveBlockSlotWorldPosition(blockerOwner, slotIndex));
    }

    public static void ClearBlockSlot(IBlockable target)
    {
        if (target?.Owner != null && target.Owner.TryGetComponent(out UnitMovement movement))
        {
            movement.ClearMovementOverride();
        }
    }

    public static Vector2 ResolveBlockSlotWorldPosition(UnitRuntime blockerOwner, int slotIndex)
    {
        int resolvedSlotIndex = WrapSlotIndex(slotIndex);
        Vector3 cellSize = blockerOwner.CombatGrid.CellSize;
        float cellScale = Mathf.Min(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y));
        if (cellScale <= Mathf.Epsilon)
        {
            cellScale = 1f;
        }

        return (Vector2)blockerOwner.WorldPosition + slotDirections[resolvedSlotIndex] * (cellScale * slotRadiusInCells);
    }

    private static Vector2 ResolveBlockDirection(UnitRuntime blockerOwner, UnitRuntime targetOwner)
    {
        if (targetOwner.Movement != null && targetOwner.Movement.CurrentMoveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            return -targetOwner.Movement.CurrentMoveDirection.normalized;
        }

        Vector2 offset = (Vector2)targetOwner.WorldPosition - (Vector2)blockerOwner.WorldPosition;
        if (offset.sqrMagnitude > Mathf.Epsilon)
        {
            return offset.normalized;
        }

        Vector2 targetFacingDirection = targetOwner.FacingDirection;
        if (targetFacingDirection.sqrMagnitude > Mathf.Epsilon)
        {
            return -targetFacingDirection.normalized;
        }

        Vector2 blockerFacingDirection = blockerOwner.FacingDirection;
        return blockerFacingDirection.sqrMagnitude > Mathf.Epsilon ? blockerFacingDirection.normalized : Vector2.right;
    }

    private static int ResolveDirectionIndex(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return WrapSlotIndex(Mathf.RoundToInt(angle / 45f));
    }

    private static int WrapSlotIndex(int slotIndex)
    {
        return (slotIndex % SlotCount + SlotCount) % SlotCount;
    }
}
