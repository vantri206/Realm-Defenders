using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroDeploymentSystem : MonoBehaviour
{
    private HeroSquad heroSquad;
    private HeroPlacement heroPlacement;
    private CombatTimeController combatTime;

    private HeroCombatState selectedHero;
    private HeroRuntime selectedHeroRuntime;
    private readonly Dictionary<HeroCombatState, HeroRuntime> deployedHero = new Dictionary<HeroCombatState, HeroRuntime>();
    private bool isInitialized;

    public HeroCombatState SelectedHero => selectedHero;
    public HeroRuntime SelectedHeroRuntime => selectedHeroRuntime;
    public IReadOnlyDictionary<HeroCombatState, HeroRuntime> DeployedHero => deployedHero;

    public void Initialize(HeroSquad heroSquad, HeroPlacement heroPlacement, CombatTimeController combatTime)
    {
        if (heroSquad == null)
        {
            Debug.LogError("[HeroDeploymentSystem] HeroSquad is required to initialize deployment.", this);
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

        this.heroSquad = heroSquad;
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

        for (int i = 0; i < heroSquad.HeroCount; i++)
        {
            HeroCombatState combatState = heroSquad.HeroCombatStates[i];
            if (combatState == null || !combatState.IsValid)
            {
                continue;
            }

            if (!combatState.IsReadyDeploy)
            {
                bool isReady = combatState.TickRedeployTimer(combatTime.CombatDeltaTime);
                if (isReady)
                {
                    combatState.SetDeployState(HeroDeployState.Available);
                }
            }
        }
    }


    public void ClearSelection()
    {
        selectedHero = null;
        selectedHeroRuntime = null;
    }

    public bool SelectHero(HeroCombatState combatState)
    {
        if (combatState == null || !combatState.IsValid)
        {
            return false;
        }

        selectedHero = combatState;
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

        if (selectedHero.DeployState != HeroDeployState.Available || !HasHeroInSquad(selectedHero) || IsHeroDeployed(selectedHero))
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

        if (!SelectHero(heroRuntime.CombatState))
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

    public bool IsHeroDeployed(HeroCombatState combatState)
    {
        if (combatState == null || !combatState.IsValid)
        {
            return false;
        }

        return deployedHero.ContainsKey(combatState);
    }

    public bool TryGetDeployedHero(HeroCombatState combatState, out HeroRuntime heroRuntime)
    {
        if (combatState == null || !combatState.IsValid)
        {
            heroRuntime = null;
            return false;
        }

        return deployedHero.TryGetValue(combatState, out heroRuntime);
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

        if (!HasHeroInSquad(selectedHero) || !IsHeroDeployed(selectedHero))
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

        RemoveDeployedHero(heroRuntime.CombatState);
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

        return RetreatHero(heroRuntime);
    }

    private bool AddDeployedHero(HeroCombatState combatState, HeroRuntime heroRuntime)
    {
        if (combatState == null || !combatState.IsValid || heroRuntime == null)
        {
            return false;
        }

        if (!deployedHero.ContainsKey(combatState))
        {
            deployedHero.Add(combatState, heroRuntime);
            return true;
        }

        return false;
    }

    private void RemoveDeployedHero(HeroCombatState combatState)
    {
        if (combatState == null || !combatState.IsValid)
        {
            return;
        }

        deployedHero.Remove(combatState);
    }

    private bool HasHeroInSquad(HeroCombatState combatState)
    {
        if (heroSquad == null || combatState == null)
        {
            if (heroSquad == null)
            {
                Debug.LogError("[HeroDeploymentSystem] HeroSquad is required before checking squad membership.", this);
            }

            return false;
        }

        IReadOnlyList<HeroCombatState> combatStates = heroSquad.HeroCombatStates;
        for (int i = 0; i < combatStates.Count; i++)
        {
            if (combatStates[i] == combatState)
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


        if (heroRuntime == null || heroRuntime.CombatState == null)
        {
            return;
        }

        heroRuntime.OnDestroyed -= HandleHeroDestroyed;
        RemoveDeployedHero(heroRuntime.CombatState);
        heroRuntime.CombatState.StartRedeployCountdown();
    }
}
