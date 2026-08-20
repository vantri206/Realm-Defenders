using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroBlocker : MonoBehaviour, IBlocker
{
    private UnitRuntime owner;
    private UnitBlock block;

    private readonly List<IBlockable> blockedTargets = new List<IBlockable>();
    private readonly Dictionary<IBlockable, int> blockSlotAssignments = new Dictionary<IBlockable, int>();
    private readonly bool[] occupiedBlockSlots = new bool[BlockSpacingResolver.SlotCount];

    public bool CanBlock => !owner.IsDead && owner.ActiveCell != null && block != null && MaxBlockCount > 0;
    public int MaxBlockCount => block != null ? block.BlockCount : 0;
    public int CurrentBlockCount => blockedTargets.Count;
    public UnitRuntime Owner => owner;
    public IReadOnlyList<IBlockable> BlockedTargets => blockedTargets;

    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(UnitRuntime ownerRuntime, UnitBlock unitBlock)
    {
        owner = ownerRuntime;
        block = unitBlock;
        UpdateBlockCount();
    }

    private void OnDisable()
    {
        ClearBlocks();
    }

    public void FixedTick()
    {
        if (!CanBlock)
        {
            ClearBlocks();
            return;
        }

        ReleaseInvalidBlockedTargets();
        TryBlockTargetsInActiveCell();
        UpdateBlockCount();
    }

    public void ClearBlocks()
    {
        for (int i = blockedTargets.Count - 1; i >= 0; i--)
        {
            IBlockable blockable = blockedTargets[i];
            if (blockable != null)
            {
                ReleaseBlockSlot(blockable);
                blockable.ClearBlocked(this);
            }
        }

        blockedTargets.Clear();
        ClearBlockSlotAssignments();
        UpdateBlockCount();
    }

    public void ReleaseBlockedTarget(IBlockable target)
    {
        if (target == null)
        {
            return;
        }

        if (!blockedTargets.Remove(target))
        {
            return;
        }

        ReleaseBlockSlot(target);
        target.ClearBlocked(this);
        UpdateBlockCount();
    }

    private void TryBlockTargetsInActiveCell()
    {
        CombatGridCell activeCell = owner.ActiveCell;
        IReadOnlyList<UnitRuntime> units = activeCell.Units;

        for (int i = 0; i < units.Count && blockedTargets.Count < MaxBlockCount; i++)
        {
            if (units[i] is not IBlockable blockable)
            {
                continue;
            }

            if (!CanBlockTarget(blockable))
            {
                continue;
            }

            bool wasNotBlocking = blockedTargets.Count == 0;
            bool hasBlockSlot = BlockSpacingResolver.TryResolveBlockSlot(owner, blockable, occupiedBlockSlots,
                                                                        out int blockSlotIndex);

            blockedTargets.Add(blockable);
            blockable.OnBlocked(this);

            if (hasBlockSlot)
            {
                blockSlotAssignments.Add(blockable, blockSlotIndex);
                occupiedBlockSlots[blockSlotIndex] = true;
                BlockSpacingResolver.ApplyBlockSlot(owner, blockable, blockSlotIndex);
            }

            if (wasNotBlocking)
            {
                owner.FacePosition(blockable.Owner.CenterPosition);
            }
        }
    }

    private void ReleaseInvalidBlockedTargets()
    {
        for (int i = blockedTargets.Count - 1; i >= 0; i--)
        {
            IBlockable blockable = blockedTargets[i];
            if (IsValidBlockedTarget(blockable))
            {
                continue;
            }

            blockedTargets.RemoveAt(i);
            ReleaseBlockSlot(blockable);
            if (blockable != null)
            {
                blockable.ClearBlocked(this);
            }
        }
    }

    private void ReleaseBlockSlot(IBlockable target)
    {
        BlockSpacingResolver.ClearBlockSlot(target);
        if (target == null || !blockSlotAssignments.TryGetValue(target, out int slotIndex))
        {
            return;
        }

        blockSlotAssignments.Remove(target);
        occupiedBlockSlots[slotIndex] = false;
    }

    private void ClearBlockSlotAssignments()
    {
        blockSlotAssignments.Clear();
        for (int i = 0; i < occupiedBlockSlots.Length; i++)
        {
            occupiedBlockSlots[i] = false;
        }
    }

    public bool CanBlockTarget(IBlockable target)
    {
        if (!CanBlock || target == null || !target.CanBeBlocked || target.Owner == null)
        {
            return false;
        }

        UnitRuntime targetOwner = target.Owner;
        if (blockedTargets.Contains(target) || targetOwner == owner || targetOwner.ActiveCell != owner.ActiveCell)
        {
            return false;
        }

        if (target.CurrentBlocker != null)
        {
            return false;
        }

        if (owner.TeamIdentity == null || targetOwner.TeamIdentity == null)
        {
            return false;
        }

        return owner.TeamIdentity.IsEnemy(targetOwner.TeamIdentity);
    }

    private bool IsValidBlockedTarget(IBlockable target)
    {
        return target != null && target.CanBeBlocked && ReferenceEquals(target.CurrentBlocker, this);        
    }

    private void UpdateBlockCount()
    {
        if (block == null)
        {
            return;
        }

        block.ResetBlock();
        block.IncreaseBlock(blockedTargets.Count);
    }

    private void CacheReferences()
    {
        if (owner == null)
        {
            owner = GetComponent<UnitRuntime>();
        }
    }
}
