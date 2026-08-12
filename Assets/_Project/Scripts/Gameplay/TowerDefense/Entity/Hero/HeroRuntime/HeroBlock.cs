using System;
using UnityEngine;

[Serializable]
public class HeroBlock
{
    private int blockCount;
    private int currentBlock;

    public int CurrentBlock => currentBlock;
    public int BlockCount => blockCount;

    public HeroBlock(int blockCount)
    {
        this.blockCount = Mathf.Max(0, blockCount);
        currentBlock = 0;
    }

    public void ResetBlock()
    {
        currentBlock = 0;
    }

    public void IncreaseBlock(int amount = 1)
    {
        currentBlock = Mathf.Min(currentBlock + amount, blockCount);
    }

    public void DecreaseBlock(int amount = 1)
    {
        currentBlock = Mathf.Max(currentBlock - amount, 0);
    }

    public void RemainBlock(int amount = 1)
    {
        currentBlock = Mathf.Max(currentBlock - amount, 0);
    }

    public bool CanBlock(int amount = 1)
    {
        if (currentBlock + amount > blockCount)
        {
            return false;
        }
        return true;
    }

    public bool IsBlocked => currentBlock > 0;
}