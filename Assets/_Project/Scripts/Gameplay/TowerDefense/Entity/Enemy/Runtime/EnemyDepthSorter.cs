using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class EnemyDepthSorter : MonoBehaviour
{
    private int neighborCellRange = 1;
    private int baseSortingOrder;

    private readonly List<EnemyRuntime> registeredEnemies = new List<EnemyRuntime>();
    private readonly List<EnemyRuntime> groundEnemies = new List<EnemyRuntime>();
    private readonly List<EnemyRuntime> flyingEnemies = new List<EnemyRuntime>();
    private readonly List<EnemyRuntime> processedEnemies = new List<EnemyRuntime>();
    private readonly List<EnemyRuntime> currentDepthGroup = new List<EnemyRuntime>();
    private readonly Dictionary<EnemyRuntime, int> depthIdEnemy = new Dictionary<EnemyRuntime, int>();
    private readonly Dictionary<EnemyRuntime, SortingGroup> sortingGroupByEnemy = new Dictionary<EnemyRuntime, SortingGroup>();

    private int nextDepthId;

    private void LateUpdate()
    {
        RefreshSorting();
    }

    public void RegisterEnemy(EnemyRuntime enemy)
    {
        if (enemy == null || registeredEnemies.Contains(enemy))
        {
            return;
        }

        SortingGroup sortingGroup = enemy.Visual?.SortingGroup;
        SpriteRenderer spriteRenderer = enemy.Visual?.SpriteRenderer;

        if (sortingGroup == null || spriteRenderer == null)
        {
            Debug.Log($"[EnemyDepthSorter] Enemy '{enemy.name}' is missing a SortingGroup or SpriteRenderer");
            return;
        }

        if (spriteRenderer != null)
        {
            sortingGroup.sortingLayerID = spriteRenderer.sortingLayerID;
        }

        registeredEnemies.Add(enemy);
        depthIdEnemy.Add(enemy, nextDepthId++);
        sortingGroupByEnemy.Add(enemy, sortingGroup);
        enemy.SetDepthSorter(this);

        RefreshSorting();
    }

    public void UnregisterEnemy(EnemyRuntime enemy)
    {
        if (enemy == null)
        {
            return;
        }

        registeredEnemies.Remove(enemy);
        depthIdEnemy.Remove(enemy);
        sortingGroupByEnemy.Remove(enemy);
        enemy.SetDepthSorter(null);
    }

    public void RefreshSorting()
    {
        RemoveMissingEnemies();
        GroupEnemiesByMovementType();

        int groundBandSize = RefreshMovementTypeSorting(groundEnemies, baseSortingOrder);
        int flyingBaseSortingOrder = baseSortingOrder + groundBandSize;
        RefreshMovementTypeSorting(flyingEnemies, flyingBaseSortingOrder);
    }

    private int RefreshMovementTypeSorting(IReadOnlyList<EnemyRuntime> enemies, int bandBaseSortingOrder)
    {
        processedEnemies.Clear();
        int bandSize = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyRuntime enemy = enemies[i];
            if (enemy == null || processedEnemies.Contains(enemy))
            {
                continue;
            }

            CollectDepthGroup(enemy, enemies);
            SortDepthGroupByDepthId();
            ApplyDepthGroupSorting(bandBaseSortingOrder);
            bandSize = Mathf.Max(bandSize, currentDepthGroup.Count);
        }

        return bandSize;
    }

    private void GroupEnemiesByMovementType()
    {
        groundEnemies.Clear();
        flyingEnemies.Clear();

        for (int i = 0; i < registeredEnemies.Count; i++)
        {
            EnemyRuntime enemy = registeredEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (enemy.MovementType == UnitMovementType.Flying)
            {
                flyingEnemies.Add(enemy);
            }
            else
            {
                groundEnemies.Add(enemy);
            }
        }
    }

    private void RemoveMissingEnemies()
    {
        for (int i = registeredEnemies.Count - 1; i >= 0; i--)
        {
            EnemyRuntime enemy = registeredEnemies[i];
            if (enemy != null)
            {
                continue;
            }

            registeredEnemies.RemoveAt(i);
            depthIdEnemy.Remove(enemy);
            sortingGroupByEnemy.Remove(enemy);
        }
    }

    private void CollectDepthGroup(EnemyRuntime seedEnemy, IReadOnlyList<EnemyRuntime> enemies)
    {
        currentDepthGroup.Clear();
        AddEnemyToDepthGroup(seedEnemy);

        int searchRange = Mathf.Max(0, neighborCellRange);
        for (int groupIndex = 0; groupIndex < currentDepthGroup.Count; groupIndex++)
        {
            EnemyRuntime groupEnemy = currentDepthGroup[groupIndex];
            if (groupEnemy == null || groupEnemy.ActiveCell == null)
            {
                continue;
            }

            Vector3Int groupCellPosition = groupEnemy.ActiveCellPosition;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntime enemy = enemies[i];
                if (enemy == null || enemy.ActiveCell == null || processedEnemies.Contains(enemy))
                {
                    continue;
                }

                if (!IsInNeighborRange(groupCellPosition, enemy.ActiveCellPosition, searchRange))
                {
                    continue;
                }

                AddEnemyToDepthGroup(enemy);
            }
        }
    }

    private void AddEnemyToDepthGroup(EnemyRuntime enemy)
    {
        currentDepthGroup.Add(enemy);
        processedEnemies.Add(enemy);
    }

    private bool IsInNeighborRange(Vector3Int centerCell, Vector3Int cell, int range)
    {
        return Mathf.Abs(cell.x - centerCell.x) <= range && Mathf.Abs(cell.y - centerCell.y) <= range;
    }

    private void SortDepthGroupByDepthId()
    {
        currentDepthGroup.Sort(CompareDepthId);
    }

    private int CompareDepthId(EnemyRuntime first, EnemyRuntime second)
    {
        int firstDepthId = depthIdEnemy.TryGetValue(first, out int cachedFirstDepthId) ? cachedFirstDepthId : int.MaxValue;
        int secondDepthId = depthIdEnemy.TryGetValue(second, out int cachedSecondDepthId) ? cachedSecondDepthId : int.MaxValue;
        return firstDepthId.CompareTo(secondDepthId);
    }

    private void ApplyDepthGroupSorting(int bandBaseSortingOrder)
    {
        for (int i = 0; i < currentDepthGroup.Count; i++)
        {
            EnemyRuntime enemy = currentDepthGroup[i];
            if (sortingGroupByEnemy.TryGetValue(enemy, out SortingGroup sortingGroup) && sortingGroup != null)
            {
                sortingGroup.sortingOrder = bandBaseSortingOrder + i;
            }
        }
    }
}
