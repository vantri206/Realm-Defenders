using System.Collections.Generic;
using UnityEngine;

public class UnitStatusRuntime
{
    private readonly List<StunStatus> stunStatuses = new List<StunStatus>();
    private readonly List<PoisonStatus> poisonStatuses = new List<PoisonStatus>();
    private readonly List<TemporaryStatModifierStatus> temporaryStatStatuses = new List<TemporaryStatModifierStatus>();
    private readonly List<DefenseReductionStatus> defenseReductionStatuses = new List<DefenseReductionStatus>();
    private readonly UnitRuntime owner;

    private bool isTicking;
    private bool isClearing;

    public bool IsStunned => stunStatuses.Count > 0;
    public bool IsPoisoned => poisonStatuses.Count > 0;

    public UnitStatusRuntime(UnitRuntime owner)
    {
        this.owner = owner;
    }

    public bool ApplyStun(string statusId, GameObject source, float duration)
    {
        if (!CanApplyStatus(statusId, source) || duration <= 0f)
        {
            return false;
        }

        StatusKey key = CreateStatusKey(statusId, source);
        StunStatus stunStatus = FindStunStatus(key);
        if (stunStatus != null)
        {
            stunStatus.Refresh(duration);
        }
        else
        {
            stunStatuses.Add(new StunStatus(key, duration));
        }

        return true;
    }

    public bool ApplyPoison(string statusId, GameObject attacker, float damagePerTick, float duration, float tickInterval, int maxStackCount)
    {
        if (!CanApplyStatus(statusId, attacker) || damagePerTick <= 0f || duration <= 0f || tickInterval <= 0f || maxStackCount <= 0)
        {
            return false;
        }

        StatusKey key = CreateStatusKey(statusId, attacker);
        PoisonStatus poisonStatus = FindPoisonStatus(key);
        if (poisonStatus == null)
        {
            poisonStatus = new PoisonStatus(key, owner, maxStackCount);
            poisonStatuses.Add(poisonStatus);
        }

        poisonStatus.AddStack(attacker, damagePerTick, duration, tickInterval, maxStackCount);
        return true;
    }

    public bool ApplyTemporaryStatModifiers(string statusId, GameObject source, IReadOnlyList<UnitStatModifier> modifiers, float duration)
    {
        if (!CanApplyStatus(statusId, source) || owner.Stats == null || modifiers == null || modifiers.Count == 0 || duration <= 0f)
        {
            return false;
        }

        StatusKey key = CreateStatusKey(statusId, source);
        TemporaryStatModifierStatus modifierStatus = FindStatStatus(key);
        if (modifierStatus != null)
        {
            modifierStatus.Refresh(duration);
            return true;
        }

        temporaryStatStatuses.Add(new TemporaryStatModifierStatus(key, owner.Stats, modifiers, duration));
        return true;
    }

    public bool ApplyDefenseReduction(string statusId, GameObject source, float defenseReduction, float duration,
                                      int maxStackCount, string modifierId)
    {
        if (!CanApplyStatus(statusId, source) || owner.Stats == null || defenseReduction <= 0f || duration <= 0f ||
            maxStackCount <= 0 || string.IsNullOrWhiteSpace(modifierId))
        {
            return false;
        }

        StatusKey key = CreateStatusKey(statusId, source);
        DefenseReductionStatus reductionStatus = FindDefenseReductionStatus(key);
        if (reductionStatus == null)
        {
            reductionStatus = new DefenseReductionStatus(key, owner.Stats, maxStackCount);
            defenseReductionStatuses.Add(reductionStatus);
        }

        reductionStatus.AddStack(defenseReduction, duration, maxStackCount, modifierId);
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || owner == null)
        {
            return;
        }

        isTicking = true;

        TickStunStatuses(deltaTime);
        TickPoisonStatuses(deltaTime);
        TickStatStatuses(deltaTime);
        TickDefenseReductionStatuses(deltaTime);

        isTicking = false;

        if (owner.IsDead)
        {
            isClearing = true;
        }

