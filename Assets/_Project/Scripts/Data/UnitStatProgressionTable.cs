using UnityEngine;

public class UnitStatProgressionTable : ScriptableObject
{
    [SerializeField, HideInInspector] private UnitBaseStats[] statsByLevel;

    public int MaxLevel => statsByLevel != null ? statsByLevel.Length : 0;

    public UnitBaseStats GetStatsForLevel(int level)
    {
        if (statsByLevel == null || level < 1 || level > statsByLevel.Length)
        {
            Debug.LogError($"[UnitStatProgressionTable] Invalid level {level}. Valid levels are between 1 and {MaxLevel}.");
            return null;
        }

        return statsByLevel[level - 1];
    }
}
