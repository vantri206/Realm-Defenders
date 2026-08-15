using System.Collections.Generic;

public interface IBlockable
{
    bool CanBeBlocked { get; }
    bool IsBlocked { get; }
    UnitRuntime Owner { get; }
    IBlocker CurrentBlocker { get; }

    void OnBlocked(IBlocker blocker);
    void ClearBlocked(IBlocker blocker);
}

public interface IBlocker
{
    bool CanBlock { get; }
    int MaxBlockCount { get; }
    int CurrentBlockCount { get; }
    UnitRuntime Owner { get; }
    IReadOnlyList<IBlockable> BlockedTargets { get; }

    bool CanBlockTarget(IBlockable target);
    void ReleaseBlockedTarget(IBlockable target);
}
