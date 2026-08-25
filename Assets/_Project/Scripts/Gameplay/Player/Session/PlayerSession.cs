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
    }

    public bool HasRosterTest()
    {
        return heroRoster != null && heroRoster.HasHeroes;
    }
}
