using UnityEngine;

public class RunSession : MonoBehaviour
{
    [SerializeField] private StartRunTeam startRunTeam = new StartRunTeam();
    [SerializeField] private bool loadStartTeam = true;

    private RunTeam runTeam;

    public RunTeam RunTeam => runTeam;

    private void Awake()
    {
        runTeam = new RunTeam();
        
        if (loadStartTeam)
        {
            LoadStartRunTeam();
        }
    }

    public void LoadStartRunTeam()
    {
        if (startRunTeam == null || !startRunTeam.HasHeroes)
        {
            return;
        }

        if (runTeam == null)
        {
            runTeam = new RunTeam();
        }

        runTeam.LoadInitialTeam(startRunTeam);
    }

    public bool HasTestTeam()
    {
        return runTeam != null && runTeam.HasHeroes;
    }
}
