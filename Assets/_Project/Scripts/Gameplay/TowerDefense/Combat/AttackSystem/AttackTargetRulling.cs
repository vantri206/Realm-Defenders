using UnityEngine;

public static class AttackTargetRulling
{
    public static bool CanTarget(UnitRuntime attacker, Hurtbox target)
    {
        if (attacker == null || target == null)
        {
            return false;
        }

        return CanHit(attacker.AttackType, target);
    }

    public static bool CanTarget(UnitRuntime attacker, UnitRuntime target)
    {
        if (attacker == null || target == null)
        {
            return false;
        }

        return CanAttackMovementType(attacker.AttackType, target.MovementType);
    }

    public static bool CanHit(UnitAttackType attackType, Hurtbox target)
    {
        if (target == null)
        {
            return false;
        }

        UnitRuntime targetRuntime = target.OwnerRuntime;
        if (targetRuntime == null)
        {
            return false;
        }

        return CanAttackMovementType(attackType, targetRuntime.MovementType);
    }

    private static bool CanAttackMovementType(UnitAttackType attackType, UnitMovementType targetMovementType)
    {
        return (attackType == UnitAttackType.Ranged || targetMovementType == UnitMovementType.Ground && attackType == UnitAttackType.Melee);
    }
}
