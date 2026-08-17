using UnityEngine;

public static class BlockSpacingResolver
{
    private const float pushDistance = 0.5f;
    private const float pushDuration = 0.25f;

    public static void ApplyBlockForce(UnitRuntime blockerOwner, IBlockable target)
    {
        if (blockerOwner == null || target == null || target.Owner == null)
        {
            return;
        }

        UnitRuntime targetOwner = target.Owner;
        if (!targetOwner.TryGetComponent(out UnitMovement movement))
        {
            Debug.LogError("[BlockSpacingResolver] UnitMovement is required to apply block force.", targetOwner);
            return;
        }

        Vector2 direction = ResolvePushDirection(blockerOwner, targetOwner, movement);
        if (direction == Vector2.zero)
        {
            return;
        }

        movement.ApplyForce(direction, pushDistance, pushDuration);
    }

    private static Vector2 ResolvePushDirection(UnitRuntime blockerOwner, UnitRuntime targetOwner, UnitMovement movement)
    {
        Vector2 moveDirection = movement.CurrentMoveDirection;
        if (moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            return -moveDirection.normalized;
        }

        Vector2 offset = (Vector2)targetOwner.CenterPosition - (Vector2)blockerOwner.CenterPosition;
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
        return blockerFacingDirection.sqrMagnitude > Mathf.Epsilon ? -blockerFacingDirection.normalized : Vector2.right;
    }
}
