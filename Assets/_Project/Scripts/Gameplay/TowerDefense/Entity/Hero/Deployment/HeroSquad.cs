using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeroSquad : MonoBehaviour
{
    private readonly List<HeroCombatState> heroCombatStates = new List<HeroCombatState>();

    private HeroSquadView heroSquadView;
    private StageSystem levelSystem;

    public IReadOnlyList<HeroCombatState> HeroCombatStates => heroCombatStates;
    public int HeroCount => heroCombatStates.Count;
    
    public void Initialize(HeroSquadView squadView, StageSystem levelSystem)
    {
        if (squadView == null)
        {
            Debug.LogError("[HeroSquad] HeroSquadView is required to initialize the squad.", this);
            return;
        }

        heroSquadView = squadView;
        this.levelSystem = levelSystem;

        heroSquadView.Initialize();

        if (levelSystem != null)
        {
            levelSystem.OnStageStatsChanged += UpdateHeroDeployStates;
        }
    }

    public bool AddHeroInstance(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogWarning("[HeroSquad] Attempted to add a null HeroInstance to the squad.");
            return false;
        }

        HeroCombatState combatState = new HeroCombatState(heroInstance);
        if (!combatState.IsValid)
        {
            Debug.LogWarning("[HeroSquad] Failed to create a combat state for the added HeroInstance.");
            return false;
        }

        if (heroSquadView == null)
        {
            Debug.LogError("[HeroSquad] HeroSquadView is required before adding hero cards.", this);
            return false;
        }

        HeroCard heroCard = heroSquadView.AddHeroCard(combatState);
        if (heroCard == null)
        {
            Debug.LogWarning("[HeroSquad] Failed to create a HeroCard for the added HeroInstance.");
            return false;
        }
        
        heroCombatStates.Add(combatState);
        combatState.OnDeployStateChanged += OnHeroDeployStateChanged;

        return true;
    }

    public bool RemoveHeroInstance(HeroInstance heroInstance)
    {
        if (heroInstance == null)
        {
            Debug.LogWarning("[HeroSquad] Attempted to remove a null HeroInstance from the squad.");
            return false;
        }

        if (heroSquadView == null)
        {
            Debug.LogError("[HeroSquad] HeroSquadView is required before removing hero cards.", this);
            return false;
        }

        HeroCombatState combatState = heroCombatStates.FirstOrDefault(state => state != null && state.HeroInstance == heroInstance);
        if (combatState == null)
        {
            return false;
        }

        heroSquadView.RemoveHeroCard(combatState);
        combatState.OnDeployStateChanged -= OnHeroDeployStateChanged;
        heroCombatStates.Remove(combatState);
        return true;
    }

    private void UpdateHeroDeployStates()
    {
        if (levelSystem == null)
        {
            Debug.LogError("[HeroSquad] LevelSystem is required to update hero deploy states.", this);
            return;
        }

        foreach (var combatState in heroCombatStates)
        {
            if (combatState == null || (combatState.DeployState != HeroDeployState.Available && combatState.DeployState != HeroDeployState.Unavailable))
            {
                continue;
            }

            bool canDeploy = levelSystem.CanDeployHero(combatState.DeployCost);
            combatState.SetDeployState(canDeploy ? HeroDeployState.Available : HeroDeployState.Unavailable);
        }
    }

    private void OnHeroDeployStateChanged(HeroCombatState combatState, HeroDeployState newState)
    {
        if (heroSquadView == null)
        {
            Debug.LogError("[HeroSquad] HeroSquadView is required to update hero card deploy state.", this);
            return;
        }

        if (newState == HeroDeployState.Deployed)
        {
            heroSquadView.RemoveHeroCard(combatState);
        }
        else
        {
            if (!heroSquadView.HeroCards.Any(card => card.CombatState == combatState))
            {
                heroSquadView.AddHeroCard(combatState);
            }

            HeroCard heroCard = heroSquadView.HeroCards.FirstOrDefault(card => card.CombatState == combatState);
            if (heroCard != null)
            {
                heroCard.OnHeroDeployStateChanged(newState);
            }
        }
    }

    public bool HasHeroDefinition(HeroDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        return heroCombatStates.Any(combatState => combatState != null && combatState.Definition == definition);
    }
}
