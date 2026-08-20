using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroDeploymentSystem : MonoBehaviour
{
    private HeroInventory heroInventory;
    private HeroPlacement heroPlacement;
    private CombatTimeController combatTime;

    private HeroInstance selectedHero;
    private HeroRuntime selectedHeroRuntime;
    private readonly Dictionary<HeroInstance, HeroRuntime> deployedHero = new Dictionary<HeroInstance, HeroRuntime>();
    private bool isInitialized;

    public HeroInstance SelectedHero => selectedHero;
    public HeroRuntime SelectedHeroRuntime => selectedHeroRuntime;
    public IReadOnlyDictionary<HeroInstance, HeroRuntime> DeployedHero => deployedHero;

    public void Initialize(HeroInventory heroInventory, HeroPlacement heroPlacement, CombatTimeController combatTime)
    {
        if (heroInventory == null)
        {
            Debug.LogError("[HeroDeploymentSystem] HeroInventory is required to initialize deployment.", this);
            isInitialized = false;
            return;
        }

        if (heroPlacement == null)
        {
            Debug.LogError("[HeroDeploymentSystem] HeroPlacement is required to initialize deployment.", this);
            isInitialized = false;
            return;
        }

        if (combatTime == null)
        {
            Debug.LogError("[HeroDeploymentSystem] CombatTimeController is required to initialize deployment.", this);
            isInitialized = false;
            return;
        }

        this.heroInventory = heroInventory;
        this.heroPlacement = heroPlacement;
        this.combatTime = combatTime;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        for (int i = 0; i < heroInventory.HeroCount; i++)
        {
            HeroInstance heroInstance = heroInventory.HeroInstances[i];
            if (heroInstance == null || !heroInstance.IsValid)
            {
                continue;
            }

            if (!heroInstance.IsReadyDeploy)
            {
                bool isReady = heroInstance.TickRedeployTimer(combatTime.CombatDeltaTime);
                if (isReady)
                {
                    heroInstance.SetDeployState(HeroDeployState.Available);
                }
            }
        }
    }


    public void ClearSelection()
    {
        selectedHero = null;
    }

    public bool SelectHero(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return false;
        }

        selectedHero = heroInstance;
        return true;
    }

    public bool CanDeploySelectedHero(CombatGridCell cell)
    {
        if (selectedHero == null)
        {
            return false;
        }

        if (heroPlacement == null)
        {
            Debug.LogError("[HeroDeploymentSystem] HeroPlacement is required to deploy selected hero.", this);
            return false;
        }

        if (!HasHeroInInventory(selectedHero) || IsHeroDeployed(selectedHero))
        {
            return false;
        }

        return heroPlacement.CanPlaceHero(selectedHero, cell);
    }

    public bool SelectHeroRuntime(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null)
        {
            return false;
        }

        if (!SelectHero(heroRuntime.Instance))
        {
            return false;
        }

        selectedHeroRuntime = heroRuntime;
        return true;
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
            selectedHero.SetDeployState(HeroDeployState.Deployed);
            
            ClearSelection();
            placedHero.SetInitialFacingDirection(direction);

            placedHero.OnDestroyed += HandleHeroDestroyed;
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
            Debug.LogError("[HeroDeploymentSystem] HeroPlacement with a CombatGrid is required to retreat a hero.", this);
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

    public bool RetreatHero(HeroRuntime heroRuntime)
    {
        if (!heroPlacement.RemoveHero(heroRuntime))
        {
            return false;
        }

        return RemoveDeployedHero(heroRuntime.Instance);
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

        return RetreatHero(heroRuntime);
    }

    private bool AddDeployedHero(HeroInstance heroInstance, HeroRuntime heroRuntime)
    {
        if (heroInstance == null || !heroInstance.IsValid || heroRuntime == null)
        {
            return false;
        }

        if (!deployedHero.ContainsKey(heroInstance))
        {
            deployedHero.Add(heroInstance, heroRuntime);
            return true;
        }

        return false;
    }

    private bool RemoveDeployedHero(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return false;
        }

        return deployedHero.Remove(heroInstance);
    }

    private bool HasHeroInInventory(HeroInstance heroInstance)
    {
        if (heroInventory == null || heroInstance == null)
        {
            if (heroInventory == null)
            {
                Debug.LogError("[HeroDeploymentSystem] HeroInventory is required before checking inventory membership.", this);
            }

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

    private void HandleHeroDestroyed(UnitRuntime unitRuntime)
    {
        if (!(unitRuntime is HeroRuntime heroRuntime))
        {
            return;
        }


        if (heroRuntime == null || heroRuntime.Instance == null)
        {
            return;
        }

        RemoveDeployedHero(heroRuntime.Instance);
    }
}
