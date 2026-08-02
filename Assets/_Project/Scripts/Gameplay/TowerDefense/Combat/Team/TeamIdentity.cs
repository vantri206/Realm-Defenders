using UnityEngine;

public enum Team
{
    Environment,    // Neutral entities can damage all other factions, and only be damaged by player
    Player,
    Enemy
}

public class TeamIdentity : MonoBehaviour
{
    [SerializeField] private Team team = Team.Environment;
    public Team Team => team;

    public bool IsEnemy(TeamIdentity other)
    {
        if (other == null)
        {
            return false;
        }

        if (team == Team.Environment)
        {
            return true;
        }

        if (other.team == Team.Environment)
        {
            return team == Team.Player;
        }

        return team != other.team;
    }
}
