using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetSelector : MonoBehaviour
{
    private UnitRuntime owner;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        CacheReferences();
    }

    public bool Initialize(UnitRuntime owner)
    {
        if (owner == null || owner.BattleTeam == null)
        {
            Debug.LogError("[TargetSelector] UnitRuntime with TeamIdentity is required.", this);
            isInitialized = false;
            return false;
        }

        this.owner = owner;
        isInitialized = true;
        return true;
    }

    public Hurtbox SelectTarget(IReadOnlyList<Hurtbox> validTargets, Vector3 origin, TargetPriorityMode priorityMode, UnitAttackType attackType)
    {
        if (validTargets == null || validTargets.Count == 0)
        {
            return null;
        }

        switch (priorityMode)
        {
            case TargetPriorityMode.HighestPathProgress:
                return SelectHighestPathProgress(validTargets, origin, attackType);

            case TargetPriorityMode.LowestHealthPercent:
                return SelectLowestHealthPercent(validTargets, attackType);

            case TargetPriorityMode.AirPriority:
                return SelectAirPriority(validTargets, origin, attackType);

            case TargetPriorityMode.Nearest:
            default:
                return SelectNearest(validTargets, origin, attackType);
        }
    }

    private Hurtbox SelectNearest(IReadOnlyList<Hurtbox> validTargets, Vector3 position, UnitAttackType attackType)
    {
        Hurtbox selectedTarget = null;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < validTargets.Count; i++)
        {
            Hurtbox target = validTargets[i];
            if (target == null || !AttackTargetRulling.CanTarget(attackType, target))
            {
                continue;
            }

            float distance = ((Vector2)position - target.AimPosition).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                selectedTarget = target;
            }
        }

        return selectedTarget;
    }

    private Hurtbox SelectLowestHealthPercent(IReadOnlyList<Hurtbox> validTargets, UnitAttackType attackType)
    {
        Hurtbox selectedTarget = null;
        float lowestHealthPercent = float.PositiveInfinity;

        for (int i = 0; i < validTargets.Count; i++)
        {
            Hurtbox target = validTargets[i];
            if (target == null || !AttackTargetRulling.CanTarget(attackType, target))
            {
                continue;
            }

            IDamageable damageable = target.GetDamageable();
            if (damageable == null || damageable.IsDead || damageable.MaxHealth <= 0f)
            {
                continue;
            }

            float healthPercent = damageable.CurrentHealth / damageable.MaxHealth;
            if (healthPercent < lowestHealthPercent)
            {
                lowestHealthPercent = healthPercent;
                selectedTarget = target;
            }
        }

        return selectedTarget;
    }

    private Hurtbox SelectHighestPathProgress(IReadOnlyList<Hurtbox> validTargets, Vector3 position, UnitAttackType attackType)
    {
        return SelectHighestPathProgress(validTargets, position, attackType, null);
    }

    private Hurtbox SelectAirPriority(IReadOnlyList<Hurtbox> validTargets, Vector3 position, UnitAttackType attackType)
    {
        Hurtbox airTarget = SelectHighestPathProgress(validTargets, position, attackType, UnitMovementType.Flying);
        
        if (airTarget != null)
        {
            return airTarget;
        }

        return SelectHighestPathProgress(validTargets, position, attackType, UnitMovementType.Ground);
    }

    private Hurtbox SelectHighestPathProgress(IReadOnlyList<Hurtbox> validTargets, Vector3 position, UnitAttackType attackType, UnitMovementType? movementTypeFilter = null)
    {
        Hurtbox selectedTarget = null;
        float highestPathProgress = float.NegativeInfinity;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < validTargets.Count; i++)
        {
            Hurtbox target = validTargets[i];
            if (target == null || !AttackTargetRulling.CanTarget(attackType, target))
            {
                continue;
            }

            UnitRuntime targetRuntime = target.OwnerRuntime;
            if (targetRuntime == null)
            {
                continue;
            }

            if (movementTypeFilter.HasValue && targetRuntime.MovementType != movementTypeFilter.Value)
            {
                continue;
            }

            float pathProgress = targetRuntime is EnemyRuntime enemy ? enemy.PathProgressScore : 0f;
            float distance = ((Vector2)position - target.AimPosition).sqrMagnitude;
            if (pathProgress > highestPathProgress || Mathf.Approximately(pathProgress, highestPathProgress) && distance < nearestDistance)
            {
                highestPathProgress = pathProgress;
                nearestDistance = distance;
                selectedTarget = target;
            }
        }

        return selectedTarget;
    }

    public bool TrySelectLockedBlockingTarget(UnitAttackType attackType, out Hurtbox target)
    {
        target = null;

        if (!isInitialized)
        {
            return false;
        }

        if (TrySelectBlockedTarget(attackType, out target))
        {
            return true;
        }

        if (TrySelectBlockingTarget(attackType, out target))
        {
            return true;
        }

        return false;
    }

    private bool TrySelectBlockedTarget(UnitAttackType attackType, out Hurtbox target)
    {
        target = null;

        if (!owner.TryGetComponent(out IBlocker blocker))
        {
            return false;
        }

        if (blocker.BlockedTargets.Count == 0)
        {
            return false;
        }

        IReadOnlyList<IBlockable> blockedTargets = blocker.BlockedTargets;
        for (int i = 0; i < blockedTargets.Count; i++)
        {
            IBlockable blockable = blockedTargets[i];
            if (blockable == null || blockable.Owner == null)
            {
                continue;
            }

            if (TryGetHurtbox(blockable.Owner, attackType, out Hurtbox targetHurtbox))
            {
                target = targetHurtbox;
                return true;
            }
        }

        return false;
    }

    private bool TrySelectBlockingTarget(UnitAttackType attackType, out Hurtbox target)
    {
        target = null;

        if (!owner.TryGetComponent(out IBlockable blockable))
        {
            return false;
        }

        if (!blockable.IsBlocked)
        {
            return false;
        }

        if (blockable.CurrentBlocker == null || blockable.CurrentBlocker.Owner == null)
        {
            return false;
        }

        return TryGetHurtbox(blockable.CurrentBlocker.Owner, attackType, out target);
    }

    private bool TryGetHurtbox(UnitRuntime targetRuntime, UnitAttackType attackType, out Hurtbox targetHurtbox)
    {
        targetHurtbox = null;

        if (targetRuntime == null || targetRuntime.IsDead)
        {
            return false;
        }

        if (!AttackTargetRulling.CanTarget(attackType, targetRuntime))
        {
            return false;
        }

        targetHurtbox = targetRuntime.Hurtbox;
        if (targetHurtbox == null)
        {
            return false;
        }

        IDamageable damageable = targetHurtbox.GetDamageable();
        if (damageable == null || damageable.IsDead)
        {
            targetHurtbox = null;
            return false;
        }

        TeamIdentity targetTeam = targetHurtbox.GetTargetTeam();
        if (!owner.BattleTeam.IsEnemy(targetTeam))
        {
            targetHurtbox = null;
            return false;
        }

        return true;
    }

    private void CacheReferences()
    {
        if (owner == null)
        {
            owner = GetComponent<UnitRuntime>();
        }
    }
}
