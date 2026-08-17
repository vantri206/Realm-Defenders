using UnityEngine;

public class HeroPlacement : MonoBehaviour
{
    private CombatGrid combatGrid;
    private UnitPathfindingSystem unitPathfindingSystem;

    public CombatGrid CombatGrid => combatGrid;

    public void Initialize(CombatGrid combatGrid, UnitPathfindingSystem unitPathfindingSystem)
    {
        if (combatGrid == null || unitPathfindingSystem == null)
        {
            Debug.LogError("[HeroPlacement] Both CombatGrid and UnitPathfindingSystem are required to initialize hero placement.", this);
            return;
        }

        this.combatGrid = combatGrid;
        this.unitPathfindingSystem = unitPathfindingSystem;
    }

    public bool CanPlaceHero(HeroInstance instance, CombatGridCell cell)
    {
        if (instance == null || !instance.IsValid)
        {
            return false;
        }

        if (!instance.Definition.IsValid)
        {
            return false;
        }

        if (combatGrid == null)
        {
            Debug.LogError("[HeroPlacement] CombatGrid is required before checking hero placement.", this);
            return false;
        }

        if (cell == null || !combatGrid.TryGetCell(cell.CellPosition, out CombatGridCell gridCell) || gridCell != cell)
        {
            return false;
        }

        return cell.CanDeployHero();
    }

    public HeroRuntime PlaceHero(HeroInstance instance, CombatGridCell cell)
    {
        if (!CanPlaceHero(instance, cell))
        {
            return null;
        }

        combatGrid.TryCellToWorldBottomCenter(cell, out Vector3 spawnPosition);
        HeroRuntime hero = Instantiate(instance.Definition.Prefab, spawnPosition, Quaternion.identity, transform);

        hero.Initialize(instance, combatGrid, cell.CellPosition, unitPathfindingSystem);

        return hero;
    }

    public bool RemoveHero(HeroRuntime hero)
    {
        if (hero == null)
        {
            return false;
        }

        if (combatGrid == null)
        {
            Debug.LogError("[HeroPlacement] CombatGrid is required before removing a hero.", this);
            return false;
        }

        if (hero.AnchorCell == null || !combatGrid.TryGetCell(hero.AnchorCell.CellPosition, out CombatGridCell anchorCell) || anchorCell != hero.AnchorCell)
        {
            return false;
        }

        Vector3Int cellPosition = hero.AnchorCell.CellPosition;

        if (!combatGrid.TryGetCell(cellPosition, out CombatGridCell cell) || cell.AnchoredHero != hero)
        {
            return false;
        }

        cell.ClearAnchoredHero();
        hero.ClearAnchorCell();
        hero.RemoveCombat();
        Destroy(hero.gameObject);
        return true;
    }
}