        if (isClearing)
        {
            ClearAllStatuses();
        }
    }

    public void Clear()
    {
        if (isTicking)
        {
            isClearing = true;
            return;
        }

        ClearAllStatuses();
    }

    private void TickStunStatuses(float deltaTime)
    {
        for (int i = stunStatuses.Count - 1; i >= 0; i--)
        {
            StunStatus stunStatus = stunStatuses[i];
            stunStatus.Tick(deltaTime);
            if (!stunStatus.IsActive)
            {
                stunStatuses.RemoveAt(i);
            }
        }
    }

    private void TickPoisonStatuses(float deltaTime)
    {
        for (int i = poisonStatuses.Count - 1; i >= 0; i--)
        {
            PoisonStatus poisonStatus = poisonStatuses[i];
            poisonStatus.Tick(deltaTime);
            if (!poisonStatus.IsActive)
            {
                poisonStatuses.RemoveAt(i);
            }

            if (owner.IsDead)
            {
                isClearing = true;
                return;
            }
        }
    }

    private void TickStatStatuses(float deltaTime)
    {
        for (int i = temporaryStatStatuses.Count - 1; i >= 0; i--)
        {
            TemporaryStatModifierStatus statStatus = temporaryStatStatuses[i];
            statStatus.Tick(deltaTime);
            if (!statStatus.IsActive)
            {
                temporaryStatStatuses.RemoveAt(i);
            }
        }
    }

    private void TickDefenseReductionStatuses(float deltaTime)
    {
        for (int i = defenseReductionStatuses.Count - 1; i >= 0; i--)
        {
            DefenseReductionStatus reductionStatus = defenseReductionStatuses[i];
            reductionStatus.Tick(deltaTime);
            if (!reductionStatus.IsActive)
            {
                defenseReductionStatuses.RemoveAt(i);
            }
        }
    }

    private StunStatus FindStunStatus(StatusKey key)
    {
        for (int i = 0; i < stunStatuses.Count; i++)
        {
            StunStatus stunStatus = stunStatuses[i];
            if (stunStatus.Key.IsStatus(key))
            {
                return stunStatus;
            }
        }

        return null;
    }

    private PoisonStatus FindPoisonStatus(StatusKey key)
    {
        for (int i = 0; i < poisonStatuses.Count; i++)
        {
            PoisonStatus poisonStatus = poisonStatuses[i];
            if (poisonStatus.Key.IsStatus(key))
            {
                return poisonStatus;
            }
        }

        return null;
    }

    private TemporaryStatModifierStatus FindStatStatus(StatusKey key)
    {
        for (int i = 0; i < temporaryStatStatuses.Count; i++)
        {
            TemporaryStatModifierStatus statStatus = temporaryStatStatuses[i];
            if (statStatus.Key.IsStatus(key))
            {
                return statStatus;
            }
        }

        return null;
    }

    private DefenseReductionStatus FindDefenseReductionStatus(StatusKey key)
    {
        for (int i = 0; i < defenseReductionStatuses.Count; i++)
        {
            DefenseReductionStatus reductionStatus = defenseReductionStatuses[i];
            if (reductionStatus.Key.IsStatus(key))
            {
                return reductionStatus;
            }
        }

        return null;
    }

    private bool CanApplyStatus(string statusId, GameObject source)
    {
        return owner != null && !owner.IsDead && !string.IsNullOrWhiteSpace(statusId) && source != null;
    }

    private static StatusKey CreateStatusKey(string statusId, GameObject source)
    {
        return new StatusKey(statusId, source.GetInstanceID());
    }

    private void ClearAllStatuses()
    {
        for (int i = 0; i < poisonStatuses.Count; i++)
        {
            poisonStatuses[i].Clear();
        }

        for (int i = 0; i < temporaryStatStatuses.Count; i++)
        {
            temporaryStatStatuses[i].Clear();
        }

        for (int i = 0; i < defenseReductionStatuses.Count; i++)
        {
            defenseReductionStatuses[i].Clear();
        }

        stunStatuses.Clear();
        poisonStatuses.Clear();
        temporaryStatStatuses.Clear();
        defenseReductionStatuses.Clear();
        isClearing = false;
    }
}
