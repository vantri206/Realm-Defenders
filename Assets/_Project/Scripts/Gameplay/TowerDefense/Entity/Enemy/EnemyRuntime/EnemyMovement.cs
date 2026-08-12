using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    private const float collisionWidth = 0.01f;
    private const int castResultsCount = 16;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;
    
    private EnemySpeed enemySpeed;
    private Vector2 currentMoveDirection;

    private readonly RaycastHit2D[] castResults = new RaycastHit2D[castResultsCount];
    private ContactFilter2D movementBlockerFilter;

    public EnemySpeed Speed => enemySpeed;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (col == null)
        {
            col = GetComponentInChildren<Collider2D>();
        }

        movementBlockerFilter = new ContactFilter2D();
        movementBlockerFilter.SetLayerMask(GameLayer.ObstacleBlockerMask);
        movementBlockerFilter.useTriggers = false;
    }
    
    public void Initialize(EnemySpeed enemySpeed)
    {
        if (enemySpeed == null)
        {
            Debug.LogError("[EnemyMovement] EnemySpeed is required for movement initialization.", this);
            return;
        }

        this.enemySpeed = enemySpeed;
    }

    public void FixedTick(float fixedDeltaTime)
    {
        if (rb == null)
        {
            Debug.LogError("[EnemyMovement] Rigidbody2D is required to process movement.", this);
            return;
        }

        if (enemySpeed == null)
        {
            Debug.LogError("[EnemyMovement] EnemySpeed is required to process movement. Call Initialize before FixedTick.", this);
            return;
        }

        Vector2 movement = currentMoveDirection.normalized * enemySpeed.MoveSpeed * fixedDeltaTime;
        Vector2 resolvedMovement = ResolveMovement(movement);
        rb.MovePosition(rb.position + resolvedMovement);
    }

    private Vector2 ResolveMovement(Vector2 movement)
    {
        float moveDistance = movement.magnitude;
        if (moveDistance <= Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        if (col == null)
        {
            Debug.LogError("[EnemyMovement] Collider2D is required to cast movement against blockers.", this);
            return movement;
        }

        Vector2 moveDirection = movement / moveDistance;
        int hitCount = col.Cast(moveDirection, movementBlockerFilter, castResults, moveDistance + collisionWidth);
        if (hitCount == 0)
        {
            return movement;
        }

        float nearestHitDistance = moveDistance;
        for (int i = 0; i < hitCount; i++)
        {
            nearestHitDistance = Mathf.Min(nearestHitDistance, castResults[i].distance);
        }

        float canMoveDistance = Mathf.Max(0f, nearestHitDistance - collisionWidth);
        return moveDirection * canMoveDistance;
    }
    
    public void SetMoveSpeed(float newMoveSpeed)
    {
        if (enemySpeed == null)
        {
            Debug.LogError("[EnemyMovement] Cannot set move speed before Initialize.", this);
            return;
        }
        
        enemySpeed.SetMoveSpeed(newMoveSpeed);
    }

    public void SetMoveDirection(Vector2 direction)
    {
        currentMoveDirection = direction;
    }
}
