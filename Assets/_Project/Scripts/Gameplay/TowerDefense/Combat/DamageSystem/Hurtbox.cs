using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private MonoBehaviour damageableMB;

    private IDamageable damageable;
    private Collider2D hurtboxCollider;

    private void Awake()
    {
        if (hurtboxCollider == null)
        {
            hurtboxCollider = GetComponent<Collider2D>();
        }
    }

    public Vector2 Position
    {
        get
        {
            return hurtboxCollider != null  ? (Vector2)hurtboxCollider.bounds.center : (Vector2)transform.position;
        }
    }

    public IDamageable GetDamageable()
    {
        if (damageable != null)
        {
            return damageable;
        }

        damageable = damageableMB as IDamageable;

        if (damageable == null)
        {
            damageable = GetComponentInParent<IDamageable>();
            damageableMB = damageable as MonoBehaviour;
        }

        return damageable;
    }

    public GameObject GetTargetObject()
    {
        IDamageable damageable = GetDamageable();

        if (damageable is Component component)
        {
            return component.gameObject;
        }

        return gameObject;
    }

    public TeamIdentity GetTargetTeam()
    {
        TeamIdentity teamIdentity = GetComponentInParent<TeamIdentity>();

        if (teamIdentity == null)
        {
            Debug.LogWarning($"[Hurtbox] {name} does not have a TeamIdentity.", this);
        }

        return teamIdentity;
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        if (damageableMB != null && damageableMB is not IDamageable)
        {
            Debug.LogError($"{damageableMB.name} must implement IDamageable.", this);
            damageableMB = null;
        }
    }

    private void Reset()
    {
        IDamageable damageableBehaviour = GetComponentInParent<IDamageable>();
        damageableMB = damageableBehaviour as MonoBehaviour;
    }
#endif
}
