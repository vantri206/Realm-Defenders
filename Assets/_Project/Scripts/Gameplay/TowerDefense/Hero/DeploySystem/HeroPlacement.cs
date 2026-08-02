using UnityEngine;

public class HeroPlacement : MonoBehaviour
{
    private CombatGrid combatGrid;

    public CombatGrid CombatGrid => combatGrid;

    public void Initialize(CombatGrid combatGrid)
    {
        this.combatGrid = combatGrid;
    }

    public bool CanPlaceHero(HeroInstance instance, Vector3Int cellPosition)
    {
        if (instance == null || !instance.IsValid)
        {
            return false;
        }

        if (instance.Definition.Prefab == null || combatGrid == null)
        {
            return false;
        }

        if (!combatGrid.TryGetCell(cellPosition, out CombatGridCell cell))
        {
            return false;
        }

        return cell.CanDeployHero();
    }

    public HeroRuntime PlaceHero(HeroInstance instance, Vector3Int cellPosition)
    {
        HeroRuntime hero = null;

        if (!CanPlaceHero(instance, cellPosition))
        {
            return null;
        }

        if (!combatGrid.TryGetCell(cellPosition, out CombatGridCell cell))
        {
            return null;
        }

        Vector3 spawnPosition = combatGrid.CellToWorldCenter(cellPosition);
        hero = Instantiate(instance.Definition.Prefab, spawnPosition, Quaternion.identity, transform);

        hero.Initialize(instance, combatGrid, cellPosition);
        cell.SetDeployedHero(hero);

        return hero;
    }

    public HeroRuntime RemoveHero(HeroRuntime hero)
    {
        if (hero == null || combatGrid == null)
        {
            return null;
        }

        if (hero.GridPosition == null || !hero.GridPosition.HasCell)
        {
            return null;
        }

        Vector3Int cellPosition = hero.GridPosition.CurrentCell;
        if (!combatGrid.TryGetCell(cellPosition, out CombatGridCell cell) || cell.DeployedHero != hero)
        {
            return null;
        }

        cell.ClearDeployedHero();
        hero.GridPosition.Clear();
        Destroy(hero.gameObject);
        return hero;
    }
}
