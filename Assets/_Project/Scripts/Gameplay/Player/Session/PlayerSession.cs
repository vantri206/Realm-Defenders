using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    [SerializeField] private HeroProgressionConfig progressionConfig;

    [SerializeField] private StarterHeroRoster startHeroRoster = new StarterHeroRoster();
    [SerializeField] private bool loadStartRoster = true;

    private HeroProgression progression;
    private HeroRoster heroRoster;

    public HeroProgression Progression => progression;
    public HeroRoster HeroRoster => heroRoster;

    private void Awake()
    {
        progression = new HeroProgression();
        
        progression.Initialize(progressionConfig);

        heroRoster = new HeroRoster();
        
        if (loadStartRoster)
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
            heroRoster = new HeroRoster();
        }

        heroRoster.LoadInitialRoster(startHeroRoster);
        RefreshHeroProgression();
    }

    public void RefreshHeroProgression()
    {
        if (progression == null || !progression.IsInitialized || heroRoster == null)
        {
            return;
        }

        for (int i = 0; i < heroRoster.Heroes.Count; i++)
        {
            progression.RefreshHeroLevel(heroRoster.Heroes[i]);
        }
    }

    public bool HasRosterTest()
    {
        return heroRoster != null && heroRoster.HasHeroes;
    }
}
