using UnityEngine;

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

    public bool IsAlly(TeamIdentity other)
    {
        if (other == null || team == Team.Environment || other.team == Team.Environment)
        {
            return false;
        }

        return team == other.team;
    }

    public bool IsTargetSide(TeamIdentity other, TargetSide targetSide)
    {
        switch (targetSide)
        {
            case TargetSide.Enemy:
                return IsEnemy(other);
            case TargetSide.Ally:
                return IsAlly(other);
            default:
                return false;
        }
    }
}
