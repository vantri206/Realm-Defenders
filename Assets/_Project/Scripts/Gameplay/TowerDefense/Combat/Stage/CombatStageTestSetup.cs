using System.Collections.Generic;
using UnityEngine;

public class CombatStageTestSetup : MonoBehaviour
{
    [SerializeField] private CombatStageAuthoring stageAuthoring;
    [SerializeField] private StarterHeroRoster startTeamConfig = new StarterHeroRoster();

    public bool TryCreateBootstrapData(out CombatBootstrapData bootstrapData)
    {
        HeroRoster roster = new HeroRoster();
        roster.LoadInitialRoster(startTeamConfig);
        IReadOnlyList<HeroInstance> squad = roster.Heroes;

        if (stageAuthoring == null)
        {
            Debug.LogError("[CombatStageTestSetup] CombatStageAuthoring is required.", this);
            bootstrapData = null;
            return false;
        }

        return stageAuthoring.TryCreateBootstrapData(squad, out bootstrapData);
    }
}
