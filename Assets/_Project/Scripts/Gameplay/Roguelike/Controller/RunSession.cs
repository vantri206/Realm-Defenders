using UnityEngine;

public class RunSession : MonoBehaviour
{
    [SerializeField] private RunConfig runConfig;

    [SerializeField] private StartHeroRoster startHeroRoster = new StartHeroRoster();
    [SerializeField] private bool loadStartTeam = true;

    private RunProgression progression;
    private RunHeroRoster heroRoster;

    private bool isInitialized = false;

    public RunProgression Progression => progression;
    public RunHeroRoster HeroRoster => heroRoster;

    private void Awake()
    {
        heroRoster = new RunHeroRoster();
        
        if (loadStartTeam)
        {
            LoadStartRoster();
        }
    }

    public void LoadStartRoster()
    {
        if (startHeroRoster == null || !startHeroRoster.HasHeroes)
        {
            return;
        }

        if (heroRoster == null)
        {
            heroRoster = new RunHeroRoster();
        }

        heroRoster.LoadInitialTeam(startHeroRoster);
    }

    public bool HasRosterTest()
    {
        return heroRoster != null && heroRoster.HasHeroes;
    }
}
