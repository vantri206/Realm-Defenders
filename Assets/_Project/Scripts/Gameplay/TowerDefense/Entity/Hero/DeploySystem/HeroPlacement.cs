using UnityEngine;

public class HeroPlacement : MonoBehaviour
{
    private UnitCombatContext combatContext;

    public CombatGrid CombatGrid => combatContext?.CombatGrid;

    public void Initialize(UnitCombatContext combatContext)
    {
        if (combatContext == null || !combatContext.IsValid)
        {
            Debug.LogError("[HeroPlacement] A valid CombatReferencesContext is required to initialize hero placement.", this);
            return;
        }

        this.combatContext = combatContext;
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

        if (combatContext == null || combatContext.CombatGrid == null)
        {
            Debug.LogError("[HeroPlacement] CombatGrid is required before checking hero placement.", this);
            return false;
        }

        if (cell == null || !combatContext.CombatGrid.TryGetCell(cell.CellPosition, out CombatGridCell gridCell) || gridCell != cell)
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

        combatContext.CombatGrid.TryCellToWorldBottomCenter(cell, out Vector3 spawnPosition);
        HeroRuntime hero = Instantiate(instance.Definition.Prefab, spawnPosition, Quaternion.identity, transform);

        hero.Initialize(instance, combatContext, cell.CellPosition);

        return hero;
    }

    public bool RemoveHero(HeroRuntime hero)
    {
        if (hero == null)
        {
            return false;
        }

        if (combatContext == null || combatContext.CombatGrid == null)
        {
            Debug.LogError("[HeroPlacement] CombatGrid is required before removing a hero.", this);
            return false;
        }

        if (hero.AnchorCell == null || !combatContext.CombatGrid.TryGetCell(hero.AnchorCell.CellPosition, out CombatGridCell anchorCell) || anchorCell != hero.AnchorCell)
        {
            return false;
        }

        Vector3Int cellPosition = hero.AnchorCell.CellPosition;

        if (!combatContext.CombatGrid.TryGetCell(cellPosition, out CombatGridCell cell) || cell.AnchoredHero != hero)
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
