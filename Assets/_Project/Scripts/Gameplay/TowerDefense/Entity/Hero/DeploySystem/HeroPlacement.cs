using UnityEngine;

public class HeroPlacement : MonoBehaviour
{
    private CombatGrid combatGrid;

    public CombatGrid CombatGrid => combatGrid;

    public void Initialize(CombatGrid combatGrid)
    {
        this.combatGrid = combatGrid;
    }

    public bool CanPlaceHero(HeroInstance instance, CombatGridCell cell)
    {
        if (instance == null || !instance.IsValid)
        {
            return false;
        }

        if (!instance.Definition.IsValid || combatGrid == null)
        {
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
        HeroRuntime hero = null;

        if (!CanPlaceHero(instance, cell))
        {
            return null;
        }

        Vector3 spawnPosition = combatGrid.CellToWorldBottomCenter(cell.CellPosition);
        hero = Instantiate(instance.Definition.Prefab, spawnPosition, Quaternion.identity, transform);

        hero.Initialize(instance, combatGrid, cell.CellPosition);
        cell.SetDeployedHero(hero);

        return hero;
    }

    public bool RemoveHero(HeroRuntime hero)
    {
        if (hero == null || combatGrid == null)
        {
            return false;
        }

        if (hero.GridPosition == null || !hero.GridPosition.HasCell)
        {
            return false;
        }

        Vector3Int cellPosition = hero.GridPosition.CurrentCell;
        if (!combatGrid.TryGetCell(cellPosition, out CombatGridCell cell) || cell.DeployedHero != hero)
        {
            return false;
        }

        cell.ClearDeployedHero();
        hero.GridPosition.Clear();
        Destroy(hero.gameObject);
        return true;
    }
}
