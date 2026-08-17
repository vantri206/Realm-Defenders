using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetSelector : MonoBehaviour
{
    private UnitRuntime owner;

    private TargetPriorityMode priorityMode = TargetPriorityMode.Nearest;

    public TargetPriorityMode PriorityMode => priorityMode;

    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(UnitRuntime owner, TargetPriorityMode priorityMode)
    {
        this.owner = owner;
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
                return SelectNearest(validTargets, origin);

            case TargetPriorityMode.LowestHealthPercent:
                return SelectLowestHealthPercent(validTargets);

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
            if (target == null || !AttackTargetRulling.CanTarget(owner, target))
            {
                continue;
            }

            float distance = ((Vector2)position - target.CenterPosition).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                selectedTarget = target;
            }
        }

        return selectedTarget;
    }

    private Hurtbox SelectLowestHealthPercent(IReadOnlyList<Hurtbox> validTargets)
    {
        Hurtbox selectedTarget = null;
        float lowestHealthPercent = float.PositiveInfinity;

        for (int i = 0; i < validTargets.Count; i++)
        {
            Hurtbox target = validTargets[i];
            if (target == null || !AttackTargetRulling.CanTarget(owner, target))
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

    private Hurtbox SelectHighestPathProgress(IReadOnlyList<Hurtbox> validTargets)
    {
        return SelectNearest(validTargets, owner.CenterPosition);
    }

    public bool TrySelectLockedBlockingTarget(out Hurtbox target)
    {
        target = null;
        CacheReferences();

        if (owner == null)
        {
            return false;
        }

        if (TrySelectBlockedTarget(out target))
        {
            return true;
        }

        if (TrySelectBlockingTarget(out target))
        {
            return true;
        }

        return false;
    }

    private bool TrySelectBlockedTarget(out Hurtbox target)
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

            if (TryGetHurtbox(blockable.Owner, out target))
            {
                return true;
            }
        }

        return false;
    }

    private bool TrySelectBlockingTarget(out Hurtbox target)
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

        return TryGetHurtbox(blockable.CurrentBlocker.Owner, out target);
    }

    private bool TryGetHurtbox(UnitRuntime runtime, out Hurtbox target)
    {
        target = null;

        if (runtime == null || runtime.IsDead)
        {
            return false;
        }

        target = runtime.GetComponentInChildren<Hurtbox>();
        if (target == null)
        {
            return false;
        }

        if (!AttackTargetRulling.CanTarget(owner, target))
        {
            target = null;
            return false;
        }

        IDamageable damageable = target.GetDamageable();
        if (damageable == null || damageable.IsDead)
        {
            target = null;
            return false;
        }

        TeamIdentity targetTeam = target.GetTargetTeam();
        if (owner.TeamIdentity != null && !owner.TeamIdentity.IsEnemy(targetTeam))
        {
            target = null;
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
