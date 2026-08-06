using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroDeploymentSystem : MonoBehaviour
{
    private HeroInventory heroInventory;
    private HeroPlacement heroPlacement;

    private HeroInstance selectedHero;
    private readonly Dictionary<HeroInstance, HeroRuntime> deployedHero = new Dictionary<HeroInstance, HeroRuntime>();

    public HeroInstance SelectedHero => selectedHero;
    public IReadOnlyDictionary<HeroInstance, HeroRuntime> DeployedHero => deployedHero;

    public void Initialize(HeroInventory heroInventory, HeroPlacement heroPlacement)
    {
        this.heroInventory = heroInventory;
        this.heroPlacement = heroPlacement;
    }

    public void ClearSelection()
    {
        selectedHero = null;
    }

    public bool CanSelectHero(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return false;
        }

        if (!HasHeroInInventory(heroInstance))
        {
            return false;
        }

        return true;
    }

    public bool SelectHero(HeroInstance heroInstance)
    {
        if (!CanSelectHero(heroInstance))
        {
            return false;
        }

        selectedHero = heroInstance;
        return true;
    }

    public bool CanDeploySelectedHero(CombatGridCell cell)
    {
        if (selectedHero == null || heroPlacement == null)
        {
            return false;
        }

        if (!HasHeroInInventory(selectedHero) || IsHeroDeployed(selectedHero))
        {
            return false;
        }

        return heroPlacement.CanPlaceHero(selectedHero, cell);
    }

    public HeroRuntime DeploySelectedHero(CombatGridCell cell, Vector2Int direction)
    {
        if (!CanDeploySelectedHero(cell))
        {
            return null;
        }

        HeroRuntime placedHero = heroPlacement.PlaceHero(selectedHero, cell);
        if (placedHero != null)
        {
            AddDeployedHero(selectedHero, placedHero);
            ClearSelection();

            placedHero?.SetFacingDirection(direction);
        }

        return placedHero;
    }

    public bool IsHeroDeployed(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return false;
        }

        return deployedHero.ContainsKey(heroInstance);
    }

    public bool TryGetDeployedHero(HeroInstance heroInstance, out HeroRuntime heroRuntime)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            heroRuntime = null;
            return false;
        }

        return deployedHero.TryGetValue(heroInstance, out heroRuntime);
    }

    public bool CanRetreatSelectedHero()
    {
        if (heroPlacement == null || heroPlacement.CombatGrid == null)
        {
            return false;
        }

        if (selectedHero == null || !selectedHero.IsValid)
        {
            return false;
        }

        if (!HasHeroInInventory(selectedHero) || !IsHeroDeployed(selectedHero))
        {
            return false;
        }

        return true;
    }

    public bool RetreatSelectedHero()
    {
        if (!CanRetreatSelectedHero())
        {
            return false;
        }

        if (!TryGetDeployedHero(selectedHero, out HeroRuntime heroRuntime))
        {
            return false;
        }

        HeroInstance heroInstance = selectedHero;
        if (!heroPlacement.RemoveHero(heroRuntime))
        {
            return false;
        }

        RemoveDeployedHero(heroInstance);
        return true;
    }

    private void AddDeployedHero(HeroInstance heroInstance, HeroRuntime heroRuntime)
    {
        if (heroInstance == null || !heroInstance.IsValid || heroRuntime == null)
        {
            return;
        }

        if (!deployedHero.ContainsKey(heroInstance))
        {
            deployedHero.Add(heroInstance, heroRuntime);
        }
    }

    private void RemoveDeployedHero(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return;
        }

        deployedHero.Remove(heroInstance);
    }

    private bool HasHeroInInventory(HeroInstance heroInstance)
    {
        if (heroInventory == null || heroInstance == null)
        {
            return false;
        }

        IReadOnlyList<HeroInstance> heroInstances = heroInventory.HeroInstances;
        for (int i = 0; i < heroInstances.Count; i++)
        {
            if (heroInstances[i] == heroInstance)
            {
                return true;
            }
        }

        return false;
    }
}
